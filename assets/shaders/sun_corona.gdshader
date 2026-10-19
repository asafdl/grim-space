shader_type spatial;
render_mode unshaded, blend_add, depth_draw_never, cull_front, shadows_disabled, fog_disabled;

// ---------------------------------------------------------------------------
// Corona and glare, drawn on a CONCENTRIC SPHERE around the photosphere.
//
// This used to be a camera-facing quad.  A quad has edges, and an edge that
// happens to fall inside the frame reads as a straight cut through the glow.
// A concentric sphere cannot have that problem: the profile is a function of
// where the fragment lands on screen relative to the star's centre, so it is
// continuous all the way round and reaches zero long before the shell's own
// silhouette.
//
// The radius is measured in SCREEN space against the star's projected disc
// radius, so the corona sits on the limb exactly at any camera distance.
//
// Reference: SOHO/LASCO C2 shows the corona as narrow radial rays separated
// by darker lanes, with a few broad "helmet" streamers.  SDO AIA 171/193 show
// the inner corona reaching ~0.24 of the disc brightness right at the limb and
// gone by ~1.15 R, with an exponential falloff of ~0.065 R.
// ---------------------------------------------------------------------------

uniform vec3 corona_color : source_color = vec3(1.00, 0.47, 0.18);
uniform vec3 inner_color : source_color = vec3(1.00, 0.62, 0.30);
uniform float brightness : hint_range(0.0, 3.0) = 0.08;
uniform float inner_brightness : hint_range(0.0, 3.0) = 0.20;
uniform float falloff : hint_range(0.01, 1.0) = 0.065;
uniform float streamer_density : hint_range(1.0, 80.0) = 24.0;
uniform float streamer_strength : hint_range(0.0, 2.0) = 0.85;
uniform float helmet_strength : hint_range(0.0, 2.0) = 0.65;
uniform float helmet_density : hint_range(0.5, 20.0) = 4.0;
uniform float speed : hint_range(0.0, 0.5) = 0.02;
uniform float seed = 0.0;
uniform float sun_radius = 1.0;
uniform float shell_radius = 3.0;

varying vec2 v_center_ndc;
varying vec2 v_limb_ndc;

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
	vec3 center = (MODEL_MATRIX * vec4(0.0, 0.0, 0.0, 1.0)).xyz;
	vec3 facing = normalize(CAMERA_POSITION_WORLD - center);
	vec3 side = normalize(cross(facing, vec3(0.0, 1.0, 0.0)));
	vec3 limb = center + side * sun_radius;

	vec4 center_clip = PROJECTION_MATRIX * VIEW_MATRIX * vec4(center, 1.0);
	vec4 limb_clip = PROJECTION_MATRIX * VIEW_MATRIX * vec4(limb, 1.0);
	v_center_ndc = center_clip.xy / center_clip.w;
	v_limb_ndc = limb_clip.xy / limb_clip.w;
}

void fragment() {
	vec2 ndc = SCREEN_UV * 2.0 - 1.0;
	float aspect = VIEWPORT_SIZE.x / max(VIEWPORT_SIZE.y, 1.0);
	vec2 scale = vec2(aspect, 1.0);
	vec2 d = (ndc - v_center_ndc) * scale;
	float limb = max(length((v_limb_ndc - v_center_ndc) * scale), 1e-5);
	float r = length(d) / limb;
	float dist = r - 1.0;
	// Everything is masked off inside the disc rather than branching out.
	float outside = step(1.0, r);
	float theta = atan(d.y, d.x);

	// Narrow radial streamers: high angular frequency, low radial frequency.
	float a = theta / 6.283185307179586;
	float streak = smoothstep(0.32, 0.68,
		fbm(vec3(a * streamer_density, dist * 1.5, TIME * speed + seed), 3));

	// Broad helmet streamers.
	float helmet = smoothstep(0.36, 0.64,
		fbm(vec3(a * helmet_density, 1.7, TIME * speed * 0.5 + seed), 2));

	float shell = mix(1.0, 0.15 + 1.70 * streak, streamer_strength);
	shell *= mix(1.0, 0.30 + 1.50 * helmet, helmet_strength);

	// Inner corona: exponential decay away from the limb.
	float radial = exp(-dist / max(falloff, 0.001));
	float inner = radial * shell * inner_brightness;

	// A long, very gentle lift of the space around the star.  It reaches zero
	// before the shell's own silhouette, so there is no boundary anywhere.
	float lift = pow(clamp(1.0 - dist / max(shell_radius - 1.0, 0.001), 0.0, 1.0), 3.0);
	float outer = lift * brightness;

	vec3 col = (inner_color * inner + corona_color * outer) * outside;
	ALBEDO = col;
	ALPHA = clamp(max(max(col.r, col.g), col.b), 0.0, 1.0);
}
