shader_type spatial;
render_mode cull_disabled, shadows_disabled;

uniform sampler2D albedo_texture : source_color, filter_linear_mipmap, repeat_enable;
uniform bool use_texture = false;
uniform vec3 tint : source_color = vec3(0.42, 0.44, 0.5);
uniform float desaturation : hint_range(0.0, 1.0) = 0.95;
uniform sampler2D occlusion_texture : filter_linear_mipmap, repeat_enable;
uniform bool use_occlusion_texture = false;
uniform int occlusion_channel = 0;
uniform sampler2D roughness_texture : filter_linear_mipmap, repeat_enable;
uniform bool use_roughness_texture = false;
uniform int roughness_channel = 1;
uniform float direct_light_strength : hint_range(0.0, 1.0) = 0.3;

// Keeps only the slice of the mesh whose Z lies between the two jagged edges.
uniform float keep_min = -10.0;
uniform float keep_max = 10.0;
uniform vec2 keep_min_slope = vec2(0.0);
uniform vec2 keep_max_slope = vec2(0.0);
uniform float jag_amplitude = 0.06;
uniform float noise_scale = 6.0;
uniform vec2 edge_phase = vec2(0.0);

// Window / light geometry: only a sparse few cells still glow.
uniform bool lights_mode = false;
uniform sampler2D window_texture : source_color, filter_linear_mipmap, repeat_enable;
uniform bool use_window_texture = false;
uniform vec3 window_color : source_color = vec3(1.0);
uniform float light_cell_size : hint_range(0.001, 0.05) = 0.025;
uniform float light_density : hint_range(0.0, 1.0) = 0.025;
uniform float light_strength = 0.2;

uniform vec3 sun_direction = vec3(0.0, 1.0, 0.0);
uniform vec3 planet_direction = vec3(0.0, -1.0, 0.0);
uniform vec3 planet_fill_color : source_color = vec3(0.4, 0.4, 0.55);
uniform float planet_fill_strength : hint_range(0.0, 1.0) = 0.03;
uniform vec3 shadow_fill_color : source_color = vec3(0.32, 0.36, 0.5);
uniform float shadow_floor : hint_range(0.0, 0.3) = 0.05;
uniform vec3 sun_rim_color : source_color = vec3(1.0, 0.42, 0.2);
uniform float sun_rim_strength = 0.025;

varying vec3 local_position;
varying vec3 world_normal;

void vertex() {
	local_position = VERTEX;
	world_normal = normalize(mat3(MODEL_MATRIX) * NORMAL);
}

float hash_cell(vec2 cell) {
	return fract(sin(dot(cell, vec2(127.1, 311.7))) * 43758.5453);
}

float hash_cell3(vec3 cell) {
	return fract(sin(dot(cell, vec3(127.1, 311.7, 74.7))) * 43758.5453);
}

float value_noise(vec2 position) {
	vec2 cell = floor(position);
	vec2 blend = fract(position);
	blend = blend * blend * (3.0 - 2.0 * blend);

	float lower = mix(hash_cell(cell), hash_cell(cell + vec2(1.0, 0.0)), blend.x);
	float upper = mix(hash_cell(cell + vec2(0.0, 1.0)), hash_cell(cell + vec2(1.0, 1.0)), blend.x);
	return mix(lower, upper, blend.y) * 2.0 - 1.0;
}

float jagged_offset(vec2 position) {
	float broad = value_noise(position * vec2(0.55, 0.8));
	float sharp = value_noise(position * vec2(1.7, 2.2));
	return floor((broad * 1.15 + sharp * 0.35) * 5.0) / 5.0 * jag_amplitude;
}

void fragment() {
	vec2 cut_position = local_position.xy * noise_scale;
	float low_edge = keep_min + dot(local_position.xy, keep_min_slope)
		+ jagged_offset(cut_position + edge_phase);
	float high_edge = keep_max + dot(local_position.xy, keep_max_slope)
		+ jagged_offset(cut_position.yx - edge_phase + 17.0);
	if (local_position.z < low_edge || local_position.z > high_edge) {
		discard;
	}

	if (lights_mode) {
		vec3 cell = floor(local_position / light_cell_size);
		float lit = step(1.0 - light_density, hash_cell3(cell));
		vec3 glow = use_window_texture ? texture(window_texture, UV).rgb * window_color : vec3(0.0);
		ALBEDO = vec3(0.02);
		ROUGHNESS = 1.0;
		EMISSION = glow * lit * light_strength;
	} else {
		vec3 source = use_texture ? texture(albedo_texture, UV).rgb : vec3(1.0);
		float luma = dot(source, vec3(0.299, 0.587, 0.114));
		float detail = clamp((luma - 0.22) * 1.8 + 0.22, 0.0, 1.0);
		vec3 albedo = mix(source, vec3(detail), desaturation) * tint;
		float cavity = use_occlusion_texture ? texture(occlusion_texture, UV)[occlusion_channel] : 1.0;
		albedo *= mix(0.35, 1.0, cavity);

		float soot = 0.5 * (value_noise(local_position.xz * 22.0) + value_noise(local_position.yz * 22.0));
		albedo *= mix(0.5, 1.0, smoothstep(-0.45, 0.5, soot));
		float edge_distance = min(local_position.z - low_edge, high_edge - local_position.z);
		albedo *= mix(0.25, 1.0, smoothstep(0.0, 0.04, edge_distance));

		vec3 n = FRONT_FACING ? world_normal : -world_normal;
		float planet_facing = clamp(dot(n, planet_direction) * 0.5 + 0.5, 0.0, 1.0);
		float rim = pow(1.0 - clamp(dot(NORMAL, VIEW), 0.0, 1.0), 3.0);
		float sun_side = smoothstep(-0.2, 0.6, dot(n, sun_direction));

		ALBEDO = albedo;
		float surface_roughness = use_roughness_texture ? texture(roughness_texture, UV)[roughness_channel] : 0.8;
		ROUGHNESS = clamp(surface_roughness, 0.7, 1.0);
		METALLIC = 0.0;
		SPECULAR = 0.15;
		AO = cavity;
		AO_LIGHT_AFFECT = 1.0;
		EMISSION = albedo * (
				shadow_fill_color * shadow_floor
				+ planet_fill_color * planet_facing * planet_facing * planet_fill_strength)
			+ sun_rim_color * rim * sun_side * sun_rim_strength;
	}

}

void light() {
	float facing = max(dot(NORMAL, LIGHT), 0.0);
	DIFFUSE_LIGHT += ALBEDO * LIGHT_COLOR * ATTENUATION * facing * direct_light_strength / PI;
}
