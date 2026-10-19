shader_type canvas_item;
render_mode unshaded, blend_add;

// ---------------------------------------------------------------------------
// Screen space glare.
//
// The Sun is a light source, not a sprite: it has to burn into the lens and
// lift the whole frame.  This is drawn additively in screen space, so the
// glare survives wherever the Sun is on screen - even when it is a small
// distant disc, and even when it is behind the frame edge.
//
// The companion pass, sun_dip.gdshader, darkens the frame when the Sun is
// filling it (the "squint").
// ---------------------------------------------------------------------------

uniform vec2 sun_screen = vec2(0.5, 0.5);
uniform float sun_radius = 0.05;
uniform float aspect = 1.7778;
uniform float intensity = 1.0;
uniform float breathe = 0.10;
uniform float streak_amount = 0.25;
uniform float chromatic = 0.35;
uniform vec3 glare_tint : source_color = vec3(1.00, 0.58, 0.26);
uniform vec3 core_tint : source_color = vec3(1.00, 0.93, 0.80);

void fragment() {
	vec2 d = (UV - sun_screen) * vec2(aspect, 1.0);
	float r = length(d);

	// The eye keeps adjusting: the whole glare breathes.
	float breath = 1.0 + breathe * sin(TIME * 0.83) * sin(TIME * 0.31 + 1.7);

	float sr = max(sun_radius, 0.0005);

	// Long range halo: it has to reach the far corners of the frame.
	float wide = exp(-r * 1.45) * 0.16;
	// A tighter, hotter inner halo.
	float near = exp(-r * 6.5) * 0.26;
	// The burn-in core, only where the disc actually is.
	float core = exp(-pow(r / sr, 2.0) * 0.9);

	vec3 col = glare_tint * (wide + near);

	// Chromatic bleed: the halo is shifted warm at the core and cool at the
	// edges, and the shift grows with the distance from the Sun.
	float fringe = chromatic * 0.10 * smoothstep(0.0, 0.55, r);
	col.r *= 1.0 + fringe;
	col.b *= 1.0 - fringe * 0.8;

	// A restrained anamorphic streak - horizontal, not a gimmick.
	float streak = exp(-abs(d.y) * 40.0) * exp(-abs(d.x) * 1.9) * streak_amount * 0.30;
	col += glare_tint * streak;

	// The white hot core where the disc burns through the optics.
	col += core_tint * core * 0.30;

	COLOR = vec4(col * intensity * breath, 1.0);
}
