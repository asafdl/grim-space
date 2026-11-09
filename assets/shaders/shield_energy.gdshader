shader_type spatial;
render_mode cull_back, blend_mix, depth_draw_never, shadows_disabled;

uniform vec4 team_color : source_color = vec4(0.52, 0.68, 0.76, 1.0);
uniform vec3 bubble_center = vec3(0.0);
uniform vec3 bubble_radius = vec3(1.0);
uniform float opacity : hint_range(0.0, 1.0) = 1.0;
uniform vec4 pulse_color : source_color = vec4(0.32, 0.85, 1.0, 1.0);
uniform float pulse_strength = 0.0;
uniform float dissolve : hint_range(0.0, 1.0) = 0.0;
uniform vec3 link_point = vec3(0.0, 0.0, 1.0);
uniform float link_glow = 0.0;
uniform float link_phase = 0.0;
varying vec3 shell_position;

void vertex() {
	shell_position = (VERTEX - bubble_center) / bubble_radius;
}

void fragment() {
	float noise = fract(sin(dot(floor(FRAGCOORD.xy / 9.0), vec2(127.1, 311.7))) * 43758.5453);
	if (dissolve >= 1.0 || (dissolve > 0.0 && noise < dissolve)) {
		discard;
	}
	vec3 direction = normalize(shell_position);
	vec3 magnitude = abs(direction);
	vec3 cube = direction / max(magnitude.x, max(magnitude.y, magnitude.z));
	vec2 panel;
	if (magnitude.x >= magnitude.y && magnitude.x >= magnitude.z) {
		panel = cube.yz;
	} else if (magnitude.y >= magnitude.z) {
		panel = cube.xz;
	} else {
		panel = cube.xy;
	}
	vec2 grid = panel * 4.0;
	vec2 period = vec2(1.7320508, 3.0);
	vec2 a = mod(grid, period) - period * 0.5;
	vec2 b = mod(grid - period * 0.5, period) - period * 0.5;
	vec2 cell = dot(a, a) < dot(b, b) ? a : b;
	vec2 p = abs(cell);
	float hex_distance = max(p.x, dot(p, vec2(0.5, 0.8660254)));
	float edge = 0.8660254 - hex_distance;
	float pattern = 1.0 - smoothstep(0.012, 0.012 + max(fwidth(edge), 0.004), edge);
	float facing = clamp(dot(normalize(NORMAL), normalize(VIEW)), 0.0, 1.0);
	float fresnel = pow(1.0 - facing, 3.0);
	float sweep = dot(direction, normalize(vec3(0.3, 1.0, -0.25)));
	float arc_distance = abs(sweep - 0.32);
	float arc = 1.0 - smoothstep(0.018, 0.018 + max(fwidth(sweep), 0.003), arc_distance);
	float arc_halo = 1.0 - smoothstep(0.0, 0.07, arc_distance);
	float arc_breaks = smoothstep(-0.15, 0.35,
		sin(dot(direction, vec3(4.0, 0.0, 3.0)) + TIME * 0.25));
	float rim_breaks = 0.3 + 0.7 * smoothstep(-0.1, 0.55,
		sin(dot(direction, vec3(-3.0, 5.0, 2.0))));
	float rim_core = pow(1.0 - facing, 3.5) * rim_breaks;
	float rim_halo = fresnel * rim_breaks;
	float patches = smoothstep(0.35, 0.8,
		sin(dot(direction, vec3(5.0, -4.0, 3.0)) + TIME * 0.2));
	float markings = pattern * patches * (0.3 + 0.65 * fresnel);
	float energy = rim_core * 1.2 + arc * arc_breaks * 0.8 + markings * 0.45;
	float link_distance = 1.0 - dot(direction, link_point);
	float link_arrival = pow(0.5 + 0.5 * cos(link_phase - TIME * 2.2), 6.0);
	float link_spot = link_glow * exp(-link_distance * 60.0) * (0.7 + link_arrival * 0.8);
	float link_hexes = link_glow * pattern * exp(-link_distance * 14.0) * (0.35 + link_arrival * 0.65);
	energy += link_spot * 1.5 + link_hexes;
	ALBEDO = vec3(0.0);
	SPECULAR = 0.0;
	EMISSION = team_color.rgb * (energy * 3.0
		+ rim_halo * 0.35 + arc_halo * arc_breaks * 0.2)
		+ pulse_color.rgb * pulse_strength * (0.15 + energy);
	ALPHA = clamp(0.008 + rim_halo * 0.22 + energy * 0.65
		+ arc_halo * arc_breaks * 0.08 + pulse_strength * 0.08, 0.0, 0.8) * opacity;
}
