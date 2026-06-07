Creating an item is easy:
- Within the editor, go to the tabs above, Project >  Tools > View Item Database...
- From there you can easily create an item and add its translations
- Add an icon in [res://sprites/icons] before or after the item creation, icons are 16x16 for inventory items and can be bigger if they are structures
- Optionally, you may create a custom scene inheriting from [item.tscn] and add it to [res://databases/items], from there, your item will spawn the custom scene instead, allowing you to implement custom behaviour within that node 