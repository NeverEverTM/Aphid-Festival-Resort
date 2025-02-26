shader_type canvas_item;
uniform float speed = 2;
uniform vec4 flashing_color  : source_color = vec4(1.0);

void fragment() {
	if (COLOR.a == 1.0)
		COLOR = mix(COLOR, flashing_color, max(sin(TIME * speed), 0.0));
}
