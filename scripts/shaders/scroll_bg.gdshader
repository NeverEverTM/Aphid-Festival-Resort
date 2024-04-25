shader_type canvas_item;

uniform float speed = 0.05;
uniform vec2 node_size = vec2(0.0, 0.0);
uniform vec4 color : source_color = vec4(1.0);
varying vec2 vert;

void vertex() {
	VERTEX += vec2(0, 2.0);
	vert = VERTEX * 0.5;
}

void fragment() {
	vec2 full_uv = vert/node_size;
	vec2 scroll = vec2(full_uv.x - TIME * speed, full_uv.y + TIME * speed);
	vec4 _sample = texture(TEXTURE, scroll);
	_sample *= color;
	_sample.a = COLOR.a;
	COLOR = _sample;
}