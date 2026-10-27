shader_type sky;

uniform sampler2D stars_panorama : filter_linear_mipmap, source_color;
uniform sampler2D nebula_panorama : filter_linear_mipmap, source_color;
uniform float energy : hint_range(0.0, 2.0) = 0.26;
uniform float nebula_strength : hint_range(0.0, 2.0) = 0.45;
uniform vec3 nebula_center = vec3(0.764, -0.404, -0.503);
uniform float nebula_radius : hint_range(0.05, 0.8) = 0.30;
uniform float nebula_aspect : hint_range(0.5, 2.5) = 1.071;

// SKY-PASS black hole (direction-locked, like the nebula patch): a fixed sky
// direction, so the shadow and ring have ZERO parallax under WASD pans and no
// far-clip ceiling — the sky dome is "infinitely far" by construction. All
// sizes are angular (radians).
uniform vec3 bh_center = vec3(-0.62, -0.38, -0.52);
// Opaque black shadow disc radius. Old tuning preserved: horizon 26 units at
// 260 far => atan = 0.0996 rad.
uniform float bh_shadow_radius : hint_range(0.01, 1.2) = 0.10;
// Outer edge of the glow. EHT/GMVA class: shadow diameter ~ half the glow
// diameter (shadow small inside generous light).
uniform float bh_glow_radius : hint_range(0.02, 1.6) = 0.19;
uniform float bh_energy : hint_range(0.0, 4.0) = 1.55;
uniform vec3 bh_hot_color : source_color = vec3(1.0, 0.93, 0.70);
uniform vec3 bh_warm_color : source_color = vec3(1.0, 0.47, 0.16);
uniform vec3 bh_outer_color : source_color = vec3(0.45, 0.12, 0.04);
// One side of the ring brighter. In a direction-locked sky pass the doppler
// side is pinned to the ring the way a real photograph pins it.
uniform float bh_doppler_amount : hint_range(0.0, 1.5) = 0.55;
uniform float bh_doppler_sign : hint_range(-1.0, 1.0) = 1.0;
// Alive grain + differential swirl on TIME, carried over from the old disk.
uniform float bh_swirl_speed : hint_range(0.0, 2.0) = 0.30;
uniform float bh_grain : hint_range(0.0, 2.0) = 0.75;
uniform float bh_flicker : hint_range(0.0, 1.0) = 0.65;
uniform float bh_flicker_speed : hint_range(0.0, 8.0) = 2.2;

vec2 panorama_uv(vec3 eyedir) {
	vec2 uv = vec2(atan(eyedir.x, eyedir.z), acos(clamp(-eyedir.y, -1.0, 1.0)));
	uv /= vec2(TAU, PI);
	return vec2(fract(uv.x + 0.5), uv.y);
}

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

vec3 panorama_patch(
	vec3 eyedir,
	sampler2D panorama,
	vec3 patch_center,
	float patch_radius,
	float patch_aspect,
	vec2 uv_flip) {
	vec3 center = normalize(patch_center);
	vec3 up_hint = abs(center.y) < 0.99 ? vec3(0.0, 1.0, 0.0) : vec3(1.0, 0.0, 0.0);
	vec3 right = normalize(cross(up_hint, center));
	vec3 patch_up = cross(center, right);

	vec3 dir = normalize(eyedir);
	float alignment = dot(dir, center);
	if (alignment <= 0.0) {
		return vec3(0.0);
	}

	float half_height = tan(patch_radius);
	float half_width = half_height * patch_aspect;
	vec2 projected = vec2(dot(dir, right), dot(dir, patch_up)) / alignment;
	vec2 uv = projected / (2.0 * vec2(half_width, half_height)) + 0.5;
	uv = (uv - 0.5) * uv_flip + 0.5;
	if (uv.x <= 0.0 || uv.x >= 1.0 || uv.y <= 0.0 || uv.y >= 1.0) {
		return vec3(0.0);
	}

	vec3 sample_rgb = texture(panorama, uv).rgb;
	float lum = dot(sample_rgb, vec3(0.299, 0.587, 0.114));
	float gas = smoothstep(0.01, 0.08, lum);
	vec2 centered_uv = (uv - 0.5) * 2.0;
	float breakup = noise(uv * 5.0) * 0.16 + noise(uv * 11.0) * 0.06;
	float edge_mask = 1.0 - smoothstep(0.30 + breakup, 0.96 + breakup, length(centered_uv));
	return sample_rgb * gas * edge_mask;
}

// Full far-layer composite including the black hole. Structured as a normal
// function so early returns are legal (illegal in the sky() processor).
vec3 apply_black_hole(vec3 base, vec3 eyedir) {
	vec3 center = normalize(bh_center);
	float mu = dot(eyedir, center);
	if (mu <= 0.0) {
		return base;
	}

	vec3 up_hint = abs(center.y) < 0.99 ? vec3(0.0, 1.0, 0.0) : vec3(1.0, 0.0, 0.0);
	vec3 right = normalize(cross(up_hint, center));
	vec3 local_up = cross(center, right);
	vec2 projected = vec2(dot(eyedir, right), dot(eyedir, local_up)) / mu;
	float ang = length(projected);
	if (ang >= bh_glow_radius) {
		return base;
	}
	vec2 unit = projected / max(ang, 0.000001);
	float t = clamp((ang - bh_shadow_radius) / max(bh_glow_radius - bh_shadow_radius, 0.001), 0.0, 1.0);
	float ring_angle = atan(projected.y, projected.x);

	// ITEM B — the shadow: a solid opaque pure-black disc with a clean edge.
	// It is drawn UNDER the glow in this one element: the glow mask rises only
	// once the disc edge is fully drawn (1.02x radius outward), so glow never
	// bleeds onto the black disc — no fresnel-rim-only read, no grey fringe.
	float shadow = 1.0 - smoothstep(bh_shadow_radius * 0.99, bh_shadow_radius * 1.01, ang);
	float glow_mask = smoothstep(bh_shadow_radius * 1.02, bh_shadow_radius * 1.10, ang);

	// Broad soft photon ring + wide diffuse outward falloff (tuned next round).
	float ring_center = bh_shadow_radius + (bh_glow_radius - bh_shadow_radius) * 0.35;
	float ring_band = exp(-pow((ang - ring_center) / (0.25 * (bh_glow_radius - bh_shadow_radius)), 2.0));
	float halo = pow(1.0 - t, 2.0);

	// Differential swirl on TIME (inner laps outer), carried over.
	float spiral = ring_angle * 1.5 - t * 7.5 + TIME * bh_swirl_speed * (1.45 - 0.95 * t);
	float grain = noise(vec2(spiral * 1.7, t * 16.0)) * 0.65
		+ noise(vec2(spiral * 3.3, t * 30.0)) * 0.35;
	float pulse = noise(vec2(TIME * bh_flicker_speed, 3.1)) * 0.65
		+ noise(vec2(TIME * bh_flicker_speed * 2.6, 8.7)) * 0.35;
	float alive = 1.0 + bh_flicker * (pulse * 2.0 - 1.0) * 0.5;

	// Doppler side: bright on one side of the ring.
	float doppler = max(0.05, 1.0 + bh_doppler_amount * bh_doppler_sign * dot(unit, vec2(0.8, 0.6)));

	float brightness = (ring_band * 1.5 + halo * 0.65)
		* bh_energy * glow_mask * doppler * alive
		* (0.55 + grain * bh_grain * 0.55);
	vec3 glow = bh_hot_color * ring_band
		+ mix(bh_warm_color, bh_outer_color, smoothstep(0.1, 0.75, t)) * (ring_band * 0.6 + halo);

	return base * (1.0 - shadow) + glow * brightness;
}

void sky() {
	vec3 base = texture(stars_panorama, panorama_uv(EYEDIR)).rgb * energy
		+ panorama_patch(EYEDIR, nebula_panorama, nebula_center, nebula_radius, nebula_aspect, vec2(1.0)) * nebula_strength;
	COLOR = apply_black_hole(base, EYEDIR);
}
