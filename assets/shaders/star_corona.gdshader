shader_type spatial;
render_mode unshaded, blend_add, depth_draw_never, cull_disabled, shadows_disabled, fog_disabled;

uniform vec4 corona_color : source_color = vec4(1.0, 0.42, 0.12, 1.0);
uniform float brightness : hint_range(0.0, 5.0) = 0.65;
uniform float width : hint_range(0.05, 0.65) = 0.3;
uniform float speed : hint_range(0.0, 1.0) = 0.08;
uniform float irregularity : hint_range(0.0, 1.0) = 0.55;
uniform float seed = 0.0;

// The quad half-width is twice the sphere radius (see StarCorona.cs).
// Noise sampled on a circle avoids the seam produced by angle-based UVs.
float hash21(vec2 p) {
    p = fract(p * vec2(123.34, 456.21));
    p += dot(p, p + 45.32);
    return fract(p.x * p.y);
}

float noise2(vec2 p) {
    vec2 cell = floor(p);
    vec2 f = fract(p);
    f = f * f * (3.0 - 2.0 * f);
    return mix(mix(hash21(cell), hash21(cell + vec2(1.0, 0.0)), f.x),
               mix(hash21(cell + vec2(0.0, 1.0)), hash21(cell + vec2(1.0)), f.x), f.y);
}

void vertex() {
    // Face the camera position, retaining uniform parent scale. The sphere
    // depth-occludes the middle; foreground geometry still occludes the halo.
    vec3 center = MODEL_MATRIX[3].xyz;
    vec3 facing = normalize(CAMERA_POSITION_WORLD - center);
    vec3 up_hint = INV_VIEW_MATRIX[1].xyz;
    vec3 right = normalize(cross(up_hint, facing));
    vec3 up = cross(facing, right);
    float scale = length(MODEL_MATRIX[0].xyz);
    MODELVIEW_MATRIX = VIEW_MATRIX * mat4(
        vec4(right * scale, 0.0), vec4(up * scale, 0.0),
        vec4(facing * scale, 0.0), vec4(center, 1.0));
}

void fragment() {
    vec2 p = (UV * 2.0 - 1.0) * 2.0;
    float radius = length(p);
    vec2 direction = p / max(radius, 0.001);
    float t = TIME * speed;
    vec2 offset = vec2(seed * 0.37, seed * 0.73);
    float broad = noise2(direction * 5.0 + offset + vec2(t, -t * 0.6));
    float fine = noise2(direction * 15.0 + offset + vec2(-t * 1.3, t * 0.8));
    float extension = width * mix(1.0, 0.45 + broad * 1.3, irregularity);
    float distance_from_edge = max(radius - 1.0, 0.0);
    float halo = exp(-distance_from_edge / max(extension, 0.001) * 3.0);
    float wisps = mix(1.0, 0.35 + fine * 0.95, irregularity);
    // Overlap the opaque sphere slightly; do not cut a visible gap at its rim.
    float inner_fade = smoothstep(0.85, 1.0, radius);
    float outer_fade = 1.0 - smoothstep(1.75, 1.95, radius);
    ALBEDO = corona_color.rgb * brightness;
    ALPHA = clamp(halo * wisps * inner_fade * outer_fade * corona_color.a, 0.0, 1.0);
}
