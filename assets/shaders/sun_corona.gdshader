shader_type spatial;
render_mode unshaded, blend_add, depth_draw_never, cull_front, shadows_disabled, fog_disabled;

// ---------------------------------------------------------------------------
// Corona and glare, drawn on a CONCENTRIC SPHERE around the photosphere.
//
// This used to be a camera-facing quad.  A quad has edges, and an edge that
// happens to fall inside the frame reads as a straight cut through the glow.
// A concentric shell cannot have that problem.
//
// The radius of every fragment is measured against the photosphere's
// silhouette in WORLD space - the tangent point of the view ray on the
// photosphere.  Measured in normalised device coordinates the same thing was
// off by a wide margin at close range, which opened a dark "moat" between the
// surface and the corona.
//
// The profile is maximal AT the limb and decays monotonically outward, so
// there is never a dip between the surface and the glow.
//
// Reference: SOHO/LASCO C2 shows the corona as narrow radial rays separated
// by darker lanes, with a few broad "helmet" streamers.  SDO AIA 171/193 show
// the inner corona reaching ~0.24 of the disc brightness right at the limb and
// gone by ~1.15 R, with an exponential falloff of ~0.065 R.
// ---------------------------------------------------------------------------

uniform vec3 corona_color : source_color = vec3(1.00, 0.47, 0.18);
uniform vec3 inner_color : source_color = vec3(1.00, 0.62, 0.30);
uniform float brightness : hint_range(0.0, 3.0) = 0.40;
uniform float inner_brightness : hint_range(0.0, 3.0) = 0.20;
uniform float falloff : hint_range(0.01, 1.0) = 0.18;
uniform float streamer_density : hint_range(1.0, 80.0) = 24.0;
uniform float streamer_strength : hint_range(0.0, 2.0) = 0.85;
uniform float helmet_strength : hint_range(0.0, 2.0) = 0.65;
uniform float helmet_density : hint_range(0.5, 20.0) = 4.0;
uniform float speed : hint_range(0.0, 0.5) = 0.02;
uniform float seed = 0.0;
uniform float sun_radius = 1.0;
uniform float shell_radius = 2.2;

varying vec3 v_centre;
varying vec3 v_shell_world;

float hash31(vec3 p) {
	p = fract(p * 0.3183099 + 0.1);
	p *= 17.0;
	return fract(p.x * p.y * p.z * (p.x + p.y + p.z));
}

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

float fbm(vec3 p, int octaves) {
	float sum = 0.0;
	float amp = 0.5;
	float norm = 0.0;
	for (int i = 0; i < 6; i++) {
		if (i >= octaves) {
			break;
		}
		sum += vnoise(p) * amp;
		norm += amp;
		p *= 2.07;
		amp *= 0.5;
	}
	return sum / max(norm, 0.0001);
}

void vertex() {
	v_centre = (MODEL_MATRIX * vec4(0.0, 0.0, 0.0, 1.0)).xyz;
	v_shell_world = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
}

void fragment() {
	// The radius of this fragment against the photosphere's silhouette, in the
	// same projected space.  The silhouette is the tangent point of the view
	// ray on the photosphere.
	vec3 to_camera = CAMERA_POSITION_WORLD - v_centre;
	float axis_len = max(length(to_camera), sun_radius * 1.0001);
	vec3 axis = to_camera / axis_len;
	vec3 offset = v_shell_world - v_centre;
	float along = dot(offset, axis);
	float perp = length(offset - axis * along);

	float alpha = asin(clamp(sun_radius / axis_len, 0.0, 0.999));
	float silhouette_perp = sun_radius * cos(alpha);
	float silhouette_depth = axis_len - sun_radius * sin(alpha);

	float r = (perp / max(axis_len - along, 1e-5))
		/ max(silhouette_perp / max(silhouette_depth, 1e-5), 1e-5);
	float dist = max(r - 1.0, 0.0);

	// The corona's structure is sampled on the shell's own direction, so it is
	// isotropic.  Sampling it in (angle, radius) keeps the noise coherent along
	// a radius, and that draws spokes fanning out of the limb; dithering the
	// angular coordinate instead just turns the spokes into contour rings.
	float streak = smoothstep(0.30, 0.70,
		fbm(offset * 7.0 + vec3(TIME * speed + seed), 3));

	// Broad helmet streamers.
	float helmet = smoothstep(0.36, 0.64,
		fbm(offset * 2.6 + vec3(3.1, 1.7, TIME * speed * 0.5 + seed), 2));

	// The structure is a gentle mottling, not a pattern of rays.
	float shell = mix(1.0, 0.45 + 1.10 * streak, streamer_strength * 0.32);
	shell *= mix(1.0, 0.55 + 0.90 * helmet, helmet_strength * 0.30);

	// One profile, maximal AT the limb and decaying monotonically outward.
	// Both terms are at their maximum at dist = 0, so there is no dip between
	// the surface and the corona and no interior ring.
	float core = exp(-dist / max(falloff, 0.001));
	float halo = pow(clamp(1.0 - dist / max(shell_radius - 1.0, 0.001), 0.0, 1.0), 2.0);
	float outside = step(1.0, r);

	// A last, sub-quantisation dither so the smooth parts of the corona do not
	// band on an 8 bit target.
	float quantisation = (vnoise(vec3(SCREEN_UV * VIEWPORT_SIZE * 0.5, 0.0)) - 0.5) * 0.006;

	vec3 col = (inner_color * (core * inner_brightness)
		+ corona_color * (halo * brightness)) * shell * outside + quantisation;

	ALBEDO = col;
	ALPHA = clamp(max(max(col.r, col.g), col.b), 0.0, 1.0);
}
