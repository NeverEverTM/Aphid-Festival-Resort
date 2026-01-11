// HIGHLIGHT SHADER FOR INVENTORY ITEMS
shader_type canvas_item;
uniform float speed = 2;
uniform vec4 flashing_color  : source_color = vec4(1.0);

void fragment() {
	float new_a = COLOR.a;
	if (COLOR.a > 0.1)
		COLOR = mix(COLOR, flashing_color, max(sin(TIME * speed), 0.0));
	COLOR.a = new_a;
}
