shader_type spatial;
render_mode cull_back, blend_mix, depth_draw_never, shadows_disabled;

uniform vec4 team_color : source_color = vec4(0.52, 0.68, 0.76, 1.0);
void fragment() {
	float aa = max(fwidth(UV.x), 0.002);
	float seam = 1.0 - smoothstep(0.015, 0.015 + aa, UV.x);
	float facing = clamp(dot(normalize(NORMAL), normalize(VIEW)), 0.0, 1.0);
	float rim = pow(1.0 - facing, 3.0);
	ALBEDO = mix(team_color.rgb, vec3(0.8, 0.85, 0.9), 0.25);
	METALLIC = 0.45;
	ROUGHNESS = 0.22;
	SPECULAR = 0.8;
	EMISSION = team_color.rgb * (seam * 1.6 + rim * 0.18);
	ALPHA = 0.10 + rim * 0.14 + seam * 0.26;
}
