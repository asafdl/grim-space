shader_type spatial;
render_mode cull_disabled;

uniform sampler2D albedo_texture : source_color, filter_linear_mipmap, repeat_enable;
uniform vec4 albedo_color : source_color = vec4(1.0);
uniform bool use_albedo_texture = false;
uniform float metallic_amount = 0.0;
uniform float roughness_amount = 0.7;
uniform bool unshaded = false;
uniform float dissolve : hint_range(0.0, 1.0) = 0.0;
uniform float block_pixels : hint_range(2.0, 32.0) = 9.0;
uniform vec4 edge_color : source_color = vec4(0.6, 0.3, 1.0, 1.0);

float block_noise(vec2 block) {
	return fract(sin(dot(block, vec2(127.1, 311.7))) * 43758.5453);
}

void fragment() {
	float noise = block_noise(floor(FRAGCOORD.xy / block_pixels));
	if (dissolve >= 1.0 || (dissolve > 0.0 && noise < dissolve)) {
		discard;
	}
	vec3 base = albedo_color.rgb;
	if (use_albedo_texture) {
		base *= texture(albedo_texture, UV).rgb;
	}
	float edge = dissolve > 0.0 ? 1.0 - smoothstep(dissolve, dissolve + 0.12, noise) : 0.0;
	vec3 edge_emission = edge_color.rgb * edge * 2.0;
	ALPHA = albedo_color.a;
	if (unshaded) {
		ALBEDO = vec3(0.0);
		EMISSION = base + edge_emission;
	} else {
		ALBEDO = base;
		METALLIC = metallic_amount;
		ROUGHNESS = roughness_amount;
		EMISSION = edge_emission;
	}
}
