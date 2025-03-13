shader_type canvas_item;
uniform sampler2D noise: repeat_enable;

uniform float scroll_speed : hint_range(0, 1) = 0.1;
uniform float buoyancy = 2.0;
uniform vec2 tile_size = vec2(16.0, 16.0);
uniform int pixelization_strength : hint_range(1,200) = 40;
uniform float sensitivity : hint_range(0,1) = 0.1;
uniform float sensitivity_reflection : hint_range(0,1) = 0.9;
uniform vec4 foam_color : source_color = vec4(1.0);
uniform vec4 shadow_color : source_color = vec4(1.0);
uniform vec4 reflection_color : source_color = vec4(1.0);

varying vec2 vert;

void vertex() {
	VERTEX += vec2(0, buoyancy - sin(TIME) * buoyancy);
	vert = VERTEX * 0.5;
}

void fragment() {
	if (COLOR.a != 0.0)
	{
		// get tile texture and pixelize
		vec2 full_uv = round(vert/tile_size * float(pixelization_strength)) / float(pixelization_strength);

		// create motion and get a sample from the noise with it applied
		vec2 scroll = vec2(full_uv.x + TIME * scroll_speed, -full_uv.y + -sin(TIME) * scroll_speed);
		vec4 noise_sample = texture(noise, scroll);
		float _sens_refl_sample = sensitivity_reflection + 0.05 * cos(TIME);
		// we check sensivity to draw reflection, foam and shadow, overriding going from first to last
		if (noise_sample.r > _sens_refl_sample && noise_sample.b > _sens_refl_sample && noise_sample.g > _sens_refl_sample)
			COLOR = reflection_color;
		else if (noise_sample.r < sensitivity && noise_sample.b < sensitivity && noise_sample.g < sensitivity)
			COLOR = foam_color;
		else
		{
			// apply the same logic for shadows
			vec2 scroll_b = vec2(-(full_uv.x + TIME * scroll_speed), full_uv.y + sin(TIME) * scroll_speed);
			vec4 noise_sample_b = texture(noise, scroll_b);

			if (noise_sample_b.r < sensitivity && noise_sample_b.b < sensitivity && noise_sample_b.g < sensitivity)
				COLOR = shadow_color;
		}
	}
}
