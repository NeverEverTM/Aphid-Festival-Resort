To implement  a structure:
- Include a packed scene in the structures folder in res://databases
  - Must have a metadata "id" with their corresponding ID
  - Root must be a Sprite2D, AnimatedSprite2D or have the metadata "size" and "offset" for correct selection when clicking over the structure
- Needs a structure icon in the icons folder in res://sprites
- Have an entry in the [items.csv] with the corresponding ID
- Have an entry in the translation file [structures.csv]