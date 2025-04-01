New aphid skins and items can be drawn and implemented rather easily using the atlas packager as a tool.

### Step 1
[For Skins]
You need to open the default template for aphid skins in [documentation/aphid_skin.kra] (it only opens in Krita but you can copy the template's structure to another program afterwards) and save the file as a brand new file.

The structure for the templates is composed by the "skin" containing another two groups for "[adult]" aphids and "[baby]" aphids. The layers inside this can be modified at your heart's content with some ruling:
	- Layers [cannot be bigger than 64x64] px due to limitations.
	- [You cannot create more skin piece layers] than there is. Any temp layer you use should be hidden/removed before export.
	- Use whites and light greys to allow for aphid coloring to work correctly.
	- Respect positioning of the skin pieces, as too much of an offset will look awkward on the final render.
	- The "color overlay" and "background" layers can be modified however you want, as long as you also hide/remove them before export.
[For Items]
You can simply create a 18x18 image and draw the item, make sure to leave empty the margin by 1 pixel to avoid bleeding.
### Step 2

Now for the import, each layer must be its own individual image, an equivalent in your program must be found, make sure to hide the before mentioned ["color_overlay"] and ["background"] layers, as well as any other extra debug layer. [You will need to rename the "skin" group to a number ID, this can be any number as long as is not a duplicate.]
The process goes as follows:
	- Go to "Tools > Scripts > Export Layers"
	- Select in "Documents" the template you were working on
	- Set "Initial Directory" to where you want to save the export.
	- Set "Ignore Invisible layers" to true
	- Set "Images extensions" to PNG
	- [(FOR EVERY OTHER PROGRAM)] It's important that you find the equivalents to the above when exporting your layers, such as setting to PNG and removing hidden layers.
- After this, you should have a folder with the name of the file you were working on. We will be using the folder inside of that instead, the one that [has the ID as its name].
- Enter the godot project, and go to "Project > Tools > Generate Atlas & Textures...", if it does not appear, go instead to "Project > Project Settings > Plugins" and make sure "Export Image Folders" is on.
- A GUI for the atlas packer will popup, you will need to assign each folder
### Step 3
- Now with all skin folders selected, instead of selecting a folder, you will press the "Generate Atlas & Textures..." button located in the same tab. Red error text means something went wrong, check the debugger for more information. Otherwise, the green text in the debugger should tell you what to do next.
- After all this, you can now export layers, run the generate atlas command again, and instantly see the results. In order to see them during run-time, make sure to run the command "reload" in the debug console.
