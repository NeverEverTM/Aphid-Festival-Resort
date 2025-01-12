shader_type canvas_item;
uniform vec4 color : source_color = vec4(1.0);
uniform vec4 color1 : source_color = vec4(1.0);

void vertex() {
	// Called for every vertex the material is visible on.
}

void fragment() {
	if (COLOR == color)
		COLOR = color1;
}

//void light() {
	// Called for every pixel for every light affecting the CanvasItem.
	// Uncomment to replace the default light processing function with this one.
//}
