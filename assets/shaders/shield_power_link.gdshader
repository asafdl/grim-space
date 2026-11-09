shader_type spatial;
render_mode blend_add, depth_draw_never, cull_disabled, shadows_disabled;

uniform vec4 team_color : source_color = vec4(0.52, 0.68, 0.76, 1.0);
uniform float link_length = 1.0;
uniform float phase = 0.0;
uniform float sway = 0.03;
uniform float opacity : hint_range(0.0, 1.0) = 1.0;
uniform float dissolve : hint_range(0.0, 1.0) = 0.0;
varying float along;

void vertex() {
	along = clamp(VERTEX.y / link_length + 0.5, 0.0, 1.0);
	float envelope = sin(along * 3.14159265);
	VERTEX.x += envelope * sway * (sin(along * 8.0 + TIME * 1.1 + phase)
		+ 0.3 * sin(along * 17.0 - TIME * 0.7 + phase));
	VERTEX.z += envelope * sway * sin(along * 11.0 - TIME * 0.85 + phase);
}

void fragment() {
	float noise = fract(sin(dot(floor(FRAGCOORD.xy / 9.0), vec2(127.1, 311.7))) * 43758.5453);
	if (dissolve >= 1.0 || (dissolve > 0.0 && noise < dissolve)) {
		discard;
	}
	float traveling = pow(0.5 + 0.5 * cos(along * 6.2831853 - TIME * 2.2 + phase), 6.0);
	float socket = smoothstep(0.82, 1.0, along);
	vec3 tint = mix(team_color.rgb, vec3(0.86, 0.94, 1.0), 0.18);
	ALBEDO = vec3(0.0);
	SPECULAR = 0.0;
	EMISSION = tint * (1.1 + traveling * 1.6 + socket * 1.4) * opacity;
}
