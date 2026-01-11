// shader used by grass and other wind-affected items
shader_type canvas_item;
render_mode world_vertex_coords;

const vec2 noise_direction = vec2(1.0);
global uniform sampler2D noise : repeat_enable;
global uniform float wind;

uniform float sway : hint_range(1.0, 100.0) = 1.0;

void vertex() {
	vec3 world_vertex = (MODEL_MATRIX * vec4(VERTEX, 1.0, 1.0)).xyz;
	vec2 scroll = UV + (-noise_direction) * ((TIME + world_vertex.x) * wind / 100.0);
	vec4 sample = texture(noise, scroll);
	VERTEX += vec2(sin(sample.r - 0.3) * (1.0 - UV.y) * sway, 0);
}