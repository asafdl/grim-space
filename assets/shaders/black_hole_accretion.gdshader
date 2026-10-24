// Accretion disk: additive annulus on a world-fixed quad. Real geometry +
// perspective (not a decal), tilted to open toward the playfield camera, with
// spiral-shear grain and a doppler-bright approaching side.
shader_type spatial;
render_mode blend_add, depth_draw_never, cull_disabled, unshaded;

uniform float inner_ratio : hint_range(0.05, 0.9) = 0.5;
uniform float energy : hint_range(0.0, 4.0) = 1.6;
uniform vec3 hot_color : source_color = vec3(1.0, 0.93, 0.70);
uniform vec3 warm_color : source_color = vec3(1.0, 0.47, 0.16);
uniform vec3 outer_color : source_color = vec3(0.45, 0.12, 0.04);
// Approaching-side boosting: multiplied against the quad-local X coordinate
// (X is aligned to the map direction at build time).
uniform float doppler_amount : hint_range(0.0, 1.5) = 0.55;
uniform float doppler_sign : hint_range(-1.0, 1.0) = 1.0;
uniform float swirl_speed : hint_range(0.0, 2.0) = 0.30;
uniform float grain_energy : hint_range(0.0, 2.0) = 0.9;
// Alive read: the disk energy breathes on its own clock (no baked still frame).
uniform float flicker_amount : hint_range(0.0, 1.0) = 0.65;
uniform float flicker_speed : hint_range(0.0, 8.0) = 2.2;

float hash(vec2 point) {
	return fract(sin(dot(point, vec2(127.1, 311.7))) * 43758.5453);
}

float noise(vec2 point) {
	vec2 cell = floor(point);
	vec2 offset = fract(point);
	offset = offset * offset * (3.0 - 2.0 * offset);

	return mix(
		mix(hash(cell), hash(cell + vec2(1.0, 0.0)), offset.x),
		mix(hash(cell + vec2(0.0, 1.0)), hash(cell + vec2(1.0)), offset.x),
		offset.y);
}

void fragment() {
	vec2 p = UV * 2.0 - 1.0;
	float r = length(p);
	float t = (r - inner_ratio) / max(1.0 - inner_ratio, 0.001);
	float angle = atan(p.y, p.x);

	// Differential ROTATION: the drift rate rises toward the center, so the
	// inner band laps the outer and the spiral arms wind up instead of the
	// whole pattern turning as one rigid stencil.
	float spiral = angle * 1.5 - t * 7.5 + TIME * swirl_speed * (1.45 - 0.95 * t);
	float grain = noise(vec2(spiral * 1.7, t * 16.0)) * 0.65
		+ noise(vec2(spiral * 3.3, t * 30.0)) * 0.35;

	float annulus = smoothstep(inner_ratio - 0.05, inner_ratio + 0.10, r)
		* (1.0 - smoothstep(0.82, 1.0, r));
	float hot_edge = exp(-t * 5.5);

	vec3 col = mix(warm_color, outer_color, clamp(t * 1.4, 0.0, 1.0));
	col += hot_color * hot_edge * 1.5;

	float doppler = 1.0 + doppler_amount * doppler_sign * p.x;
	// Two octaves of TIME noise: a slow swell plus a faster shimmer, so the
	// disk never holds one still brightness (a photo would).
	float pulse = noise(vec2(TIME * flicker_speed, 3.1)) * 0.65
		+ noise(vec2(TIME * flicker_speed * 2.6, 8.7)) * 0.35;
	float alive = 1.0 + flicker_amount * (pulse * 2.0 - 1.0) * 0.5;
	float brightness = annulus * (0.5 + grain * grain_energy) * doppler * energy * alive
		+ hot_edge * annulus * 0.6 * alive;
	ALBEDO = col * brightness;
}
