shader_type canvas_item;

uniform float speed = 1.0;

void fragment()
{
	vec2 scroll = UV + vec2(0.0, 1.0) * TIME * speed;
	if (roundEven(mod(scroll.y * 100.0, 2.0)) == 1.0)
		COLOR = COLOR;
	else if (roundEven(mod(scroll.y * 100.0 + scroll.y, 2.0)) == 1.0)
		COLOR.r = 1.0;
}