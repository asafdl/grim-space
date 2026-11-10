shader_type canvas_item;
render_mode unshaded;

varying vec4 glyph_tint;

void vertex() {
	glyph_tint = COLOR;
}

void fragment() {
	vec4 tint = glyph_tint;
	float core = texture(TEXTURE, UV).a;
	float halo = 0.0;
	float weights = 0.0;
	for (int y = -3; y <= 3; y++) {
		for (int x = -3; x <= 3; x++) {
			vec2 offset = vec2(float(x), float(y));
			float weight = exp(-dot(offset, offset) / 4.0);
			halo += texture(TEXTURE, UV + offset * TEXTURE_PIXEL_SIZE * 2.0).a * weight;
			weights += weight;
		}
	}
	halo = clamp(halo / weights * 1.8, 0.0, 0.75);
	float alpha = core + halo * (1.0 - core);
	vec3 light = mix(tint.rgb, vec3(1.0), 0.32);
	COLOR = vec4(mix(tint.rgb, light, core), alpha * tint.a);
}
