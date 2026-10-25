shader_type sky;

uniform sampler2D stars_panorama : filter_linear_mipmap, source_color;
uniform sampler2D nebula_panorama : filter_linear_mipmap, source_color;
uniform sampler2D horizon_panorama : filter_linear_mipmap, source_color;
uniform float energy : hint_range(0.0, 2.0) = 0.26;
uniform float nebula_strength : hint_range(0.0, 2.0) = 0.45;
uniform float nebula_gas_start : hint_range(0.0, 0.1) = 0.01;
uniform float nebula_gas_end : hint_range(0.01, 0.2) = 0.08;
uniform float nebula_mip_level : hint_range(0.0, 5.0) = 0.0;
uniform bool nebula_organic_edges = false;
uniform vec3 nebula_tint_color : source_color = vec3(0.9, 0.42, 0.28);
uniform vec3 nebula_tint_color_2 : source_color = vec3(0.9, 0.42, 0.28);
uniform vec3 nebula_tint_color_3 : source_color = vec3(0.9, 0.42, 0.28);
uniform vec3 nebula_tint_color_4 : source_color = vec3(0.9, 0.42, 0.28);
uniform vec3 nebula_tint_color_5 : source_color = vec3(0.9, 0.42, 0.28);
uniform vec3 nebula_tint_color_6 : source_color = vec3(0.9, 0.42, 0.28);
uniform vec3 nebula_tint_color_7 : source_color = vec3(0.9, 0.42, 0.28);
uniform vec3 nebula_tint_color_8 : source_color = vec3(0.9, 0.42, 0.28);
uniform vec3 nebula_tint_color_9 : source_color = vec3(0.9, 0.42, 0.28);
uniform vec3 nebula_tint_color_10 : source_color = vec3(0.9, 0.42, 0.28);
uniform vec3 nebula_tint_color_11 : source_color = vec3(0.9, 0.42, 0.28);
uniform vec3 nebula_tint_color_12 : source_color = vec3(0.9, 0.42, 0.28);
uniform vec3 nebula_tint_color_13 : source_color = vec3(0.9, 0.42, 0.28);
uniform vec3 nebula_tint_color_14 : source_color = vec3(0.9, 0.42, 0.28);
uniform vec3 nebula_tint_color_15 : source_color = vec3(0.9, 0.42, 0.28);
uniform vec3 nebula_tint_color_16 : source_color = vec3(0.9, 0.42, 0.28);
uniform vec3 nebula_tint_color_17 : source_color = vec3(0.9, 0.42, 0.28);
uniform vec3 nebula_tint_color_18 : source_color = vec3(0.9, 0.42, 0.28);
uniform vec3 nebula_tint_color_19 : source_color = vec3(0.9, 0.42, 0.28);
uniform vec3 nebula_tint_color_20 : source_color = vec3(0.9, 0.42, 0.28);
uniform vec3 nebula_tint_color_21 : source_color = vec3(0.9, 0.42, 0.28);
uniform vec3 nebula_tint_color_22 : source_color = vec3(0.9, 0.42, 0.28);
uniform vec3 nebula_tint_color_23 : source_color = vec3(0.9, 0.42, 0.28);
uniform vec3 nebula_tint_color_24 : source_color = vec3(0.9, 0.42, 0.28);
uniform vec3 nebula_tint_color_25 : source_color = vec3(0.9, 0.42, 0.28);
uniform vec3 nebula_tint_color_26 : source_color = vec3(0.9, 0.42, 0.28);
uniform vec3 nebula_tint_color_27 : source_color = vec3(0.9, 0.42, 0.28);
uniform vec3 nebula_tint_color_28 : source_color = vec3(0.9, 0.42, 0.28);
uniform vec3 nebula_tint_color_29 : source_color = vec3(0.9, 0.42, 0.28);
uniform vec3 nebula_tint_color_30 : source_color = vec3(0.9, 0.42, 0.28);
uniform float nebula_tint_strength : hint_range(0.0, 1.0) = 0.0;
uniform float nebula_layer_2_strength = 0.0;
uniform float nebula_layer_3_strength = 0.0;
uniform float nebula_layer_4_strength = 0.0;
uniform float nebula_layer_5_strength = 0.0;
uniform float nebula_layer_6_strength = 0.0;
uniform float nebula_layer_7_strength = 0.0;
uniform float nebula_layer_8_strength = 0.0;
uniform float nebula_layer_9_strength = 0.0;
uniform float nebula_layer_10_strength = 0.0;
uniform float nebula_layer_11_strength = 0.0;
uniform float nebula_layer_12_strength = 0.0;
uniform float nebula_layer_13_strength = 0.0;
uniform float nebula_layer_14_strength = 0.0;
uniform float nebula_layer_15_strength = 0.0;
uniform float nebula_layer_16_strength = 0.0;
uniform float nebula_layer_17_strength = 0.0;
uniform float nebula_layer_18_strength = 0.0;
uniform float nebula_layer_19_strength = 0.0;
uniform float nebula_layer_20_strength = 0.0;
uniform float nebula_layer_21_strength = 0.0;
uniform float nebula_layer_22_strength = 0.0;
uniform float nebula_layer_23_strength = 0.0;
uniform float nebula_layer_24_strength = 0.0;
uniform float nebula_layer_25_strength = 0.0;
uniform float nebula_layer_26_strength = 0.0;
uniform float nebula_layer_27_strength = 0.0;
uniform float nebula_layer_28_strength = 0.0;
uniform float nebula_layer_29_strength = 0.0;
uniform float nebula_layer_30_strength = 0.0;
uniform vec3 nebula_center = vec3(0.764, -0.404, -0.503);
uniform vec3 nebula_center_2 = vec3(0.764, -0.404, -0.503);
uniform vec3 nebula_center_3 = vec3(0.764, -0.404, -0.503);
uniform vec3 nebula_center_4 = vec3(0.764, -0.404, -0.503);
uniform vec3 nebula_center_5 = vec3(0.764, -0.404, -0.503);
uniform vec3 nebula_center_6 = vec3(0.764, -0.404, -0.503);
uniform vec3 nebula_center_7 = vec3(0.764, -0.404, -0.503);
uniform vec3 nebula_center_8 = vec3(0.764, -0.404, -0.503);
uniform vec3 nebula_center_9 = vec3(0.764, -0.404, -0.503);
uniform vec3 nebula_center_10 = vec3(0.764, -0.404, -0.503);
uniform vec3 nebula_center_11 = vec3(0.764, -0.404, -0.503);
uniform vec3 nebula_center_12 = vec3(0.764, -0.404, -0.503);
uniform vec3 nebula_center_13 = vec3(0.764, -0.404, -0.503);
uniform vec3 nebula_center_14 = vec3(0.764, -0.404, -0.503);
uniform vec3 nebula_center_15 = vec3(0.764, -0.404, -0.503);
uniform vec3 nebula_center_16 = vec3(0.764, -0.404, -0.503);
uniform vec3 nebula_center_17 = vec3(0.764, -0.404, -0.503);
uniform vec3 nebula_center_18 = vec3(0.764, -0.404, -0.503);
uniform vec3 nebula_center_19 = vec3(0.764, -0.404, -0.503);
uniform vec3 nebula_center_20 = vec3(0.764, -0.404, -0.503);
uniform vec3 nebula_center_21 = vec3(0.764, -0.404, -0.503);
uniform vec3 nebula_center_22 = vec3(0.764, -0.404, -0.503);
uniform vec3 nebula_center_23 = vec3(0.764, -0.404, -0.503);
uniform vec3 nebula_center_24 = vec3(0.764, -0.404, -0.503);
uniform vec3 nebula_center_25 = vec3(0.764, -0.404, -0.503);
uniform vec3 nebula_center_26 = vec3(0.764, -0.404, -0.503);
uniform vec3 nebula_center_27 = vec3(0.764, -0.404, -0.503);
uniform vec3 nebula_center_28 = vec3(0.764, -0.404, -0.503);
uniform vec3 nebula_center_29 = vec3(0.764, -0.404, -0.503);
uniform vec3 nebula_center_30 = vec3(0.764, -0.404, -0.503);
uniform float nebula_radius : hint_range(0.05, 0.8) = 0.30;
uniform float nebula_radius_2 : hint_range(0.05, 0.8) = 0.30;
uniform float nebula_radius_3 : hint_range(0.05, 0.8) = 0.30;
uniform float nebula_radius_4 : hint_range(0.05, 0.8) = 0.30;
uniform float nebula_radius_5 : hint_range(0.05, 0.8) = 0.30;
uniform float nebula_radius_6 : hint_range(0.05, 0.8) = 0.30;
uniform float nebula_radius_7 : hint_range(0.05, 0.8) = 0.30;
uniform float nebula_radius_8 : hint_range(0.05, 0.8) = 0.30;
uniform float nebula_radius_9 : hint_range(0.05, 0.8) = 0.30;
uniform float nebula_radius_10 : hint_range(0.05, 0.8) = 0.30;
uniform float nebula_radius_11 : hint_range(0.05, 0.8) = 0.30;
uniform float nebula_radius_12 : hint_range(0.05, 0.8) = 0.30;
uniform float nebula_radius_13 : hint_range(0.05, 0.8) = 0.30;
uniform float nebula_radius_14 : hint_range(0.05, 0.8) = 0.30;
uniform float nebula_radius_15 : hint_range(0.05, 0.8) = 0.30;
uniform float nebula_radius_16 : hint_range(0.05, 0.8) = 0.30;
uniform float nebula_radius_17 : hint_range(0.05, 0.8) = 0.30;
uniform float nebula_radius_18 : hint_range(0.05, 0.8) = 0.30;
uniform float nebula_radius_19 : hint_range(0.05, 0.8) = 0.30;
uniform float nebula_radius_20 : hint_range(0.05, 0.8) = 0.30;
uniform float nebula_radius_21 : hint_range(0.05, 0.8) = 0.30;
uniform float nebula_radius_22 : hint_range(0.05, 0.8) = 0.30;
uniform float nebula_radius_23 : hint_range(0.05, 0.8) = 0.30;
uniform float nebula_radius_24 : hint_range(0.05, 0.8) = 0.30;
uniform float nebula_radius_25 : hint_range(0.05, 0.8) = 0.30;
uniform float nebula_radius_26 : hint_range(0.05, 0.8) = 0.30;
uniform float nebula_radius_27 : hint_range(0.05, 0.8) = 0.30;
uniform float nebula_radius_28 : hint_range(0.05, 0.8) = 0.30;
uniform float nebula_radius_29 : hint_range(0.05, 0.8) = 0.30;
uniform float nebula_radius_30 : hint_range(0.05, 0.8) = 0.30;
uniform float nebula_aspect : hint_range(0.5, 8.0) = 1.071;
uniform float nebula_aspect_2 : hint_range(0.5, 8.0) = 1.071;
uniform float nebula_aspect_3 : hint_range(0.5, 8.0) = 1.071;
uniform float nebula_aspect_4 : hint_range(0.5, 8.0) = 1.071;
uniform float nebula_aspect_5 : hint_range(0.5, 8.0) = 1.071;
uniform float nebula_aspect_6 : hint_range(0.5, 8.0) = 1.071;
uniform float nebula_aspect_7 : hint_range(0.5, 8.0) = 1.071;
uniform float nebula_aspect_8 : hint_range(0.5, 8.0) = 1.071;
uniform float nebula_aspect_9 : hint_range(0.5, 8.0) = 1.071;
uniform float nebula_aspect_10 : hint_range(0.5, 8.0) = 1.071;
uniform float nebula_aspect_11 : hint_range(0.5, 8.0) = 1.071;
uniform float nebula_aspect_12 : hint_range(0.5, 8.0) = 1.071;
uniform float nebula_aspect_13 : hint_range(0.5, 8.0) = 1.071;
uniform float nebula_aspect_14 : hint_range(0.5, 8.0) = 1.071;
uniform float nebula_aspect_15 : hint_range(0.5, 8.0) = 1.071;
uniform float nebula_aspect_16 : hint_range(0.5, 8.0) = 1.071;
uniform float nebula_aspect_17 : hint_range(0.5, 8.0) = 1.071;
uniform float nebula_aspect_18 : hint_range(0.5, 8.0) = 1.071;
uniform float nebula_aspect_19 : hint_range(0.5, 8.0) = 1.071;
uniform float nebula_aspect_20 : hint_range(0.5, 8.0) = 1.071;
uniform float nebula_aspect_21 : hint_range(0.5, 8.0) = 1.071;
uniform float nebula_aspect_22 : hint_range(0.5, 8.0) = 1.071;
uniform float nebula_aspect_23 : hint_range(0.5, 8.0) = 1.071;
uniform float nebula_aspect_24 : hint_range(0.5, 8.0) = 1.071;
uniform float nebula_aspect_25 : hint_range(0.5, 8.0) = 1.071;
uniform float nebula_aspect_26 : hint_range(0.5, 8.0) = 1.071;
uniform float nebula_aspect_27 : hint_range(0.5, 8.0) = 1.071;
uniform float nebula_aspect_28 : hint_range(0.5, 8.0) = 1.071;
uniform float nebula_aspect_29 : hint_range(0.5, 8.0) = 1.071;
uniform float nebula_aspect_30 : hint_range(0.5, 8.0) = 1.071;
uniform vec4 nebula_region = vec4(0.0, 0.0, 1.0, 1.0);
uniform vec4 nebula_region_2 = vec4(0.0, 0.0, 1.0, 1.0);
uniform vec4 nebula_region_3 = vec4(0.0, 0.0, 1.0, 1.0);
uniform vec4 nebula_region_4 = vec4(0.0, 0.0, 1.0, 1.0);
uniform vec4 nebula_region_5 = vec4(0.0, 0.0, 1.0, 1.0);
uniform vec4 nebula_region_6 = vec4(0.0, 0.0, 1.0, 1.0);
uniform vec4 nebula_region_7 = vec4(0.0, 0.0, 1.0, 1.0);
uniform vec4 nebula_region_8 = vec4(0.0, 0.0, 1.0, 1.0);
uniform vec4 nebula_region_9 = vec4(0.0, 0.0, 1.0, 1.0);
uniform vec4 nebula_region_10 = vec4(0.0, 0.0, 1.0, 1.0);
uniform vec4 nebula_region_11 = vec4(0.0, 0.0, 1.0, 1.0);
uniform vec4 nebula_region_12 = vec4(0.0, 0.0, 1.0, 1.0);
uniform vec4 nebula_region_13 = vec4(0.0, 0.0, 1.0, 1.0);
uniform vec4 nebula_region_14 = vec4(0.0, 0.0, 1.0, 1.0);
uniform vec4 nebula_region_15 = vec4(0.0, 0.0, 1.0, 1.0);
uniform vec4 nebula_region_16 = vec4(0.0, 0.0, 1.0, 1.0);
uniform vec4 nebula_region_17 = vec4(0.0, 0.0, 1.0, 1.0);
uniform vec4 nebula_region_18 = vec4(0.0, 0.0, 1.0, 1.0);
uniform vec4 nebula_region_19 = vec4(0.0, 0.0, 1.0, 1.0);
uniform vec4 nebula_region_20 = vec4(0.0, 0.0, 1.0, 1.0);
uniform vec4 nebula_region_21 = vec4(0.0, 0.0, 1.0, 1.0);
uniform vec4 nebula_region_22 = vec4(0.0, 0.0, 1.0, 1.0);
uniform vec4 nebula_region_23 = vec4(0.0, 0.0, 1.0, 1.0);
uniform vec4 nebula_region_24 = vec4(0.0, 0.0, 1.0, 1.0);
uniform vec4 nebula_region_25 = vec4(0.0, 0.0, 1.0, 1.0);
uniform vec4 nebula_region_26 = vec4(0.0, 0.0, 1.0, 1.0);
uniform vec4 nebula_region_27 = vec4(0.0, 0.0, 1.0, 1.0);
uniform vec4 nebula_region_28 = vec4(0.0, 0.0, 1.0, 1.0);
uniform vec4 nebula_region_29 = vec4(0.0, 0.0, 1.0, 1.0);
uniform vec4 nebula_region_30 = vec4(0.0, 0.0, 1.0, 1.0);
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

vec3 nebula_palette_color(int index) {
	if (index == 0) {
		return vec3(0.3, 0.68, 1.0);
	}
	if (index == 1) {
		return vec3(1.0, 0.42, 0.38);
	}
	if (index == 2) {
		return vec3(0.55, 0.4, 1.0);
	}
	if (index == 3) {
		return vec3(0.25, 0.78, 0.82);
	}
	if (index == 4) {
		return vec3(1.0, 0.64, 0.3);
	}
	return vec3(0.78, 0.38, 0.72);
}

vec3 panorama_patch(
	vec3 eyedir,
	sampler2D panorama,
	vec3 patch_center,
	float patch_radius,
	float patch_aspect,
	vec2 uv_flip,
	vec4 texture_region,
	vec3 tint_color,
	float tint_strength) {
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
	if (!nebula_organic_edges
		&& (uv.x <= 0.0 || uv.x >= 1.0 || uv.y <= 0.0 || uv.y >= 1.0)) {
		return vec3(0.0);
	}

	vec2 texture_uv = texture_region.xy + clamp(uv, vec2(0.0), vec2(1.0)) * texture_region.zw;
	vec3 sample_rgb = textureLod(panorama, texture_uv, nebula_mip_level).rgb;
	float lum = dot(sample_rgb, vec3(0.299, 0.587, 0.114));
	float gas = smoothstep(nebula_gas_start, nebula_gas_end, lum);
	float edge_mask;
	if (nebula_organic_edges) {
		float shape_noise =
			(noise(uv * vec2(3.0, 7.0)) - 0.5) * 0.28
			+ (noise(uv * vec2(8.0, 15.0)) - 0.5) * 0.12;
		float radius = length((uv - 0.5) * 2.0);
		float cloud_shape = 1.0 - smoothstep(0.65 + shape_noise, 1.08 + shape_noise, radius);
		float edge_distance = min(
			min(uv.x, 1.0 - uv.x),
			min(uv.y, 1.0 - uv.y));
		float edge_noise = (noise(uv * vec2(13.0, 19.0)) - 0.5) * 0.08;
		float boundary_fade = smoothstep(0.0, 0.22, max(edge_distance + edge_noise - 0.04, 0.0));
		edge_mask = cloud_shape * boundary_fade;
	} else {
		float edge_distance = min(
			min(uv.x, 1.0 - uv.x),
			min(uv.y, 1.0 - uv.y));
		edge_mask = smoothstep(0.0, 0.12, edge_distance);
	}
	vec3 tinted_rgb = mix(sample_rgb, lum * tint_color, tint_strength);
	return tinted_rgb * gas * edge_mask;
}

void sky() {
	vec3 stars = texture(stars_panorama, panorama_uv(EYEDIR)).rgb * energy;
	vec3 nebula = panorama_patch(
		EYEDIR,
		nebula_panorama,
		nebula_center,
		nebula_radius,
		nebula_aspect,
		vec2(1.0),
		nebula_region,
		nebula_tint_color,
		nebula_tint_strength)
		+ panorama_patch(
			EYEDIR,
			nebula_panorama,
			nebula_center_2,
			nebula_radius_2,
			nebula_aspect_2,
			vec2(1.0),
			nebula_region_2,
			nebula_tint_color_2,
			nebula_tint_strength) * nebula_layer_2_strength
		+ panorama_patch(
			EYEDIR,
			nebula_panorama,
			nebula_center_3,
			nebula_radius_3,
			nebula_aspect_3,
			vec2(1.0),
			nebula_region_3,
			nebula_tint_color_3,
			nebula_tint_strength) * nebula_layer_3_strength
		+ panorama_patch(
			EYEDIR,
			nebula_panorama,
			nebula_center_4,
			nebula_radius_4,
			nebula_aspect_4,
			vec2(1.0),
			nebula_region_4,
			nebula_tint_color_4,
			nebula_tint_strength) * nebula_layer_4_strength
		+ panorama_patch(
			EYEDIR,
			nebula_panorama,
			nebula_center_5,
			nebula_radius_5,
			nebula_aspect_5,
			vec2(1.0),
			nebula_region_5,
			nebula_tint_color_5,
			nebula_tint_strength) * nebula_layer_5_strength
		+ panorama_patch(
			EYEDIR,
			nebula_panorama,
			nebula_center_6,
			nebula_radius_6,
			nebula_aspect_6,
			vec2(1.0),
			nebula_region_6,
			nebula_tint_color_6,
			nebula_tint_strength) * nebula_layer_6_strength
		+ panorama_patch(
			EYEDIR,
			nebula_panorama,
			nebula_center_7,
			nebula_radius_7,
			nebula_aspect_7,
			vec2(1.0),
			nebula_region_7,
			nebula_tint_color_7,
			nebula_tint_strength) * nebula_layer_7_strength
		+ panorama_patch(
			EYEDIR,
			nebula_panorama,
			nebula_center_8,
			nebula_radius_8,
			nebula_aspect_8,
			vec2(1.0),
			nebula_region_8,
			nebula_tint_color_8,
			nebula_tint_strength) * nebula_layer_8_strength
		+ panorama_patch(
			EYEDIR,
			nebula_panorama,
			nebula_center_9,
			nebula_radius_9,
			nebula_aspect_9,
			vec2(1.0),
			nebula_region_9,
			nebula_tint_color_9,
			nebula_tint_strength) * nebula_layer_9_strength
		+ panorama_patch(
			EYEDIR,
			nebula_panorama,
			nebula_center_10,
			nebula_radius_10,
			nebula_aspect_10,
			vec2(1.0),
			nebula_region_10,
			nebula_tint_color_10,
			nebula_tint_strength) * nebula_layer_10_strength
		+ panorama_patch(
			EYEDIR,
			nebula_panorama,
			nebula_center_11,
			nebula_radius_11,
			nebula_aspect_11,
			vec2(1.0),
			nebula_region_11,
			nebula_tint_color_11,
			nebula_tint_strength) * nebula_layer_11_strength
		+ panorama_patch(
			EYEDIR,
			nebula_panorama,
			nebula_center_12,
			nebula_radius_12,
			nebula_aspect_12,
			vec2(1.0),
			nebula_region_12,
			nebula_tint_color_12,
			nebula_tint_strength) * nebula_layer_12_strength
		+ panorama_patch(
			EYEDIR,
			nebula_panorama,
			nebula_center_13,
			nebula_radius_13,
			nebula_aspect_13,
			vec2(1.0),
			nebula_region_13,
			nebula_tint_color_13,
			nebula_tint_strength) * nebula_layer_13_strength
		+ panorama_patch(
			EYEDIR,
			nebula_panorama,
			nebula_center_14,
			nebula_radius_14,
			nebula_aspect_14,
			vec2(1.0),
			nebula_region_14,
			nebula_tint_color_14,
			nebula_tint_strength) * nebula_layer_14_strength
		+ panorama_patch(
			EYEDIR,
			nebula_panorama,
			nebula_center_15,
			nebula_radius_15,
			nebula_aspect_15,
			vec2(1.0),
			nebula_region_15,
			nebula_tint_color_15,
			nebula_tint_strength) * nebula_layer_15_strength
		+ panorama_patch(
			EYEDIR,
			nebula_panorama,
			nebula_center_16,
			nebula_radius_16,
			nebula_aspect_16,
			vec2(1.0),
			nebula_region_16,
			nebula_tint_color_16,
			nebula_tint_strength) * nebula_layer_16_strength
		+ panorama_patch(
			EYEDIR,
			nebula_panorama,
			nebula_center_17,
			nebula_radius_17,
			nebula_aspect_17,
			vec2(1.0),
			nebula_region_17,
			nebula_tint_color_17,
			nebula_tint_strength) * nebula_layer_17_strength
		+ panorama_patch(
			EYEDIR,
			nebula_panorama,
			nebula_center_18,
			nebula_radius_18,
			nebula_aspect_18,
			vec2(1.0),
			nebula_region_18,
			nebula_tint_color_18,
			nebula_tint_strength) * nebula_layer_18_strength
		+ panorama_patch(
			EYEDIR,
			nebula_panorama,
			nebula_center_19,
			nebula_radius_19,
			nebula_aspect_19,
			vec2(1.0),
			nebula_region_19,
			nebula_tint_color_19,
			nebula_tint_strength) * nebula_layer_19_strength
		+ panorama_patch(
			EYEDIR,
			nebula_panorama,
			nebula_center_20,
			nebula_radius_20,
			nebula_aspect_20,
			vec2(1.0),
			nebula_region_20,
			nebula_tint_color_20,
			nebula_tint_strength) * nebula_layer_20_strength
		+ panorama_patch(
			EYEDIR,
			nebula_panorama,
			nebula_center_21,
			nebula_radius_21,
			nebula_aspect_21,
			vec2(1.0),
			nebula_region_21,
			nebula_tint_color_21,
			nebula_tint_strength) * nebula_layer_21_strength
		+ panorama_patch(
			EYEDIR,
			nebula_panorama,
			nebula_center_22,
			nebula_radius_22,
			nebula_aspect_22,
			vec2(1.0),
			nebula_region_22,
			nebula_tint_color_22,
			nebula_tint_strength) * nebula_layer_22_strength
		+ panorama_patch(
			EYEDIR,
			nebula_panorama,
			nebula_center_23,
			nebula_radius_23,
			nebula_aspect_23,
			vec2(1.0),
			nebula_region_23,
			nebula_tint_color_23,
			nebula_tint_strength) * nebula_layer_23_strength
		+ panorama_patch(
			EYEDIR,
			nebula_panorama,
			nebula_center_24,
			nebula_radius_24,
			nebula_aspect_24,
			vec2(1.0),
			nebula_region_24,
			nebula_tint_color_24,
			nebula_tint_strength) * nebula_layer_24_strength
		+ panorama_patch(
			EYEDIR,
			nebula_panorama,
			nebula_center_25,
			nebula_radius_25,
			nebula_aspect_25,
			vec2(1.0),
			nebula_region_25,
			nebula_tint_color_25,
			nebula_tint_strength) * nebula_layer_25_strength
		+ panorama_patch(
			EYEDIR,
			nebula_panorama,
			nebula_center_26,
			nebula_radius_26,
			nebula_aspect_26,
			vec2(1.0),
			nebula_region_26,
			nebula_tint_color_26,
			nebula_tint_strength) * nebula_layer_26_strength
		+ panorama_patch(
			EYEDIR,
			nebula_panorama,
			nebula_center_27,
			nebula_radius_27,
			nebula_aspect_27,
			vec2(1.0),
			nebula_region_27,
			nebula_tint_color_27,
			nebula_tint_strength) * nebula_layer_27_strength
		+ panorama_patch(
			EYEDIR,
			nebula_panorama,
			nebula_center_28,
			nebula_radius_28,
			nebula_aspect_28,
			vec2(1.0),
			nebula_region_28,
			nebula_tint_color_28,
			nebula_tint_strength) * nebula_layer_28_strength
		+ panorama_patch(
			EYEDIR,
			nebula_panorama,
			nebula_center_29,
			nebula_radius_29,
			nebula_aspect_29,
			vec2(1.0),
			nebula_region_29,
			nebula_tint_color_29,
			nebula_tint_strength) * nebula_layer_29_strength
		+ panorama_patch(
			EYEDIR,
			nebula_panorama,
			nebula_center_30,
			nebula_radius_30,
			nebula_aspect_30,
			vec2(1.0),
			nebula_region_30,
			nebula_tint_color_30,
			nebula_tint_strength) * nebula_layer_30_strength;
	for (int cloud_index = 0; cloud_index < 70; cloud_index++) {
		float index = float(cloud_index);
		float y = 1.0 - 2.0 * ((index + 0.5) / 70.0);
		float horizontal = sqrt(1.0 - y * y);
		float azimuth = index * 2.39996323;
		vec3 center = vec3(cos(azimuth) * horizontal, y, sin(azimuth) * horizontal);
		vec2 random_key = vec2(index, 0.0);
		float region_width = 0.38 + hash(random_key + vec2(1.0, 0.0)) * 0.08;
		float region_height = 0.42 + hash(random_key + vec2(2.0, 0.0)) * 0.08;
		vec2 region_origin = vec2(
			hash(random_key + vec2(3.0, 0.0)) * (1.0 - region_width),
			hash(random_key + vec2(4.0, 0.0)) * (1.0 - region_height));
		vec4 region = vec4(region_origin, region_width, region_height);
		float radius = 0.19 + hash(random_key + vec2(5.0, 0.0)) * 0.07;
		float aspect = 1.5 + hash(random_key + vec2(6.0, 0.0)) * 0.8;
		vec3 tint = nebula_palette_color(cloud_index % 6);
		nebula += panorama_patch(
			EYEDIR,
			nebula_panorama,
			center,
			radius,
			aspect,
			vec2(1.0),
			region,
			tint,
			nebula_tint_strength) * 0.35;
	}
	vec3 horizon = panorama_patch(
		EYEDIR,
		horizon_panorama,
		horizon_center,
		horizon_radius,
		horizon_aspect,
		horizon_uv_flip,
		vec4(0.0, 0.0, 1.0, 1.0),
		vec3(1.0),
		0.0) * horizon_strength;
	COLOR = stars + nebula * nebula_strength + horizon;
}
