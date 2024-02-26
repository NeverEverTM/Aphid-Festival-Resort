shader_type canvas_item;
uniform sampler2D noise: repeat_enable;

uniform float speed = 0.1;
uniform vec2 node_size = vec2(400.0, 400.0);
varying vec2 vert;
varying vec2 world_pos;

void vertex() {
	VERTEX += vec2(0, 2.0 - sin(TIME)*2.0);
	vert = VERTEX * 0.5;
	world_pos = (MODEL_MATRIX * vec4(VERTEX, 1.0, 1.0)).xy;
}

void fragment() {
	vec2 full_uv = vert/node_size;
	vec2 scroll = vec2(full_uv.x + TIME * speed, full_uv.y + TIME * speed);
	vec4 noise_sample = texture(noise, scroll);
	scroll *= distance(world_pos, full_uv);
	
	noise_sample.rgb /= 4.0;
	noise_sample.rgb -= 0.1 - sin(TIME) * 0.1;
	noise_sample.b = 0.75;
	noise_sample.a = 0.0;
	
	COLOR += noise_sample;
}
