shader_type canvas_item;
render_mode world_vertex_coords;

uniform sampler2D map;
uniform float bottom_sway : hint_range(0.0, 1.0) = 0.0;
uniform float top_sway : hint_range(0.0, 1.0) = 0.0;

void vertex() {
	vec3 world_vertex = (MODEL_MATRIX * vec4(VERTEX, 1.0, 1.0)).xyz;
	vec4 sample = texture(map, UV * world_vertex.x);
	
	float sway = UV.y;
	if (UV.y < 0.5)
		sway += top_sway;
	sway *= bottom_sway * 1000.0;
	VERTEX += vec2(sin(sample.a - 1.0) * sway, 0);
}
