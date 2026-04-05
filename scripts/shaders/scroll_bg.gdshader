// SCROLLS TEXTURES BASED ON DETERMINATED DIRECTION AND SIZE, USED FOR UI BACKGROUNDS
shader_type canvas_item;
render_mode unshaded;

global uniform float accesibility_menubgwaviness;

uniform float speed = 0.05;
uniform vec2 direction = vec2(-1.0, 1.0);
uniform vec2 node_size = vec2(1.0, 1.0);
uniform vec4 color : source_color = vec4(1.0);
uniform bool useModulateAlpha = true;

void fragment() {
	vec2 full_uv = UV * (1.0 / node_size);
	vec2 scroll = full_uv + (-direction) * TIME * (speed * accesibility_menubgwaviness);
	vec4 _sample = texture(TEXTURE, scroll);
	_sample *= color;
	if (useModulateAlpha)
		_sample.a = COLOR.a;
	COLOR = _sample;
}