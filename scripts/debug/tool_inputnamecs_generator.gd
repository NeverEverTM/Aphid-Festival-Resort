@tool
extends EditorScript

func _run() -> void:
	# Load all inputs we have
	InputMap.load_from_project_settings()
	var inputs = InputMap.get_actions().filter(func(input_name:StringName): return input_name.find(".") == -1)
	
	# Filter the built-in inputs (those who start with "ui")
	inputs = inputs.filter(func(input_name:StringName): return not input_name.begins_with("ui"))
	
	# Produce the contents by mapping the list and creating a C# field for each entry   
	var inputs_string = "\n	".join(inputs.map(func(input):
		return 'public readonly static StringName {var_name} = new("{name}");'.format({"name": input,"var_name" : input.to_pascal_case() })))
		
	var script_content = \
"""
using Godot;
// This script is automatically generated from 'res://scripts/debug/tool_inputnamecs_generator.gd'
public static class InputNames
{
	{inputs}
} 
""".format({"inputs": inputs_string})

	var script = CSharpScript.new()
	script.source_code = script_content
	ResourceSaver.save(script, 'res://scripts/utils/InputNames.cs')
	print("Succesfully imported input map as C# static singleton")
