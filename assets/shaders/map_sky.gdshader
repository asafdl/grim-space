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
// 260 far => atan = 0.0996 rad. SKYPASS V3P1 ITEM S — whole-feature scale down
// by 0.60: the rendered envelope measured ~71% of frame height at the
// standard mappose and the user ruled 40-45%; every BH length (shadow radius,
// glow outer edge, ring sigma) carries the same factor so the internal
// proportions are bit-identical — only the on-screen size moves.
uniform float bh_shadow_radius : hint_range(0.01, 1.2) = 0.06;
// Outer edge of the glow. EHT/GMVA class: shadow diameter ~ half the glow
// diameter (shadow small inside generous light).
uniform float bh_glow_radius : hint_range(0.02, 1.6) = 0.114;
// SKYPASS V3 ITEM 1 — inclination squash: the accretion light (ring band,
// halo, grain field) is drawn on a squashed ellipse — major axis horizontal,
// minor/major = this factor — so the feature reads as a semi-edge-on disc
// instead of a pole-on bull's-eye. 1.0 = the old pole-on circle. The shadow
// itself stays circular (it is the lensed silhouette, not the disc), and the
// glow mask stays keyed to that circle so light can never bleed onto it.
uniform float bh_disc_squash : hint_range(0.33, 1.0) = 0.55;
// Photon-ring band gaussian sigma. Visible FWHM = 1.66 * this (= 0.174 rad),
// >= 1/3 of the glow diameter (0.127 rad) — a broad soft band, never a knife line.
uniform float bh_ring_thickness : hint_range(0.01, 0.8) = 0.063;
uniform float bh_energy : hint_range(0.0, 4.0) = 1.55;
// SKYPASS V3 ITEM 4 — palette pinned to the over/under-arcs target crop
// (D:/ops-work/bh-yt/rely-v2/target-overunder-arcs.png): fully saturated
// deep orange/red — crest sRGB (254,152,53), mid band (209,50,2), outer
// falloff (125,1,0). Deliberately NOT the washed amber of v2 and NOT the
// NASA blue variant the user ruled out.
uniform vec3 bh_hot_color : source_color = vec3(1.0, 0.45, 0.12);
uniform vec3 bh_warm_color : source_color = vec3(0.88, 0.16, 0.005);
uniform vec3 bh_outer_color : source_color = vec3(0.62, 0.0, 0.0);
// One side of the ring brighter. In a direction-locked sky pass the doppler
// side is pinned to the ring the way a real photograph pins it.
uniform float bh_doppler_amount : hint_range(0.0, 1.5) = 1.5;
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
	// ITEM 1 — two metrics from one projection: `ang` stays the isotropic
	// (circular) angular distance and keeps driving the shadow disc and the
	// glow mask (verdict: pure-black disc, glow never on it); `ang_e` is the
	// squashed accretion metric that the disc light (band, halo, texture,
	// doppler) is drawn on, so the ellipse reads semi-edge-on. The squashed
	// ellipse is strictly inside the circular glow radius, so the early-out
	// below culls exactly the lightless region.
	float ang = length(projected);
	float ang_e = length(vec2(projected.x, projected.y / bh_disc_squash));
	if (ang >= bh_glow_radius) {
		return base;
	}
	vec2 unit = projected / max(ang, 0.000001);
	float t = clamp((ang_e - bh_shadow_radius) / max(bh_glow_radius - bh_shadow_radius, 0.001), 0.0, 1.0);
	float ring_angle = atan(projected.y, projected.x);

	// ITEM B — the shadow: a solid opaque pure-black disc with a clean edge.
	// It is drawn UNDER the glow in this one element: the glow mask rises only
	// once the disc edge is fully drawn (1.02x radius outward), so glow never
	// bleeds onto the black disc — no fresnel-rim-only read, no grey fringe.
	float shadow = 1.0 - smoothstep(bh_shadow_radius * 0.99, bh_shadow_radius * 1.01, ang);
	float glow_mask = smoothstep(bh_shadow_radius * 1.02, bh_shadow_radius * 1.10, ang);

	// ITEM C — photon ring + glow per the real references: a BROAD soft band
	// (visible FWHM ~ 3.3 * glow 0.4-glow-width >= 1/3 of the glow diameter,
	// no knife line anywhere) riding a wide diffuse falloff that runs all the
	// way out to the glow edge, so the shadow stays small inside generous light.
	float ring_center = bh_shadow_radius + (bh_glow_radius - bh_shadow_radius) * 0.42;
	float ring_width = bh_ring_thickness;
	// ITEM 1 — the band and halo ride the squashed metric: concentric
	// circles become concentric ellipses, major axis horizontal. Same
	// gaussians, same widths — the broad-band verdict carries over as long
	// as the squash stays >= ~0.33 (it is 0.55).
	float ring_band = exp(-pow((ang_e - ring_center) / ring_width, 2.0));
	float halo = pow(max(1.0 - t, 0.0), 1.35)
		* (1.0 - smoothstep(bh_glow_radius * 0.86, bh_glow_radius, ang_e));

	// Differential swirl on TIME (inner laps outer), carried over.
	float spiral = ring_angle * 1.5 - t * 7.5 + TIME * bh_swirl_speed * (1.45 - 0.95 * t);
	float grain = noise(vec2(spiral * 1.7, t * 16.0)) * 0.65
		+ noise(vec2(spiral * 3.3, t * 30.0)) * 0.35;
	float pulse = noise(vec2(TIME * bh_flicker_speed, 3.1)) * 0.65
		+ noise(vec2(TIME * bh_flicker_speed * 2.6, 8.7)) * 0.35;
	float alive = 1.0 + bh_flicker * (pulse * 2.0 - 1.0) * 0.5;

	// Doppler side: bright on one side of the ring. ITEM 3 — repinned to the
	// target crop: the bright band sits screen LEFT at the evidence pose.
	// Probe renders nailed the axis mapping: the sky-pass frame is
	// left-handed on screen — local (1,0) boosts screen LEFT (it hid under
	// the encounter UI panel in the load-save renders, which is what made
	// the earlier probes look contradictory), (0,1) boosts screen up.
	float doppler = max(0.05, 1.0 + bh_doppler_amount * bh_doppler_sign * dot(unit, vec2(1.0, 0.0)));

	// ITEM 2 — the main light keeps the v2 envelope structure, now
	// elliptical: brightness is bounded at the squashed glow edge (v2 had
	// the same hard outer bound on the circle — it was the early-out). The
	// gaussian band tail must not leak past it; the lensed arcs (below)
	// ride INSIDE this envelope as of skypass v3.1 item B.
	float glow_env = 1.0 - smoothstep(bh_glow_radius * 0.97, bh_glow_radius, ang_e);
	float brightness = bh_energy * glow_mask * glow_env * alive
		* (0.55 + grain * bh_grain * 0.35)
		* (ring_band * (0.85 + 0.75 * max(doppler - 1.0, 0.0)) + halo * 0.30 * doppler);
	vec3 ring_col = mix(bh_hot_color, bh_warm_color, smoothstep(0.0, 0.45, t));
	// ITEM 4 — the outer falloff must land on the pure deep red the crop
	// shows (sRGB 135,3,1 — no green), so the band colour itself is routed
	// to bh_outer_color at high t (the huge band sigma would otherwise keep
	// the warmer mix alive out to the glow edge and grey the falloff down).
	ring_col = mix(ring_col, bh_outer_color, smoothstep(0.42, 0.58, t));
	vec3 glow = ring_col * ring_band
		+ mix(bh_warm_color * 0.85, bh_outer_color, smoothstep(0.10, 0.55, t)) * (halo * 1.0)
		+ bh_hot_color * halo * 0.25;

	// ITEM 2 — lensed over/under arcs (Gargantua class). The far-side disc
	// light is bent OVER the top of the silhouette and back UNDER its bottom.
	// These terms are added AFTER the shadow composite so the glow mask order
	// cannot cut them, BUT SKYPASS V3P1 ITEM B: no arc pixel may cross the
	// shadow face — the user ruled the lower arc must hug OUTSIDE and BELOW
	// the shadow edge like Gargantua's lower arc, so every arc term is
	// multiplied by the shadow-face mask `outside_shadow` (zero inside the
	// disc edge) and the old front band that crossed the lower half of the
	// disc is REMOVED — it was the "bottom light cuts through the event
	// horizon" read. The under arc stays a soft BROAD gaussian (width of a
	// tenth of the shadow radius, never a 1-px filament — verdict C holds)
	// riding the alive grain and doppler factor like the ring; the top band
	// is rigid per ITEM T. Curvature: the over/under arcs hug the circular
	// lensed silhouette; the under arc centre sits just outside the shadow
	// edge (1.22x radius) so the hug is tight.
	float outside_shadow = smoothstep(bh_shadow_radius * 0.99, bh_shadow_radius * 1.01, ang);
	// SKYPASS V3P1 ITEM T — the top arc is RIGID: a crisp continuous band with
	// clean edges and no wobble. A smoothstep slab (steady crest 1.08-1.28x
	// radius, ~0.04x-radius edge transitions) replaces the old centred
	// gaussian, and the TIME-driven flicker/grain factors are dropped from the
	// arc energy: the user ruled the top arc must not wobble. It stays
	// continuous across the whole upper silhouette via a sin-taper (full over
	// the top dome, easing out at both sides), never a beaded stitch. The
	// under arc keeps its soft gaussian hug — only the top band is rigid.
	float top_mid = bh_shadow_radius * 1.18;
	float top_half = bh_shadow_radius * 0.10;
	float top_edge = bh_shadow_radius * 0.04;
	float top_band = smoothstep(top_mid - top_half - top_edge, top_mid - top_half, ang)
		* smoothstep(top_mid + top_half + top_edge, top_mid + top_half, ang)
		* smoothstep(0.0, 0.30, unit.y);
	// Crest at 1.22x radius with a 0.10x sigma: at the shadow cut (1.01x) the
	// gaussian is already exp(-(2.1)^2) ~= 1%, so the arc dies cleanly OUTSIDE
	// the rim — the hug stays tight but never truncates against the black.
	float arc_under = exp(-pow((ang - bh_shadow_radius * 1.22) / (bh_shadow_radius * 0.10), 2.0))
		* exp(-pow((ring_angle + 1.5708) / 0.75, 2.0));
	// Doppler on the arcs uses the ring's scheme (a base level everywhere,
	// boosted on the bright side) so the dim-side arc stays visible instead
	// of being wiped out — the target crop shows both arcs on both sides.
	// Arc colour tracks the hug position on the CIRCULAR silhouette (the
	// squashed metric blows up over the shadow's top/bottom face, where the
	// arcs live).
	float arc_t = clamp((ang - bh_shadow_radius) / max(bh_glow_radius - bh_shadow_radius, 0.001), 0.0, 1.0);
	// ITEM T — steady arc energy for the rigid top band (no flicker, no grain
	// — the wobble the user ruled out); the under arc keeps the alive grain.
	float arc_energy_steady = bh_energy * (0.85 + 0.75 * max(doppler - 1.0, 0.0));
	float arc_energy_alive = arc_energy_steady * alive * (0.55 + grain * bh_grain * 0.35);
	vec3 arc_col = mix(bh_hot_color, bh_warm_color, smoothstep(0.0, 0.7, arc_t));

	return base * (1.0 - shadow) + glow * brightness
		+ arc_col * ((top_band * arc_energy_steady + arc_under * 0.7 * arc_energy_alive) * outside_shadow);
}

void sky() {
	vec3 base = texture(stars_panorama, panorama_uv(EYEDIR)).rgb * energy
		+ panorama_patch(EYEDIR, nebula_panorama, nebula_center, nebula_radius, nebula_aspect, vec2(1.0)) * nebula_strength;
	COLOR = apply_black_hole(base, EYEDIR);
}
