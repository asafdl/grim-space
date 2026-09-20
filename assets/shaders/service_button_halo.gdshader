shader_type canvas_item;

uniform vec4 halo_color : source_color = vec4(0.55, 0.78, 1.0, 1.0);
uniform float emphasis : hint_range(0.0, 1.0) = 0.0;

varying vec4 vertex_color;

void vertex() {
	vertex_color = COLOR;
}

float surrounding_alpha(
	sampler2D source_texture,
	vec2 uv,
	vec2 radius
) {
	vec2 diagonal = radius * 0.7071;
	float alpha = texture(source_texture, uv + vec2(radius.x, 0.0)).a;
	alpha = max(alpha, texture(source_texture, uv - vec2(radius.x, 0.0)).a);
	alpha = max(alpha, texture(source_texture, uv + vec2(0.0, radius.y)).a);
	alpha = max(alpha, texture(source_texture, uv - vec2(0.0, radius.y)).a);
	alpha = max(alpha, texture(source_texture, uv + diagonal).a);
	alpha = max(alpha, texture(source_texture, uv - diagonal).a);
	alpha = max(alpha, texture(source_texture, uv + vec2(diagonal.x, -diagonal.y)).a);
	return max(alpha, texture(source_texture, uv + vec2(-diagonal.x, diagonal.y)).a);
}

void fragment() {
	vec4 base = texture(TEXTURE, UV) * vertex_color;
	float silhouette = smoothstep(0.08, 0.35, base.a);
	float inner = smoothstep(
		0.08,
		0.35,
		surrounding_alpha(TEXTURE, UV, TEXTURE_PIXEL_SIZE * 6.0));
	float outer = smoothstep(
		0.08,
		0.35,
		surrounding_alpha(TEXTURE, UV, TEXTURE_PIXEL_SIZE * 12.0));
	float halo = max(max(inner * 0.9, outer * 0.45) - silhouette, 0.0);
	float halo_alpha = halo * mix(0.42, 0.95, emphasis) * halo_color.a;
	float output_alpha = base.a + halo_alpha * (1.0 - base.a);
	base.rgb *= mix(1.0, 1.08, emphasis);
	vec3 output_color = base.rgb * base.a;
	output_color += halo_color.rgb * halo_alpha * (1.0 - base.a);

	COLOR = vec4(output_color / max(output_alpha, 0.0001), output_alpha);
}
