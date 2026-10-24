// Event-horizon silhouette: pure black catch-all with a thin photon ring at
// the limb (the cheap "lensing hint" — light piled up on the edge of no-return).
// Unshaded writes its color straight from ALBEDO, so the ring lives there.
shader_type spatial;
render_mode unshaded, cull_back, depth_draw_opaque, shadows_disabled;

uniform vec3 photon_color : source_color = vec3(1.0, 0.72, 0.42);
uniform float rim_energy : hint_range(0.0, 12.0) = 3.4;
uniform float rim_power : hint_range(1.0, 8.0) = 4.5;
// Photon ring brightness feed (0..1): the disk shader's doppler side, so the
// ring reads as fed BY the disk instead of painted on.
uniform float ring_feed : hint_range(0.0, 1.0) = 0.8;

void fragment() {
	float facing = clamp(dot(normalize(NORMAL), normalize(VIEW)), 0.0, 1.0);
	float fres = pow(1.0 - facing, rim_power);
	float photon_ring = smoothstep(0.42, 0.92, fres);
	ALBEDO = photon_color * photon_ring * rim_energy * (0.55 + 0.45 * ring_feed);
}
