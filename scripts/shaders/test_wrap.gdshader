shader_type canvas_item;
render_mode unshaded;

uniform float speed = 0.05;
uniform vec2 direction = vec2(-1.0, 1.0);
uniform vec2 node_size = vec2(1.0, 1.0);
uniform vec4 color : source_color = vec4(1.0);

const vec2 local_uv[] = {vec2(0,0),vec2(0,1),vec2(1,1),vec2(1,0)};

varying vec2 LOCAL_UV;

void vertex() {
	LOCAL_UV = local_uv[VERTEX_ID];
}

void fragment() {
	vec2 _size = node_size;
	_size.y = node_size.y * (1.0 + (abs(LOCAL_UV.y - 0.5) / 0.5));

	vec2 full_uv = UV * (1.0 / _size);
	vec2 scroll = full_uv + (-direction) * TIME * speed;
	vec4 _sample = texture(TEXTURE, scroll);
	_sample *= color;
	COLOR = _sample;
}