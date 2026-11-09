shader_type spatial;
render_mode cull_back, blend_mix, depth_draw_never, shadows_disabled;

uniform vec4 team_color : source_color = vec4(0.52, 0.68, 0.76, 1.0);
uniform vec3 face_axis = vec3(0.0, 0.0, 1.0);
varying vec3 shell_position;
varying vec3 world_normal;

void vertex() {
	shell_position = VERTEX;
	world_normal = normalize(MODEL_NORMAL_MATRIX * NORMAL);
}

void fragment() {
	vec3 direction = normalize(shell_position);
	vec3 magnitude = abs(direction);
	// Each production shield face owns exactly one region of the smooth shell.
	vec3 owner;
	if (magnitude.x >= magnitude.y && magnitude.x >= magnitude.z) {
		owner = vec3(sign(direction.x), 0.0, 0.0);
	} else if (magnitude.y >= magnitude.z) {
		owner = vec3(0.0, sign(direction.y), 0.0);
	} else {
		owner = vec3(0.0, 0.0, sign(direction.z));
	}
	if (dot(owner, face_axis) < 0.5) {
		discard;
	}

	vec3 cube = direction / max(magnitude.x, max(magnitude.y, magnitude.z));
	vec2 panel = abs(face_axis.x) > 0.5 ? cube.yz :
		(abs(face_axis.y) > 0.5 ? cube.xz : cube.xy);
	// Two staggered lattices define hexagonal cells without a texture.
	vec2 grid = panel * 4.0;
	vec2 period = vec2(1.7320508, 3.0);
	vec2 a = mod(grid, period) - period * 0.5;
	vec2 b = mod(grid - period * 0.5, period) - period * 0.5;
	vec2 cell = dot(a, a) < dot(b, b) ? a : b;
	vec2 p = abs(cell);
	float hex_distance = max(p.x, dot(p, vec2(0.5, 0.8660254)));
	float edge = 0.8660254 - hex_distance;
	float aa = max(fwidth(edge), 0.004);
	float pattern = 1.0 - smoothstep(0.012, 0.012 + aa, edge);

	vec3 n = normalize(NORMAL);
	vec3 view = normalize(VIEW);
	float facing = clamp(dot(n, view), 0.0, 1.0);
	float fresnel = pow(1.0 - facing, 3.0);
	vec3 wn = normalize(world_normal);
	float upper_light = pow(max(dot(wn, normalize(vec3(-0.5, 0.85, 0.35))), 0.0), 8.0);
	float side_light = pow(max(dot(wn, normalize(vec3(0.8, 0.25, -0.5))), 0.0), 14.0);
	float drift = 0.5 + 0.5 * sin(dot(direction, vec3(5.0, 3.0, -4.0)) + TIME * 0.35);
	float pattern_visibility = (0.12 + fresnel * 0.5 + upper_light * 0.4) * (0.7 + drift * 0.3);
	float detail = pattern * pattern_visibility;
	vec3 tint = mix(team_color.rgb, vec3(0.65, 0.78, 0.88), 0.3);
	vec3 pearl = vec3(0.82, 0.92, 1.0);

	ALBEDO = mix(tint, pearl, upper_light * 0.45);
	METALLIC = 0.15;
	ROUGHNESS = 0.24;
	SPECULAR = 0.85;
	EMISSION = tint * (fresnel * (0.4 + upper_light * 1.4) + detail * 0.65)
		+ pearl * upper_light * 0.55 + tint * side_light * 0.35;
	ALPHA = clamp(0.025 + fresnel * 0.38 + upper_light * 0.20
		+ side_light * 0.10 + detail * 0.13, 0.0, 0.72);
}
