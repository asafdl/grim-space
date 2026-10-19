shader_type canvas_item;
render_mode unshaded, blend_mix;

// ---------------------------------------------------------------------------
// The squint.
//
// When the Sun fills the frame the eye has to work: the exposure drops and the
// corners fall away.  This is a separate pass from the additive glare because
// it darkens rather than adds.
// ---------------------------------------------------------------------------

uniform float dip_strength = 0.0;
uniform float vignette_strength = 0.16;
uniform float aspect = 1.7778;
uniform vec3 dip_tint : source_color = vec3(0.05, 0.02, 0.006);

void fragment() {
	float v = smoothstep(1.05, 0.30, length((UV - 0.5) * vec2(aspect, 1.0)));
	float a = clamp(dip_strength + (1.0 - v) * vignette_strength, 0.0, 0.42);
	COLOR = vec4(dip_tint, a);
}
