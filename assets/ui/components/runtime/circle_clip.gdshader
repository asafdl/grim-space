shader_type canvas_item;

void fragment() {
	vec2 centered = UV * 2.0 - vec2(1.0);
	if (dot(centered, centered) > 1.0) {
		discard;
	}
	COLOR = texture(TEXTURE, UV) * COLOR;
}
