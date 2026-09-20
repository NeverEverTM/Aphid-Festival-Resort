shader_type canvas_item;
render_mode unshaded;
//
//varying vec3 screen_pos;
//varying mat4 canvas_matrix;

float color_to_float(vec4 _color)
{
	return (_color.r + _color.g + _color.b) / 3.0;
}
bool is_transparent(vec4 _color){
	return _color.a < 0.01;
}

uniform float wave_width = 1.0;
uniform float wave_frequency = 1.0;
uniform float wave_speed = 1.0;
uniform vec4 color : source_color;

void fragment()
{
	COLOR = color * texture(TEXTURE, UV + vec2(SCREEN_PIXEL_SIZE.x * wave_width * sin((UV.y + TIME * wave_speed) * wave_frequency), 0));
	//if (color_to_float(COLOR) < 1.0)
	//{
		//vec4 right = texture(TEXTURE, UV + vec2(TEXTURE_PIXEL_SIZE.x, 0));
		//vec4 left = texture(TEXTURE, UV + vec2(-TEXTURE_PIXEL_SIZE.x, 0));
		//if (!is_transparent(COLOR) && is_transparent(left))
			//COLOR = vec4(1.0);
	//}
}

//void vertex() {
	//canvas_matrix = CANVAS_MATRIX;
	//screen_pos = (CANVAS_MATRIX * MODEL_MATRIX * vec4(VERTEX, 0, 1)).xyz;
//}
// Called for every pixel for every light affecting the CanvasItem.
//void light() {
	//vec4 sprite_pos = inverse(canvas_matrix) * vec4(LIGHT_VERTEX, 1.0);
	//vec4 light_y_pos = inverse(canvas_matrix) * vec4(LIGHT_POSITION, 1.0);

	//vec4 neighbour = texture(TEXTURE, UV + vec2(0, -TEXTURE_PIXEL_SIZE.y));
	//LIGHT = COLOR * LIGHT_COLOR;
	
	//if (LIGHT_DIRECTION.g < 0.0)
	//{
		//if (neighbour.a < 0.1)
		//{
			//LIGHT = COLOR;
			//LIGHT.a = min(50.0 / distance(sprite_pos, light_y_pos), 1.0);
		//}
		//else
		//{
			//LIGHT = COLOR * LIGHT_COLOR;
			//LIGHT.a = min(10.0 / distance(sprite_pos, light_y_pos), 1.0);
		//}
	//}
	//else
	//{
		//if (neighbour.a > 0.0)
		//{
			//LIGHT = COLOR;
			//LIGHT.a = min(30.0 / distance(sprite_pos, light_y_pos), 1.0);
		//}
	//}
	
	
	//if (COLOR.a > 0.0 && right_neighbor_color.a < 0.1 && left_neighbor_color.a > 0.0)
		//COLOR = vec4(1.0);
	
	//LIGHT.a = clamp(100.0 / distance(light_y_pos, sprite_pos), 0.0, 1.0);
	
		//if (sprite_pos.x < light_y_pos.x + size && sprite_pos.y < light_y_pos.y + size){
		//if (sprite_pos.x > light_y_pos.x - size && sprite_pos.y > light_y_pos.y - size)
		//LIGHT = vec4(1.0);
	//}
	//if (screen_pos.x < LIGHT_POSITION.x + 10.0 && screen_pos.y < LIGHT_POSITION.y + 10.0){
		//if (screen_pos.x > LIGHT_POSITION.x - 10.0 && screen_pos.y > LIGHT_POSITION.y - 10.0)
		//LIGHT = vec4(1.0);
	//}
//}
//void fragment()
//{
	//vec4 right_neighbor_color = texture(TEXTURE, UV + vec2(TEXTURE_PIXEL_SIZE.x, 0));
	//vec4 left_neighbor_color = texture(TEXTURE, UV + vec2(-TEXTURE_PIXEL_SIZE.x, 0));
	//if (COLOR.a > 0.0 && right_neighbor_color.a < 0.1 && left_neighbor_color.a > 0.0)
		//COLOR = vec4(1.0);
//}