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
uniform float loop_height : hint_range(0.02, 0.70) = 0.20;
uniform float loop_span : hint_range(0.05, 1.20) = 0.30;
uniform float filament_width : hint_range(0.002, 0.08) = 0.014;
uniform float curl_strength : hint_range(0.0, 1.0) = 0.85;
uniform float curtain_strength : hint_range(0.0, 1.0) = 0.09;
uniform float seed = 0.0;
uniform float sun_radius = 1.0;
uniform float quad_half_width = 2.0;
uniform int debug_term = 0;

// THE ONE ROLE (S2' law, user-ordered 2026-10-22): the casting office
// (SunProminenceCasting.cs / make-sun-audio.py share the law) publishes the
// seeded roll's holder index and its live envelope every frame.  There is
// ONE prominence on the star at a time: gain 0 = all quiet; the envelope
// reaches exactly 0 at each holder's death, so the loop truly disappears.
uniform float role_site = 0.0;
uniform float role_gain = 1.0;

// Coronal rain: condensation blobs sliding back down the legs of a loop.
uniform float rain_amount : hint_range(0.0, 2.0) = 0.0;

// A coronal mass ejection: a shock front lifting off the limb.
// xy = the angle and the progress (0..1), z = the strength, w = the radius.
uniform vec4 cme_data = vec4(0.0, 0.0, 0.0, 0.35);

varying vec2 v_quad;
varying float v_limb;
// Which way the quad's +y axis points ON SCREEN (+1 up, -1 down), measured
// by projecting two vertices.  The site band is pinned with this instead of
// assuming a UV convention, so the arc always rises from the screen-lower
// limb wherever the strategic camera sits.
varying float v_quad_y_screen_sign;

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

	// Screen direction of the billboard's +y axis, measured from the clip
	// coordinates themselves.  +1 means +y is "up" on screen, -1 means it is
	// "down"; the fragment stage pins the prominence site to the lower rim
	// with this, so no UV-convention assumption enters the look.
	vec4 clip_center = PROJECTION_MATRIX * VIEW_MATRIX * vec4(center, 1.0);
	vec4 clip_up = PROJECTION_MATRIX * VIEW_MATRIX * vec4(center + up * scale * 0.05, 1.0);
	vec2 ndc_center = clip_center.xy / clip_center.w;
	vec2 ndc_up = clip_up.xy / clip_up.w;
	v_quad_y_screen_sign = sign(ndc_up.y - ndc_center.y);

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

	// ONE prominence: the rolled holder index, exactly as before, but pulled
	// from the casting office's role rather than from a fixed site list.
	// ONE prominence: the rolled holder index, exactly as before, but pulled
	// from the casting office's role rather than from a fixed site list.
	// When the office does not drive this star (role_gain < 0), the same
	// one-role law runs cycle-internally: one seeded roll of 0..3 per birth
	// (weighted toward 1), the holder's own envelope rising and hanging, then
	// collapsing to exactly 0, with a quiet tail as the all-quiet gap.
	float fi = clamp(role_site, 0.0, 3.0);
	float env = role_gain;
	if (role_gain < 0.0) {
		float cycle = 13.0;
		float birth = floor(TIME / cycle);
		float cycle_u = fract(TIME / cycle);
		env = smoothstep(0.0, 0.16, cycle_u) * (1.0 - smoothstep(0.42, 0.92, cycle_u));
		float roll = hash11(birth * 13.77 + seed);
		fi = roll < 0.26 ? 0.0 : (roll < 0.70 ? 1.0 : (roll < 0.88 ? 2.0 : 3.0));
	}

	float h1 = hash11(fi * 17.13 + seed);
	float h2 = hash11(fi * 31.71 + seed + 5.3);
	float h3 = hash11(fi * 47.33 + seed + 11.9);

	// The site angle is seeded per holder and pinned to the screen-LOWER half
	// of the billboard rim (v_quad_y_screen_sign is measured per frame), so
	// the arc apex is in frame wherever a strategic camera sees the disc's
	// lower limb — including the opening pose, which crops the top limb
	// off-screen.  Randomness, one-roll law and per-holder identity are
	// unchanged; only the site's angular band is pinned.
	float lower_y_sign = -v_quad_y_screen_sign;
	float base_angle = lower_y_sign > 0.0
		? mix(-0.35, SUN_PI + 0.35, h1)
		: mix(SUN_PI + 0.35, SUN_TAU - 0.35, h1);
	float phase = h2;
	float height_scale = 0.45 + 0.85 * h3;
	float span_scale = 0.55 + 0.80 * fract(h1 * 13.37 + h2);

	// The life: owned by the casting office — start time, death time, the
	// dissolve in and out (reversed at the death, ending exactly 0).

	// S2: the loops must read lit FROM the star.  The plasma is brightest
	// deep in the chromosphere at the limb and dims as the arc rises — a
	// height-facing energy term shared by the thread, the curtain and the
	// feet, floored at 0.55 so an erupting crown still glows.
	float lift = exp(-max(r - 1.0, 0.0) / 0.075);
	float light_facing = mix(0.55, 1.0, lift);

	vec3 acc_filament = vec3(0.0);
	vec3 acc_curtain = vec3(0.0);
	vec3 acc_foot = vec3(0.0);

	if (env > 0.001) {
		float h = loop_height * height_scale * env;
		float span = loop_span * span_scale;
		float a = base_angle + sin(TIME * 0.06 + phase * SUN_TAU) * 0.03;

		float dtheta = ang_diff(theta, a);
		float u = dtheta / max(span, 0.001);

		// Every contribution of this role is windowed to zero well before the
		// angular cut-off above.  Without this the cut-off itself shows up as
		// a hard straight edge in the image.
		if (abs(u) <= 1.4) {
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

			// The filament is a bundle of thin threads, not a solid tube.  The
			// noise is stretched ALONG the loop (low frequency in the along
			// direction, high frequency across it) so the plasma breaks into
			// parallel fibrils, and a slow braid slides them across each other.
			float w = filament_width * (0.55 + 0.9 * (1.0 - s));
			float tube = exp(-(dr * dr) / max(w * w, 1e-6));
			float braid = sin(uw * 5.3 + TIME * 0.7 + fi * 2.1) * 0.32;
			float across = (dr + braid * w) / max(w, 1e-6);
			// The fibril frequency along the arc is what makes the bundle READ
			// as threads instead of one smooth wire (S2).
			float fibre = vnoise(vec3(uw * 5.0, across * 3.1, TIME * 0.55 + fi * 5.1));
			float fibres = 0.18 + 1.10 * smoothstep(0.28, 0.72, fibre);
			float halo = exp(-abs(dr) / max(w * 2.0, 1e-6)) * 0.32;
			float filament = (tube * fibres + halo) * env * end_fade * window;

			// Coronal rain: bright condensation blobs running back down the legs
			// of the loop while the eruption is collapsing.  Gated on a live
			// role: rain over an all-quiet limb has no loop to fall from.
			if (rain_amount > 0.0) {
				float arc_len = span * 1.25;
				float rain_phase = fract(TIME * 0.55 + fi * 0.37);
				float along = uw * arc_len;
				float rain_along = sign(uw) * (1.0 - rain_phase) * arc_len;
				float rain_u = rain_along / max(arc_len, 0.001);
				float rain_r = 1.0 + h * sqrt(max(0.0, 1.0 - min(rain_u * rain_u, 1.0)));
				float rain_d = length(vec2(along - rain_along, (r - rain_r) * 3.4));
				float rain = exp(-rain_d * rain_d * 70.0) * rain_amount;
				filament += rain * 1.6;
			}

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

			acc_filament = prominence_color * filament * brightness * light_facing;
			acc_curtain = prominence_color * curtain * 0.55 * brightness * light_facing;
			acc_foot = footpoint_color * foot * brightness * light_facing;
		}
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
