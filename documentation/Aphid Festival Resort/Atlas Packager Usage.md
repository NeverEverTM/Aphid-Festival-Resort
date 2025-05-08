New aphid skins and items can be drawn and implemented rather easily using the atlas packager as a tool.

TODO: It nees now an update for the gui version...
### Step 1
[For Skins]
You need to open the default template for aphid skins in [documentation/aphid_skin.kra] (it only opens in Krita but you can copy the template's structure to another program afterwards) and save the file as a brand new file.

The structure for the templates is composed by the "skin" containing another two groups for "[adult]" aphids and "[baby]" aphids. The layers inside this can be modified at your heart's content with some ruling:
	- Layers [cannot be bigger than 64x64] px due to size limitations.
	- Use the given black and white pallette to allow for aphid coloring to work correctly.
	- Respect positioning of the skin pieces, as too much of an offset will look awkward on the final skin renders.
	- [You cannot create more skin piece layers] than there is. Any temp layer you use should be hidden/removed before export.
	- The "color overlay" and "background" layers are temp layers, so one can do whatever they want with them, but they also must be hidden/removed before export.
[For Items]
You can simply create a 18x18 image and draw the item, making sure to leave a margin of one pixel to avoid bleeding.
### Step 2
Now for the import, we require each layer to be its own individual image make sure to [not] include any non-skin layer.
All of this will be kept in structure, but as one folder with other subfolders inside instead.
Rename the resulting folder with the number ID, this number should be a succeding one according to the order in the skins folder. (So if the last skin folder is 5, yours is 6, and so on)
### Step 3
Enter the editor, and go into ["Project > Tools > Generate Atlas & Textures..."], if it does not appear, go instead to ["Project > Project Settings > Plugins"] and make sure "Export Image Folders" is on.
 A GUI for the atlas packer will popup, everything is pretty self explanatory.
- Any and [all folders to be included] in this export
- An atlas path ([including the name of the atlas file itself])
- An output path (where the [folder] with [all the ref textures] will reside)
- And the size of all images (ex. skins are 64x64 px like said before)