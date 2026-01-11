// SCROLLS TEXTURES BASED ON DETERMINATED DIRECTION AND SIZE, USED FOR UI BACKGROUNDS
shader_type canvas_item;
render_mode unshaded;

uniform float speed = 0.05;
uniform vec2 direction = vec2(-1.0, 1.0);
uniform vec2 node_size = vec2(1.0, 1.0);
uniform vec4 color : source_color = vec4(1.0);

void fragment() {
	vec2 full_uv = UV * (1.0 / node_size);
	vec2 scroll = full_uv + (-direction) * TIME * speed;
	vec4 _sample = texture(TEXTURE, scroll);
	_sample *= color;
	COLOR = _sample;
}