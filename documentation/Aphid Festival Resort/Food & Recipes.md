
Revise sheet for food values:
![[foods_values.ods]]
Translation file for food names can be found in the general item sheet:
![[items_translation.ods]]
Revise sheet for recipe combinations here:
![[recipes_values.ods]]
# Food Implementation
To implement a food recipe, it needs three things:
- assigned gameplay values, can be put in the [foods_values] table
- foodname_name and foodname_desc translation keys in [items_translation]
- icon to display itself, this is done in the editor through the [res://sprites/icons]
- [Optional] It can be a packed scene instead in the res://databases/items folder for custom behaviour
# Recipe Implementation
To implement a recipe it needs three things:
- assigned gameplay values, can be put in the [recipes_values] table
- recipename_name and recipename_desc translation keys in [items_translation]
- icon to display itself, this is done in the editor through the [res://sprites/icons]
- [Optional] It can be a packed scene instead in the res://databases/items folder for custom behaviour

| ***Pending Food:*** | food_value | drink_value | flavour_type |
| ------------------- | ---------- | ----------- | ------------ |
| tangy_berry         | 10         | 10          | sour         |
| dark_cherries       | 25         | 25          | bitter       |
| aphid_dew           | 0          | 5           | sweet        |
| tangy_carpaccio     | 50         | 15          | sour         |

- spicy_berry is currently in the translation sheet but neither implemented as an item or as food, it is also missing an icon