# Food Implementation
To implement a food recipe, it needs three things:
- assigned gameplay values need to be inserted in the [foods_values] table
- foodname_name and foodname_desc translation keys in [items_translation]
- icon to display itself, which is added to [res://sprites/icons] as an atlas texture or a png
- [Optional] you can skip the icon by directly creating the item as a custom packed scene inherting from the [item.tscn] node and place in the res://databases/items.
# Recipe Implementation
To implement a recipe it needs three things:
- assigned gameplay values need to be inserted in the [foods_values] table
- assigned gameplay values need to be inserted in the [recipes_values] table
- recipename_name and recipename_desc translation keys in [items_translation]
- icon to display itself, this is done in the editor through the [res://sprites/icons]
- [Optional] you can skip the icon by directly creating the item as a custom packed scene inherting from the [item.tscn] node and place in the res://databases/items.

Revise this sheet for food values:
![[foods_values.ods]]
Translations for food items can be found in the general item tr table:
![[items_translation.ods]]
Revise this sheet for recipe combinations here:
![[recipes_values.ods]]
