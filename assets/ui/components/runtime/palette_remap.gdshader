shader_type canvas_item;

uniform vec4 surface_color : source_color = vec4(0.02, 0.04, 0.07, 0.92);
uniform vec4 glow_color : source_color = vec4(0.30, 0.66, 1.0, 0.52);
uniform vec4 edge_color : source_color = vec4(0.55, 0.78, 1.0, 1.0);
uniform float glow_strength : hint_range(0.0, 1.5) = 0.7;
uniform float solid_fill : hint_range(0.0, 1.0) = 0.0;

varying vec4 vertex_color;

void vertex() {
	vertex_color = COLOR;
}

float surrounding_alpha(sampler2D source_texture, vec2 uv, vec2 radius) {
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
	vec4 source = texture(TEXTURE, UV);
	float edge_mask = smoothstep(0.56, 0.92, source.a);
	float glow_mask = smoothstep(0.12, 0.62, source.a) * (1.0 - edge_mask);
	float silhouette = smoothstep(0.04, 0.22, source.a);
	vec3 mapped_color = mix(surface_color.rgb, glow_color.rgb, glow_mask * 0.5);
	mapped_color = mix(mapped_color, edge_color.rgb, edge_mask);
	float textured_alpha = source.a * mix(surface_color.a, edge_color.a, edge_mask);
	float solid_alpha = silhouette * mix(surface_color.a, edge_color.a, edge_mask);
	float mapped_alpha = mix(textured_alpha, solid_alpha, solid_fill);

	float near_glow = smoothstep(
		0.04,
		0.22,
		surrounding_alpha(TEXTURE, UV, TEXTURE_PIXEL_SIZE * 5.0));
	float far_glow = smoothstep(
		0.04,
		0.22,
		surrounding_alpha(TEXTURE, UV, TEXTURE_PIXEL_SIZE * 11.0));
	float halo = max(max(near_glow * 0.85, far_glow * 0.38) - silhouette, 0.0);
	float halo_alpha = halo * glow_strength * glow_color.a;
	float output_alpha = mapped_alpha + halo_alpha * (1.0 - mapped_alpha);
	vec3 output_color = mapped_color * mapped_alpha;
	output_color += glow_color.rgb * halo_alpha * (1.0 - mapped_alpha);

	COLOR = vec4(
		output_color / max(output_alpha, 0.0001),
		output_alpha) * vertex_color;
}
