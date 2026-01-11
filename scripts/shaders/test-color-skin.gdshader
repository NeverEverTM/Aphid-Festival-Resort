// TEST SHADER FOR PLAYER CUSTOMIZATION PURPOSES
shader_type canvas_item;
uniform vec4 color : source_color = vec4(1.0);
uniform vec4 color1 : source_color = vec4(1.0);

void fragment() {
	if (COLOR == color)
		COLOR = color1;
}