shader_type spatial;
render_mode unshaded, blend_add, depth_draw_never, cull_disabled, shadows_disabled, fog_disabled;

// ---------------------------------------------------------------------------
// Corona: the streamer halo above the photosphere.
//
// Reference: SOHO/LASCO C2 (2 - 4 R_sun field of view) shows the corona as
// narrow radial rays separated by darker inter-streamer lanes, with a few broad
// "helmet" streamers.  SDO AIA 171/193 show the inner corona reaching ~0.24
// (171) and ~0.12 (193) of the disk brightness right at the limb, gone by
// ~1.15 R.
//
// The streamers are sampled as high angular frequency / low radial frequency
// noise, which is what makes them read as radially elongated rays rather than
// as blobs.
// ---------------------------------------------------------------------------

uniform vec3 corona_color : source_color = vec3(1.00, 0.47, 0.18);
uniform float brightness : hint_range(0.0, 3.0) = 0.05;
uniform float falloff : hint_range(0.01, 1.0) = 0.065;
uniform float streamer_density : hint_range(1.0, 80.0) = 24.0;
uniform float streamer_strength : hint_range(0.0, 2.0) = 0.85;
uniform float helmet_strength : hint_range(0.0, 2.0) = 0.65;
uniform float helmet_density : hint_range(0.5, 20.0) = 4.0;
uniform float speed : hint_range(0.0, 0.5) = 0.02;
uniform float seed = 0.0;
uniform float sun_radius = 1.0;
uniform float quad_half_width = 3.0;

varying vec2 v_quad;
varying float v_limb;

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
	vec3 center = MODEL_MATRIX[3].xyz;
	vec3 facing = normalize(CAMERA_POSITION_WORLD - center);
	vec3 up_hint = INV_VIEW_MATRIX[1].xyz;
	vec3 right = normalize(cross(up_hint, facing));
	vec3 up = cross(facing, right);
	float scale = length(MODEL_MATRIX[0].xyz);
	MODELVIEW_MATRIX = VIEW_MATRIX * mat4(
		vec4(right * scale, 0.0), vec4(up * scale, 0.0),
		vec4(facing * scale, 0.0), vec4(center, 1.0));

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
	float d = max(r - 1.0, 0.0);

	// Inner corona: exponential decay away from the limb.
	float radial = exp(-d / max(falloff, 0.001));

	// Narrow radial streamers: high angular frequency, low radial frequency.
	float a = theta / 6.283185307179586;
	float streak = smoothstep(0.32, 0.68,
		fbm(vec3(a * streamer_density, d * 1.5, TIME * speed + seed), 3));

	// Broad helmet streamers.
	float helmet = smoothstep(0.36, 0.64,
		fbm(vec3(a * helmet_density, 1.7, TIME * speed * 0.5 + seed), 2));

	float shell = mix(1.0, 0.15 + 1.70 * streak, streamer_strength);
	shell *= mix(1.0, 0.30 + 1.50 * helmet, helmet_strength);

	float intensity = radial * shell * brightness;
	// Nothing left once we are well outside the streamer region.
	intensity *= 1.0 - smoothstep(1.35, 2.10, r);
	intensity *= smoothstep(0.985, 1.03, r);

	ALBEDO = corona_color;
	ALPHA = clamp(intensity, 0.0, 1.0);
}
