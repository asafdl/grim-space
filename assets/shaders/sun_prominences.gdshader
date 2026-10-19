shader_type spatial;
render_mode unshaded, blend_add, depth_draw_never, cull_disabled, shadows_disabled, fog_disabled;

// ---------------------------------------------------------------------------
// Solar prominences + chromosphere, drawn as a camera-facing additive quad
// just outside the photosphere.
//
// Geometry: a prominence is a plasma *loop* rooted at the limb.  The loop is
// rendered as a thin shell (distance-to-arc) rather than a blob, because the
// real thing reads as a bright filament with a darker interior - see the AIA
// 304 frame grabs in SUN_NOTES.md.
//
// Measured reference values used here:
//   * loop height above the limb: 0.10 - 0.26 solar radii (max observed 0.67R)
//   * prominence brightness: ~40% of the disk brightness, strongly red
//     (mean rgb(0.24, 0.05, 0.05) against a limb of rgb(0.49, 0.03, 0.005))
//   * chromospheric ring just outside the limb: 0.145 of disk at r = 1.00R,
//     0.028 at 1.05R  ->  exponential, e-fold ~0.03R
// ---------------------------------------------------------------------------

uniform vec3 prominence_color : source_color = vec3(1.00, 0.16, 0.05);
uniform vec3 footpoint_color : source_color = vec3(1.00, 0.35, 0.10);
uniform vec3 limb_ring_color : source_color = vec3(1.00, 0.20, 0.06);
uniform float brightness : hint_range(0.0, 3.0) = 0.16;
uniform float limb_ring_strength : hint_range(0.0, 2.0) = 0.12;
uniform float spicule_haze : hint_range(0.0, 2.0) = 0.16;
uniform int site_count : hint_range(0, 8) = 5;
uniform float erupt_period : hint_range(1.0, 120.0) = 13.0;
uniform float loop_height : hint_range(0.02, 0.70) = 0.20;
uniform float loop_span : hint_range(0.05, 1.20) = 0.30;
uniform float filament_width : hint_range(0.002, 0.08) = 0.014;
uniform float curl_strength : hint_range(0.0, 1.0) = 0.85;
uniform float curtain_strength : hint_range(0.0, 1.0) = 0.09;
uniform float seed = 0.0;
uniform float sun_radius = 1.0;
uniform float quad_half_width = 2.0;
uniform int debug_term = 0;

varying vec2 v_quad;
varying float v_limb;

const float SUN_PI = 3.141592653589793;
const float SUN_TAU = 6.283185307179586;

float hash11(float n) {
	return fract(sin(n * 127.1 + 311.7) * 43758.5453123);
}

float hash31(vec3 p) {
	p = fract(p * 0.3183099 + 0.1);
	p *= 17.0;
	return fract(p.x * p.y * p.z * (p.x + p.y + p.z));
}

// Value noise with the quintic fade, so the field is C2 continuous.  That
// matters here: the curl below differentiates the field, and the cheaper
// cubic fade would leave creases along the lattice boundaries.
float vnoise(vec3 p) {
	vec3 i = floor(p);
	vec3 f = fract(p);
	f = f * f * f * (f * (f * 6.0 - 15.0) + 10.0);
	return mix(
		mix(mix(hash31(i), hash31(i + vec3(1.0, 0.0, 0.0)), f.x),
			mix(hash31(i + vec3(0.0, 1.0, 0.0)), hash31(i + vec3(1.0, 1.0, 0.0)), f.x), f.y),
		mix(mix(hash31(i + vec3(0.0, 0.0, 1.0)), hash31(i + vec3(1.0, 0.0, 1.0)), f.x),
			mix(hash31(i + vec3(0.0, 1.0, 1.0)), hash31(i + vec3(1.0, 1.0, 1.0)), f.x), f.y),
		f.z);
}

// Curl of a scalar noise potential: a divergence-free 2D flow field.  Using it
// to displace the loop surface gives the plasma its writhing, turbulent look
// without the loop ever losing its shape.
vec2 curl2(vec2 p) {
	const float e = 0.35;
	float n0 = vnoise(vec3(p, 0.0));
	float nx = vnoise(vec3(p + vec2(e, 0.0), 0.0));
	float ny = vnoise(vec3(p + vec2(0.0, e), 0.0));
	return vec2(ny - n0, -(nx - n0)) / e;
}

float ang_diff(float a, float b) {
	float d = a - b;
	return mod(d + SUN_PI, SUN_TAU) - SUN_PI;
}

void vertex() {
	vec3 center = MODEL_MATRIX[3].xyz;
	vec3 facing = normalize(CAMERA_POSITION_WORLD - center);
	vec3 up_hint = INV_VIEW_MATRIX[1].xyz;
	vec3 right = normalize(cross(up_hint, facing));
	vec3 up = cross(facing, right);
	float scale = length(MODEL_MATRIX[0].xyz);
	MODELVIEW_MATRIX = VIEW_MATRIX * mat4(
		vec4(right * scale, 0.0), vec4(up * scale, 0.0),
		vec4(facing * scale, 0.0), vec4(center, 1.0));

	// A sphere of radius R seen from distance D covers an apparent radius of
	// R*D/sqrt(D^2-R^2) on the quad plane.  Dividing by this keeps the
	// prominences rooted exactly on the silhouette at any camera distance.
	float d = max(distance(CAMERA_POSITION_WORLD, center), sun_radius * 1.0001);
	v_limb = d / sqrt(max(d * d - sun_radius * sun_radius, 1e-6));

	// v_quad is measured in solar radii so the same radius arithmetic works
	// whatever size the quad was given.  The quad half width is a mesh
	// property, not part of the node transform, so it arrives as a uniform.
	v_quad = (UV * 2.0 - 1.0) * (quad_half_width / max(sun_radius, 1e-6));
}

void fragment() {
	float r = length(v_quad) / v_limb;
	float theta = atan(v_quad.y, v_quad.x);

	vec3 acc_filament = vec3(0.0);
	vec3 acc_curtain = vec3(0.0);
	vec3 acc_foot = vec3(0.0);

	for (int i = 0; i < 8; i++) {
		if (i >= site_count) {
			break;
		}
		float fi = float(i);
		float h1 = hash11(fi * 17.13 + seed);
		float h2 = hash11(fi * 31.71 + seed + 5.3);
		float h3 = hash11(fi * 47.33 + seed + 11.9);

		float base_angle = h1 * SUN_TAU;
		float phase = h2;
		float height_scale = 0.45 + 0.85 * h3;
		float span_scale = 0.55 + 0.80 * fract(h1 * 13.37 + h2);

		// Erupt cycle: the loop rises, hangs, then collapses back onto the limb.
		float ph = fract(TIME / erupt_period + phase);
		float env = smoothstep(0.0, 0.16, ph) * (1.0 - smoothstep(0.42, 0.92, ph));
		if (env <= 0.001) {
			continue;
		}

		float h = loop_height * height_scale * env;
		float span = loop_span * span_scale;
		float a = base_angle + sin(TIME * 0.06 + phase * SUN_TAU) * 0.03;

		float dtheta = ang_diff(theta, a);
		float u = dtheta / max(span, 0.001);
		if (abs(u) > 1.4) {
			continue;
		}

		// Every contribution of this site is windowed to zero well before the
		// angular cut-off above.  Without this the cut-off itself shows up as
		// a hard straight edge in the image.
		float window = 1.0 - smoothstep(0.95, 1.38, abs(u));

		// The loop itself: a semicircular arc whose top reaches h above the limb.
		float s = sqrt(max(0.0, 1.0 - min(u * u, 1.0)));
		float r_arc = 1.0 + h * s;

		// Loop-local coordinates for the turbulence field.  These stay in a
		// small numeric range on purpose: a huge coordinate makes the lattice
		// hash lose precision and the noise degenerates into flat slabs.
		vec2 q = vec2(clamp(u, -1.4, 1.4) * 1.6, (r - 1.0) * 5.0);
		vec2 flow = curl2(q * 2.2 + vec2(TIME * 0.13, TIME * 0.07) + fi * 7.31);

		float dr = r - r_arc;
		dr += flow.x * curl_strength * 0.022;
		float uw = u + flow.y * curl_strength * 0.10;

		// The arch must reach the limb, otherwise the loop reads as a wire
		// floating in the halo.  It therefore survives almost to the feet; the
		// window below is what takes it to zero.
		float end_fade = 1.0 - smoothstep(0.95, 1.18, abs(uw));

		// Bright filament: the shell of the loop.  A real prominence is a thin
		// hot core inside a soft diffuse envelope, so the core is narrow and
		// the envelope is wide and faint.  A gentle noise breakup keeps it
		// wispy, but not so deep that the loop falls apart.
		float w = filament_width * (0.55 + 0.9 * (1.0 - s));
		float core = exp(-(dr * dr) / max(w * w, 1e-6));
		float halo = exp(-abs(dr) / max(w * 1.8, 1e-6)) * 0.45;
		float shred = 0.65 + 0.50 * vnoise(vec3(uw * 5.0, r * 4.0, TIME * 0.35 + fi * 5.1));
		float filament = (core + halo) * env * end_fade * window * shred;

		// The curtain hangs under the arch and must not extend past its feet.
		float inside = smoothstep(0.995, 1.02, r) * (1.0 - smoothstep(r_arc - 0.015, r_arc, r));
		inside *= 1.0 - smoothstep(0.62, 1.05, abs(uw));
		float striation = vnoise(vec3(uw * 15.0, r * 2.4, TIME * 0.21 + fi * 3.7));
		float curtain = inside * curtain_strength * env * window
			* smoothstep(0.45, 0.95, striation) * (0.20 + 0.80 * s);

		// Plage: the bright chromospheric footpoints at both ends of the loop.
		// NOTE: GLSL's pow() is undefined for a negative base, so the squares
		// are written out explicitly.
		float feet_axis = (abs(uw) - 1.0) / 0.22;
		float feet = exp(-feet_axis * feet_axis);
		float ring_axis = (r - 1.015) / 0.055;
		float foot = exp(-ring_axis * ring_axis) * feet * env * window;

		acc_filament += prominence_color * filament * brightness;
		acc_curtain += prominence_color * curtain * 0.55 * brightness;
		acc_foot += footpoint_color * foot * brightness;
	}

	vec3 acc = acc_filament + acc_curtain + acc_foot;

	// Chromospheric ring hugging the limb, plus the spicule haze that fades
	// into the corona.  Measured on AIA 304: 0.145 of disk at 1.00R and
	// 0.028 at 1.05R, so the ring is tight and the haze is faint.
	float d_out = max(r - 1.0, 0.0);
	float ring = exp(-d_out / 0.022) * (1.0 - smoothstep(1.08, 1.30, r));
	float haze = exp(-d_out / 0.050) * (1.0 - smoothstep(1.15, 1.55, r));
	vec3 acc_ring = limb_ring_color * (ring * limb_ring_strength + haze * spicule_haze * 0.35);

	if (debug_term == 1) {
		acc = acc_filament;
	} else if (debug_term == 2) {
		acc = acc_curtain;
	} else if (debug_term == 3) {
		acc = acc_foot;
	} else if (debug_term == 4) {
		acc = acc_ring;
	} else if (debug_term == 6) {
		acc = vec3(fract(v_quad.x * 0.2), fract(v_quad.y * 0.2), 0.25);
	} else if (debug_term == 9) {
		acc = vec3(UV.x, UV.y, 0.0);
	} else if (debug_term == 10) {
		acc = vec3(v_quad.x * 0.2 + 0.5, v_quad.y * 0.2 + 0.5, 0.0);
	}

	ALBEDO = acc;
	ALPHA = clamp(max(max(acc.r, acc.g), acc.b), 0.0, 1.0);
}
