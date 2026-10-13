shader_type spatial;
render_mode unshaded, cull_back, shadows_disabled, fog_disabled;

uniform sampler2D surface_texture : source_color, filter_linear, repeat_enable;
uniform vec3 color_grade : source_color = vec3(1.0, 0.48, 0.3);
uniform float posterize_steps : hint_range(3.0, 10.0, 1.0) = 6.0;
uniform float posterize_strength : hint_range(0.0, 1.0) = 0.42;

void fragment() {
	vec3 sampled = texture(surface_texture, UV).rgb;
	vec3 graded = sampled * color_grade;
	vec3 posterized = floor(graded * posterize_steps + 0.5) / posterize_steps;
	ALBEDO = mix(graded, posterized, posterize_strength);
}
