// DECORATIVE WAVE SHADER
shader_type canvas_item;
render_mode unshaded;

uniform float wave_frequency : hint_range(0.0, 10.0, 0.1) = 1.0;
uniform float line_width : hint_range(0.0, 1.0, 0.01) = 0.5;
uniform float speed = 1.0;
uniform float offset : hint_range(-0.5,0.5,0.01) = 0.0;
uniform float wave_range : hint_range(0.0,1.0,0.01) = 1.0;

uniform int colors_used : hint_range(0,5,1) = 5;
uniform vec4 bg_color : source_color;
uniform vec3 colors[5] : source_color;

void fragment()
{
	float a = COLOR.a;
	float _time = TIME * speed;
	float _halfLineWidth = 0.3 * line_width;
	float _fullLineWidth = _halfLineWidth * 2.0;
	float _waveFrequency = UV.x * wave_frequency;
	 // ondulating center going from 0 to 1
	float waveY = sin(_time + _waveFrequency) * (0.5 * wave_range) + (0.5 + offset);
	// add offset according to number of colors needed
	waveY = waveY - _fullLineWidth * ((float(colors_used) / 2.0) - 0.5);

	COLOR = bg_color;
	for(int i = 0; i < colors_used; i++)
	{
		if (UV.y > waveY - _halfLineWidth && UV.y < waveY + _halfLineWidth)
			COLOR.rgb = colors[i];
		waveY = waveY + _halfLineWidth * 2.0;
	}
	COLOR.a = a;
}