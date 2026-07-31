shader_type spatial;
render_mode unshaded, cull_disabled, blend_mix;

uniform vec4 albedo : source_color = vec4(0.4, 0.58, 0.78, 0.4);
uniform float pixel_radius : hint_range(1.0, 24.0) = 4.0;

void vertex() {
	vec4 clip_center = PROJECTION_MATRIX * VIEW_MATRIX * MODEL_MATRIX * vec4(0.0, 0.0, 0.0, 1.0);
	vec2 clip_offset = VERTEX.xy * pixel_radius * 2.0 / VIEWPORT_SIZE * clip_center.w;
	clip_center.xy += clip_offset;
	POSITION = clip_center;
}

void fragment() {
	vec2 centered = UV - vec2(0.5);
	if (dot(centered, centered) > 0.25) {
		discard;
	}
	ALBEDO = albedo.rgb;
	ALPHA = albedo.a;
}
