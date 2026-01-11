// DELTARUNE-STYLE PROPHECY PANEL, BECAUSE I'M THAT BORED
shader_type canvas_item;
render_mode unshaded;

const vec4 void_color = vec4(0.0);
const vec4 dark_color = vec4(1.0,1.0,1.0,0.25);

uniform sampler2D sample : repeat_enable;
uniform float speed = 1.0;
uniform vec4 outline_color : source_color;

uniform float hologram_swing : hint_range(0, 10) = 5;
uniform float hologram_transparency : hint_range(0, 1) = 0.5;
uniform vec2 hologram_direction = vec2(1.0, 1.0);
uniform int hologram_pixelization : hint_range(1,200) = 40;

vec4 render_background(vec2 uv, vec2 screen_uv){
	vec2 scroll_bg = screen_uv * vec2(0.4,0.5) + hologram_direction * TIME * (speed / 100.0);
	scroll_bg = round(scroll_bg * float(hologram_pixelization)) / float(hologram_pixelization);
	vec4 texture_sample_bg = texture(sample, scroll_bg);
	// fade color as you approach the edges
	if (texture_sample_bg.r > 0.3 && texture_sample_bg.g > 0.3 && texture_sample_bg.b > 0.3)
	{
		texture_sample_bg.a = (0.7 - (abs(uv.x - 0.5) + abs(uv.y - 0.5))) - 0.2;
		return texture_sample_bg;
	}
	else // colors under certain threshold dont get rendered
		return void_color;
}

vec4 get_holotexture(vec2 uv, vec2 screen_uv, sampler2D texture){
	vec2 scroll = screen_uv + (-hologram_direction) * TIME * (speed / 50.0);
	scroll = round(scroll * float(hologram_pixelization)) / float(hologram_pixelization);
	return texture(sample, scroll);
}

vec4 get_holouv(vec2 uv, sampler2D texture, float swing_distance){
	vec2 uv_hologram = uv;
	uv_hologram = uv_hologram - vec2(swing_distance,swing_distance) * sin(TIME * speed);
	return texture(texture, uv_hologram);
}

void vertex() {
	VERTEX += vec2(0, sin(TIME * speed));
}

bool is_same_color_as_outline(vec4 _color)
{
	if (_color.r == outline_color.r &&
		 _color.g == outline_color.g &&
		 _color.b == outline_color.b)
		return true;
	else
		return false;
}

void fragment(){
	vec4 texture_sample = get_holotexture(UV, SCREEN_UV, TEXTURE);

	if (COLOR.a == 0.0 || is_same_color_as_outline(COLOR)) // attempt to render hologram
	{
		// apply color attributes
		texture_sample *= dark_color;
		texture_sample.a = hologram_transparency;

		// get swing position of first hologram
		float swing_a = hologram_swing / 100.0;
		vec4 current_texture_uv = get_holouv(UV, TEXTURE, swing_a);

		// attempt to render first hologram
		if (current_texture_uv.a > 0.0 && !is_same_color_as_outline(current_texture_uv))
			COLOR = texture_sample;
		else {
			// get swing position of second hologram
			float swing_b = hologram_swing / 60.0;
			vec4 current_texture_uv_b = get_holouv(UV, TEXTURE, swing_b);

			// attempt to render second hologram
			if (current_texture_uv_b.a > 0.0 && !is_same_color_as_outline(current_texture_uv_b))
			{
				texture_sample.a = hologram_transparency * 0.5;
				COLOR = texture_sample;
			}
			 else // otherwise, render the texture background
				COLOR = render_background(UV, SCREEN_UV);
		}
	}
	else
		COLOR = texture_sample; // actual sprite with scrolled texture applied
}