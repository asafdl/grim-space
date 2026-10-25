shader_type sky;

uniform sampler2D stars_panorama : filter_linear_mipmap, source_color;
uniform sampler2D nebula_panorama : filter_linear_mipmap, source_color;
uniform sampler2D horizon_panorama : filter_linear_mipmap, source_color;
uniform float energy : hint_range(0.0, 2.0) = 0.26;
uniform float nebula_strength : hint_range(0.0, 2.0) = 0.45;
uniform vec3 nebula_center = vec3(0.764, -0.404, -0.503);
uniform float nebula_radius : hint_range(0.05, 0.8) = 0.30;
uniform float nebula_aspect : hint_range(0.5, 2.5) = 1.071;
uniform float horizon_strength : hint_range(0.0, 2.0) = 0.36;
uniform vec3 horizon_center = vec3(-0.62, -0.38, -0.52);
uniform float horizon_radius : hint_range(0.05, 0.8) = 0.12;
uniform float horizon_aspect : hint_range(0.5, 2.5) = 1.778;
uniform vec2 horizon_uv_flip = vec2(-1.0, -1.0);

vec2 panorama_uv(vec3 eyedir) {
	vec2 uv = vec2(atan(eyedir.x, eyedir.z), acos(clamp(-eyedir.y, -1.0, 1.0)));
	uv /= vec2(TAU, PI);
	return vec2(fract(uv.x + 0.5), uv.y);
}

float hash(vec2 point) {
	return fract(sin(dot(point, vec2(127.1, 311.7))) * 43758.5453);
}

float noise(vec2 point) {
	vec2 cell = floor(point);
	vec2 offset = fract(point);
	offset = offset * offset * (3.0 - 2.0 * offset);

	return mix(
		mix(hash(cell), hash(cell + vec2(1.0, 0.0)), offset.x),
		mix(hash(cell + vec2(0.0, 1.0)), hash(cell + vec2(1.0)), offset.x),
		offset.y);
}

vec3 panorama_patch(
	vec3 eyedir,
	sampler2D panorama,
	vec3 patch_center,
	float patch_radius,
	float patch_aspect,
	vec2 uv_flip) {
	vec3 center = normalize(patch_center);
	vec3 up_hint = abs(center.y) < 0.99 ? vec3(0.0, 1.0, 0.0) : vec3(1.0, 0.0, 0.0);
	vec3 right = normalize(cross(up_hint, center));
	vec3 patch_up = cross(center, right);

	vec3 dir = normalize(eyedir);
	float alignment = dot(dir, center);
	if (alignment <= 0.0) {
		return vec3(0.0);
	}

	float half_height = tan(patch_radius);
	float half_width = half_height * patch_aspect;
	vec2 projected = vec2(dot(dir, right), dot(dir, patch_up)) / alignment;
	vec2 uv = projected / (2.0 * vec2(half_width, half_height)) + 0.5;
	uv = (uv - 0.5) * uv_flip + 0.5;
	if (uv.x <= 0.0 || uv.x >= 1.0 || uv.y <= 0.0 || uv.y >= 1.0) {
		return vec3(0.0);
	}

	vec3 sample_rgb = texture(panorama, uv).rgb;
	float lum = dot(sample_rgb, vec3(0.299, 0.587, 0.114));
	float gas = smoothstep(0.01, 0.08, lum);
	vec2 centered_uv = (uv - 0.5) * 2.0;
	float breakup = noise(uv * 5.0) * 0.16 + noise(uv * 11.0) * 0.06;
	float edge_mask = 1.0 - smoothstep(0.30 + breakup, 0.96 + breakup, length(centered_uv));
	return sample_rgb * gas * edge_mask;
}

void sky() {
	vec3 stars = texture(stars_panorama, panorama_uv(EYEDIR)).rgb * energy;
	vec3 nebula = panorama_patch(EYEDIR, nebula_panorama, nebula_center, nebula_radius, nebula_aspect, vec2(1.0)) * nebula_strength;
	vec3 horizon = panorama_patch(EYEDIR, horizon_panorama, horizon_center, horizon_radius, horizon_aspect, horizon_uv_flip) * horizon_strength;
	COLOR = stars + nebula + horizon;
}
