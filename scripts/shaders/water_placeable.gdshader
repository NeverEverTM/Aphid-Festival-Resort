shader_type canvas_item;
uniform sampler2D screen_texture: hint_screen_texture;
uniform float frequency = 2.0;

void vertex() {
	VERTEX += vec2(0, sin(TIME) * frequency);
}