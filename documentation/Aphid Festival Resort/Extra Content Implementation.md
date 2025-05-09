## Food & Recipe Implementation
To implement a food or recipe, they require the following:
- assigned gameplay values need to be inserted in the [foods_values] table
- [recipe only] assigned gameplay values need to be inserted in the [recipes_values] table
- foodname_name and foodname_desc translation keys in [items_translation].
- icon to display itself, which is added to [res://sprites/icons] as an atlas texture or a png.
- [Optional] you can skip the icon by directly creating the item as a custom packed scene inherting from the [item.tscn] node and place in the res://databases/items.

## Structure Implementation
To implement  a structure:
- Include a packed scene in the structures folder in res://databases
  - If they are used to pre-decorate a modifieable area (like adding furniture to the resort on the editor), it must have a metadata "id" with their corresponding ID as a string value. 
  - Root must be a Sprite2D, AnimatedSprite2D or have the metadata "size" and "offset" for its build rect to be correctly built during build mode.
- Have an entry in the [items.csv] with the corresponding ID. Check the tag classification in the second tab.
- Have two entries in the translation file [structures.csv], one for its name ("id_name") and another for its description ("id_desc"), where id is replaced with the corresponding ID.