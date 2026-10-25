shader_type spatial;
render_mode unshaded, cull_disabled;

uniform float cut_plane;
uniform bool keep_lower_section;

varying vec3 local_position;

void vertex() {
	local_position = VERTEX;
}

float hash_cell(vec2 cell) {
	return fract(sin(dot(cell, vec2(127.1, 311.7))) * 43758.5453);
}

float value_noise(vec2 position) {
	vec2 cell = floor(position);
	vec2 blend = fract(position);
	blend = blend * blend * (3.0 - 2.0 * blend);

	float lower = mix(
		hash_cell(cell),
		hash_cell(cell + vec2(1.0, 0.0)),
		blend.x);
	float upper = mix(
		hash_cell(cell + vec2(0.0, 1.0)),
		hash_cell(cell + vec2(1.0, 1.0)),
		blend.x);
	return mix(lower, upper, blend.y) * 2.0 - 1.0;
}

float jagged_cut_offset(vec2 position) {
	float broad = value_noise(position * vec2(0.55, 0.8));
	float sharp = value_noise(position * vec2(1.7, 2.2));
	return floor((broad * 1.15 + sharp * 0.35) * 5.0) / 5.0;
}

void fragment() {
	float cut_edge = cut_plane + jagged_cut_offset(local_position.xy);
	if (keep_lower_section && local_position.z > cut_edge) {
		discard;
	}
	if (!keep_lower_section && local_position.z < cut_edge) {
		discard;
	}

	ALBEDO = vec3(0.0);
}
