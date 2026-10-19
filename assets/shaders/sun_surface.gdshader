shader_type spatial;
render_mode unshaded, cull_back, depth_draw_opaque, shadows_disabled, fog_disabled;

// ---------------------------------------------------------------------------
// Photosphere of a star.
//
// Every constant here was measured from NASA/SDO reference frames, not from
// memory.  See SUN_NOTES.md -> "Reference analysis" for the numbers and the
// frame grabs they came from.
//
//   * palette         AIA 304 disk centre  rgb(0.68, 0.04, 0.01)  (saturated red)
//                     HMI 193 disk centre  rgb(0.54, 0.30, 0.10)  (warm tan)
//                     visible-light look    white-yellow core -> orange -> red limb
//   * limb darkening  HMI 193 radial profile: I(0.90R)/I(centre) = 0.72,
//                     I(0.95R)/I(centre) = 0.33 -> u = 0.62, v = 0.18
//   * granulation     AIA 304 autocorrelation: cell ~13 px in a 1024 px frame
//                     whose disk diameter is ~854 px  ->  ~60 cells across the
//                     disk.  HMI 193 gives ~45.
// ---------------------------------------------------------------------------

uniform vec3 core_color : source_color = vec3(1.00, 0.93, 0.74);
uniform vec3 mid_color : source_color = vec3(1.00, 0.48, 0.13);
uniform vec3 limb_color : source_color = vec3(0.72, 0.12, 0.03);
uniform float emission_strength : hint_range(0.0, 4.0) = 0.62;
uniform float limb_darkening : hint_range(0.0, 1.2) = 0.50;
uniform float limb_darkening_sq : hint_range(0.0, 1.0) = 0.14;
uniform float limb_edge_strength : hint_range(0.0, 1.0) = 1.0;
uniform float limb_edge_floor : hint_range(0.0, 1.0) = 0.42;
uniform float granulation_cells : hint_range(6.0, 260.0) = 176.0;
uniform float granulation_strength : hint_range(0.0, 1.0) = 0.85;
uniform float supergranulation_cells : hint_range(1.0, 80.0) = 40.0;
uniform float supergranulation_strength : hint_range(0.0, 1.0) = 0.30;
uniform float flow_speed : hint_range(0.0, 0.5) = 0.035;
uniform float plage_cells : hint_range(1.0, 40.0) = 10.0;
uniform float plage_strength : hint_range(0.0, 1.0) = 0.55;
uniform vec3 rim_color : source_color = vec3(1.00, 0.34, 0.10);
uniform float rim_strength : hint_range(0.0, 2.0) = 0.22;
uniform float seed = 0.0;

// The real NASA/SDO photosphere, mapped equirectangularly.  It carries the
// actual granulation and the white-hot blisters; the procedural field below
// drifts on top of it so the surface never looks like a still image.
uniform sampler2D photosphere : source_color;
uniform float texture_blend : hint_range(0.0, 1.0) = 0.72;
uniform float texture_tiling : hint_range(0.25, 8.0) = 2.0;
uniform float drift_strength : hint_range(0.0, 1.0) = 0.45;

// How hard the disc centre burns out into the glare.
uniform float core_burn : hint_range(0.0, 8.0) = 2.4;

// Sunspots.  Each entry is (direction x, direction y, radius, strength) in the
// surface's own frame, so they migrate and turn with the disc.
uniform vec4 spots[4];

// A flare: (direction x, direction y, strength, size).
uniform vec4 flare_data = vec4(0.0, 0.0, 0.0, 0.16);

// Debug helper: 0 = normal, 1..8 = flat grey steps, 2 = image radius,
// 3 = mu, 4 = the limb edge term, 5 = the limb darkening term.
uniform int debug_term = 0;
uniform float sun_radius = 1.0;

varying vec3 sphere_dir;
varying vec3 sphere_world;
varying vec3 sphere_normal_world;
varying float v_distance;
varying float v_tangent;

const float SUN_PI = 3.141592653589793;

// iq's integer-lattice hash, stable on every GPU.
float hash31(vec3 p) {
	p = fract(p * 0.3183099 + 0.1);
	p *= 17.0;
	return fract(p.x * p.y * p.z * (p.x + p.y + p.z));
}

// 3D value noise in [0,1].  Sampling it on a direction vector (rather than on
// screen UVs) keeps the pattern locked to the surface while the camera moves.
float vnoise(vec3 p) {
	vec3 i = floor(p);
	vec3 f = fract(p);
	f = f * f * f * (f * (f * 6.0 - 15.0) + 10.0);
	return mix(
		mix(mix(hash31(i), hash31(i + vec3(1.0, 0.0, 0.0)), f.x),
			mix(hash31(i + vec3(0.0, 1.0, 0.0)), hash31(i + vec3(1.0, 1.0, 0.0)), f.x), f.y),
		mix(mix(hash31(i + vec3(0.0, 0.0, 1.0)), hash31(i + vec3(1.0, 0.0, 1.0)), f.x),
			mix(hash31(i + vec3(0.0, 1.0, 1.0)), hash31(i + vec3(1.0, 1.0, 1.0)), f.x), f.y),
		f.z);
}

float fbm(vec3 p, int octaves) {
	float sum = 0.0;
	float amp = 0.5;
	float norm = 0.0;
	for (int i = 0; i < 6; i++) {
		if (i >= octaves) {
			break;
		}
		sum += vnoise(p) * amp;
		norm += amp;
		p *= 2.03;
		amp *= 0.5;
	}
	return sum / max(norm, 0.0001);
}

// A direction on the sphere from a longitude and a sine of latitude.
vec3 sphere_direction(vec2 ll) {
	float cb = sqrt(max(0.0, 1.0 - ll.y * ll.y));
	return vec3(cos(ll.x) * cb, ll.y, sin(ll.x) * cb);
}

// Cool limb (0) -> hot core (1).  The real Sun cools towards the limb, so the
// hue ramp and the intensity ramp follow the same variable.
vec3 heat_ramp(float h) {
	vec3 c = mix(limb_color, mid_color, smoothstep(0.0, 0.55, h));
	return mix(c, core_color, smoothstep(0.60, 1.0, h));
}

void vertex() {
	// SphereMesh is centred on the local origin, so the normalized position is
	// a stable direction on the surface.  NOTE: VERTEX is model space in the
	// vertex stage but view space in the fragment stage, so the world position
	// and the world normal are captured here instead of being recomputed later.
	sphere_dir = normalize(VERTEX);
	sphere_world = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
	sphere_normal_world = normalize((MODEL_MATRIX * vec4(VERTEX, 0.0)).xyz);

	// Camera geometry for the image radius.  A sphere of radius R seen from
	// distance D covers an apparent radius of R*D/sqrt(D^2-R^2), which lets the
	// fragment stage work in true image radius fractions at any camera range.
	float d = max(distance(CAMERA_POSITION_WORLD, (MODEL_MATRIX * vec4(0.0, 0.0, 0.0, 1.0)).xyz),
		sun_radius * 1.0001);
	v_distance = d;
	v_tangent = sqrt(max(d * d - sun_radius * sun_radius, 1e-6));
}

void fragment() {
	vec3 view_dir = normalize(CAMERA_POSITION_WORLD - sphere_world);
	float mu = clamp(dot(sphere_normal_world, view_dir), 0.0, 1.0);
	float t = 1.0 - mu;

	// True image radius as a fraction of the disc's apparent radius.  sin(beta)
	// alone is not enough: perspective makes the mapping non-linear, and the
	// reference frames are matched in image radius.
	float projected = sqrt(max(0.0, 1.0 - mu * mu));
	float image_radius = projected * v_tangent
		/ max(sun_radius * (v_distance - sun_radius * mu), 1e-6);

	// I(mu)/I(1) = 1 - u(1-mu) - v(1-mu)^2
	float ld = max(1.0 - limb_darkening * t - limb_darkening_sq * t * t, 0.0);

	// The solar limb is not a smooth sphere.  Measured on AIA 304 the linear
	// brightness falls to 0.41 of the centre at 0.90R, 0.095 at 0.95R and
	// 0.037 at 1.00R - far steeper than photosphere limb darkening alone,
	// because the chromosphere has a finite thickness and the line of sight
	// leaves it near the edge.
	// The limb is a little darker, never a black ring: the floor is what stops
	// a dark moat from opening up between the surface and the corona.
	float edge = limb_edge_floor
		+ (1.0 - limb_edge_floor) * pow(1.0 - smoothstep(0.86, 1.0, image_radius), 2.2);
	ld *= mix(1.0, edge, limb_edge_strength);

	// Granulation: two octaves of value noise remapped to a cellular profile.
	// A summed fbm has a narrow distribution around 0.5, which reads as soft
	// cloud rather than as granules, so the noise is remapped instead.  The
	// domain is warped by a much lower frequency field first: without the warp
	// the cells come out as axis aligned squares, which is the single most
	// obvious difference from the real filamentary network.
	float gk = granulation_cells / SUN_PI;
	vec3 gp = sphere_dir * gk;
	gp += vec3(TIME * flow_speed * 0.31, TIME * flow_speed * -0.19, TIME * flow_speed * 0.24);
	gp += vec3(seed * 1.7, seed * 0.9, seed * 2.3);
	float wk = gk * 0.16;
	vec3 warp = vec3(
		vnoise(sphere_dir * wk + vec3(11.3, 2.7, 5.1)),
		vnoise(sphere_dir * wk + vec3(27.7, 19.4, 3.3)),
		vnoise(sphere_dir * wk + vec3(41.1, 8.8, 23.6))) - 0.5;
	vec3 wgp = gp + warp * 0.35;
	float g1 = vnoise(wgp);
	float g2 = vnoise(wgp * 2.1 + vec3(5.1, 1.7, 9.3));
	float g = smoothstep(0.26, 0.74, g1 * 0.68 + g2 * 0.32);
	g = mix(0.5, g, granulation_strength);

	// The real photosphere as the base layer.  Equirectangular mapping, and the
	// texture's first and last columns match to ~0.9% so the wrap is
	// invisible; fract() keeps the sampling continuous either way.
	// The map is tiled so its structures land at granulation scale rather than
	// at the supergranulation scale they have in the original.
	float photo_u = (atan(sphere_dir.z, sphere_dir.x) * 0.15915494 + 0.5) * texture_tiling;
	float photo_v = acos(clamp(sphere_dir.y, -1.0, 1.0)) * 0.31830989 * texture_tiling;
	vec3 photo = texture(photosphere, vec2(fract(photo_u), photo_v)).rgb;
	float photo_lum = dot(photo, vec3(0.299, 0.587, 0.114));
	// The NASA map is a contrast stretched visualisation, not a true colour
	// image, so its full range is far too strong for a disc.  It is
	// compressed around its own mean and then re-expanded by the granulation
	// term, which keeps the real blisters without the posterised look.
	float g_photo = clamp(0.5 + (photo_lum - 0.60) * 0.42, 0.0, 1.0);

	// The texture carries the structure, the procedural field drifts over it.
	g = mix(g_photo, g, 1.0 - texture_blend);
	g = clamp(g + (g - 0.5) * drift_strength, 0.0, 1.0);

	// Supergranulation: the large scale network that organises the granulation
	// into bright regions and dark lanes.  The reference frames have their
	// dominant structure at roughly a quarter of the granulation wavelength.
	float sk = supergranulation_cells / SUN_PI;
	float sg = fbm(sphere_dir * sk + vec3(seed * 0.7, TIME * flow_speed * 0.08, 0.0), 2);
	sg = clamp(0.5 + (sg - 0.5) * 1.6, 0.0, 1.0);
	sg = mix(0.5, sg, supergranulation_strength);

	// Plage: the large bright active regions with white-hot cores.  They are
	// roughly 0.1 - 0.2 solar radii across, so they get their own cell count.
	float pk = plage_cells / SUN_PI;
	float pl = smoothstep(0.36, 0.76,
		fbm(sphere_dir * pk + vec3(9.1, 4.7, seed * 3.1), 3));

	// Sunspots: a dark umbra inside a lighter penumbra.
	float spot_dark = 0.0;
	float spot_heat = 0.0;
	for (int i = 0; i < 4; i++) {
		vec4 sp = spots[i];
		if (sp.w <= 0.0) {
			continue;
		}
		float ang = acos(clamp(dot(sphere_dir, sphere_direction(sp.xy)), -1.0, 1.0));
		float penumbra = exp(-pow(ang / (sp.z * 2.8), 2.0));
		float umbra = exp(-pow(ang / max(sp.z, 0.001), 2.0));
		float d = sp.w * (penumbra * 0.40 + umbra * 0.90);
		spot_dark = max(spot_dark, d);
		spot_heat = min(spot_heat, -d * 0.5);
	}

	// A flare: a fast, local brightening at an active region.
	float flare_ang = acos(clamp(dot(sphere_dir, sphere_direction(flare_data.xy)), -1.0, 1.0));
	float flare = exp(-pow(flare_ang / max(flare_data.w, 0.01), 2.0)) * flare_data.z;

	float heat = clamp(
		0.80 * (1.0 - t)
		+ (g - 0.5) * 0.55
		+ (sg - 0.5) * 0.18
		+ pl * plage_strength * 1.00
		+ spot_heat
		+ flare * 0.45,
		0.0,
		1.0);

	// The real photosphere mottles by a factor of about two, so the granulation
	// has to move the brightness a long way, not just tint it.  The
	// supergranulation contributes the large scale bright and dark regions.
	float bright = ld * (0.66 + 0.62 * g + 0.70 * sg + 1.90 * pl) * emission_strength;
	bright *= 1.0 - 0.88 * spot_dark;
	bright += flare * 3.2 * emission_strength;

	// The disc centre burns out: staring at the Sun should hurt.  The burn is
	// driven by the viewing angle, so it only happens when you look at it.
	float burn = pow(mu, 4.0) * core_burn * (0.55 + 0.65 * g);
	bright += burn * emission_strength;
	vec3 col = heat_ramp(heat);
	ALBEDO = col * bright;

	// Chromospheric rim: the chromosphere is optically thick and is seen end on
	// near the limb, so the disc gets *brighter* towards the edge in the EUV.
	// Measured on AIA 304 the limb is brighter than the disc interior.
	float rim_axis = (image_radius - 0.950) / 0.055;
	float rim = exp(-rim_axis * rim_axis) * rim_strength;
	ALBEDO += rim_color * rim * emission_strength;

	if (debug_term > 0) {
		if (debug_term == 2) {
			ALBEDO = vec3(image_radius);
		} else if (debug_term == 3) {
			ALBEDO = vec3(mu);
		} else if (debug_term == 4) {
			ALBEDO = vec3(edge);
		} else if (debug_term == 5) {
			ALBEDO = vec3(ld);
		} else {
			ALBEDO = vec3(0.125) * float(debug_term);
		}
	}
}
