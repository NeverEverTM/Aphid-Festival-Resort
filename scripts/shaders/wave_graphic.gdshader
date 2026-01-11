// OLD VERSION OF WATER
shader_type canvas_item;
render_mode unshaded;
uniform vec4 top_wave_color : source_color;
uniform vec4 middle_wave_color : source_color;
uniform vec4 bottom_wave_color : source_color;
uniform vec4 bg_color : source_color;
uniform float line_width : hint_range(0.0, 0.5, 0.01) = 0.05;
uniform float width : hint_range(0.0, 100.0, 0.1) = 1.0;
uniform float speed = 1.0;

const vec2 local_uv[] = {vec2(0,0),vec2(0,1),vec2(1,1),vec2(1,0)};

varying vec2 LOCAL_UV;

void vertex() {
	LOCAL_UV = local_uv[VERTEX_ID];
}

void fragment()
{
	float a = COLOR.a;
	// we get the wave value by applying a sin wave to time
	// then offset it with the current X coordinate to skip ahead and create the ondulated effect
	float waveY = sin((TIME * speed + LOCAL_UV.x * width) + 1.0) * 0.25 + 0.35;
	float total_width = line_width - (1.0 - line_width);

	if (COLOR.a > 0.0 && COLOR.r > 0.01 && COLOR.g > 0.01 && COLOR.b > 0.01)
	{
		COLOR = bg_color;
		// top wave
		if (LOCAL_UV.y > waveY - line_width && LOCAL_UV.y < waveY + line_width)
			COLOR = top_wave_color;
		// middle wave
		waveY = waveY + line_width * 2.0;
		if (LOCAL_UV.y > waveY - line_width && LOCAL_UV.y < waveY + line_width)
			COLOR = middle_wave_color;
		// bottom wave
		waveY = waveY + line_width * 2.0;
		if (LOCAL_UV.y > waveY - line_width && LOCAL_UV.y < waveY + line_width)
			COLOR = bottom_wave_color;

		COLOR.a = a;
	}
}