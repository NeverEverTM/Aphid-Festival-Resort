@tool
extends EditorScript

func _run() -> void:
	InputMap.load_from_project_settings()
	var inputs = InputMap.get_actions().filter(func(input_name:StringName): return input_name.find(".") == -1)
	inputs = inputs.filter(func(input_name:StringName): return not input_name.contains("ui"))
	var inputs_string = "\n	".join(inputs.map(func(input):
		return 'public const string {var_name} = "{name}";'.format({"name": input,"var_name" : input.to_pascal_case() })))
	var script_content = \
"""
public static class InputNames
{
	{inputs}
} 
""".format({"inputs": inputs_string})

	print(script_content)

	var script = CSharpScript.new()
	script.source_code = script_content
	ResourceSaver.save(script, 'res://scripts/utils/InputNames.cs')
