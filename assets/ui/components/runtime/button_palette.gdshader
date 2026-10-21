shader_type canvas_item;

uniform vec4 surface_color : source_color = vec4(0.02, 0.04, 0.07, 1.0);
uniform vec4 edge_color : source_color = vec4(0.55, 0.78, 1.0, 1.0);
uniform float palette_mix : hint_range(0.0, 1.0) = 0.0;
uniform float brightness : hint_range(0.0, 1.5) = 1.0;

varying vec4 vertex_color;

void vertex() {
	vertex_color = COLOR;
}

void fragment() {
	vec4 source = texture(TEXTURE, UV);
	float luminance = dot(source.rgb, vec3(0.2126, 0.7152, 0.0722));
	float highlight = smoothstep(0.22, 0.72, luminance);
	vec3 remapped = mix(surface_color.rgb, edge_color.rgb, highlight);
	vec3 output_color = mix(source.rgb, remapped, palette_mix) * brightness;

	COLOR = vec4(output_color, source.a) * vertex_color;
}
