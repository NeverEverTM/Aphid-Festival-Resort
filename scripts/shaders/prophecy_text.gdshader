// DELTARUNE-STYLE PROPHECY PANEL, BECAUSE I'M THAT BORED
shader_type canvas_item;
render_mode unshaded;

uniform sampler2D sample : repeat_enable;
uniform float speed = 1.0;
uniform vec2 scroll_direction = vec2(1.0, 1.0);
uniform int hologram_pixelization : hint_range(1,200) = 40;

vec4 get_holotexture(vec2 uv, vec2 screen_uv, sampler2D texture)
{
	vec2 scroll = screen_uv + (-scroll_direction) * TIME * (speed / 50.0);
	scroll = round(scroll * float(hologram_pixelization)) / float(hologram_pixelization);
	return texture(sample, scroll);
}

vec4 get_holouv(vec2 uv, sampler2D texture, float swing_distance)
{
	vec2 uv_hologram = uv;
	uv_hologram = uv_hologram - vec2(swing_distance,swing_distance) * sin(TIME * speed);
	return texture(texture, uv_hologram);
}

void vertex() {
	VERTEX += vec2(0, sin(TIME * speed)) * 2.0;
}

void fragment(){
	vec4 texture_sample = get_holotexture(UV, SCREEN_UV, TEXTURE);
	if (COLOR == vec4(1.0,1.0,1.0,1.0))
		COLOR = texture_sample; // actual sprite
}