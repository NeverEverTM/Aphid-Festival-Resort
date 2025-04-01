# v0.2.1

## Work In Progress
- Photo Album:
	- Camera Mode [DONE]
	- Album menu with photo grid [DONE]
	- Store album per aphid Neko-Atsume style [DONE]
	- Allow to select aphids with mouse
	- frame feature?
- Kitchen Recipe Display:
	- Display grid of recipe slots
	- Maybe a search query?
- Aphid Play
	- Play State triggers custom play behaviour when nearby play objects
	- Play objects consist of custom classes dictating what aphid and the object should do
	- Adapt items to automatically find their play behaviour and allow repeats
- Aphid Training
	- Food grants skills points [DONE]
	- Train State triggers custom behaviour when nearby training objects
	- Train objects consist of custom clases dictating what aphid and the structure should do
	- Train Structures require to find the skill being trained and by how much
- Aphid Better Breeding
	- Inherit skills [DONE]
	- New Aphid skin [WIP]

- rendering is not adjusted for high zoom out
- draw bench
- add custom video support for tv (later lol)
- add custom image signs and text signs
- icon for aphid spectating
- icon for inv change mode
- icon for reset controls
- bg for photo slots
- book bg for album

## Bug Tracker:
- Furniture clips below cliff floor, not a big deal but it annoys me big time
- Problems with how and when aphid info is displayed
- Furniture should check if it can be placed before appearing in the center
- Furniture has wonky cursor detection.
- can hit the buttons in free camera hud
## Technical
- Refactor OptionsManager to work with easily adjustable config slot modules
- add event handlers for save modules
- implement cutscene manager, which connects itself to dialog manager to manage custom animation events ([Action]animationFunction , [bool]essential)
- separate cosmetic loading interface from main resoure load from INTIALIZE_GAME_PROCESS, so MainMenu simply watches its state without being necessary of GlobalManager to showcase
## Gameplay
- freaking aphid throwing, make em spin and bounce like a rubber ball for the funnies
## Furniture and Food Ideas
- Cake Mix: Replace cake recipe with this
- Preztel: Cake Mix + Leaf
- Berry Muffin: Cake Mix + Berry
- Sweet Tea: Plain Tea + Honey
- bottle cap aphid house (interactable)
## Music
- Project Color jingle
- Aphid Death jingle
# [Full Version Changelog]

# v0.2.1
## Gameplay
- Added the photo camera mode, allows to zoom in and out, and to focus on any aphid in the resort.
	- Added the album functionality to the generations menu aswell!
	- Aphids have their own folder and album. Otherwise global ones are stored in the general album.
	- Taking a photo now shows you the result.
- [Aphid inheritance of colors has been improved back], and it should be much more interesting and less dull than last version.
- Aphids now gain(or lose!) skills from food items, mix and match to see which food works best for your own.
- Aphid stats are now shown live when near an aphid, it also finally shows their current skill level.
- New Aphid Skin available in blue eggs.
- You can now sell items in your inventory by clicking and activating sell mode, just like structures, items are sold for half their price.
- Aphid traits and preferences are now revelead in the generations album depending on how much bondship you have with them (thats what the big pink circle meter is for!)
- Added a "Pull Item" and "Align to Grid" rebindable controls.
- Added the following furniture:
	- Ant Flag
	- Boulder
	- Stump Chair
	- Lilypad (Can only be placed on water)
## Technical
- Aphid skins have been packed into a spriteatlas. a heavy boost in performance should be noticeable, however, this affects how new aphid skins can be implemented, refer to [[Atlas Packager Usage]] for more information.
- Tilemaps have been properly adjusted to not block shadow lights.
- NPC's no longer repeat the same line of dialog when set to Random, and will in fact not say it again until the entire dialogue tree is exahusted.
- Build mode binds should no longer interfere with overworld binds, allowing to bind the same key to other actions in different bind groups.
- More control prompts, buttons and shortcuts have been added for accessibility.
## Graphics & Sound
- Improved TV Display furniture piece. 
- Adjusted UI theme elements.
- Grass now sways when walked over and when left by itself.
- Updated UI SFX for buying stuff, plus added a bunch more.
- Hello everyone my name is Markiplier and welcome to Five Nights at Freddys
- Retouched some menu interfaces.
- Added a backdrop to most text in the game which i think looks nice.
- Improved spritework of torches.
- Moved the tracking of the album to the aphid focus feature (this was done in the last update but forgot to include it, it also didnt have a way to trigger it)
## Development
- Updated to Godot 4.4. 
	- With this, comes the new meta reference system, thus, all resources have been automatically given a ".uid".
	- Some changes on how collections are created.
- Various improvements to the debug console.
- Optimized several string references to use cached StringNames when proper.
- Replaced absolute paths in GlobalManager with their corresponding UID's
- Updated several instances of sound managing to use the new system and keep in memory frequent ones.
- Updated the way Screen Size is calculated to allow for modifications to camera and viewport and still retain mouse tracking
- Renamed ResortGui to FreeCameraManager, as its purpose wasnt as resort general but more free camera mode focused.
- New Atlas packager utility, GUI included, please revise [[Atlas Packager Usage]] if you want to implement a skin or item.
- MenuUtil has been revamped to include better control over menu behaviour.
- Added an event handler for when the game binds a new action control.
- Moved some scenes to new folders plus added a new [Resources] folder for misc godot-specific resources.
- Created a CameraManager, which takes all the camera code from GlobalManager and FreeCameraManager and puts it in one place.
## Fixed
- You can now close the build menu tab without exiting the menu itself.
- Pickyeaters can now eat neutral-flavoured foods aswell.
- Particle manager would make other classes throw errors if they attempted to spawn particles over the particle limit. This should no longer happen.
- Aphids that were breeding would sometimes "T-Pose" towards their breed target, this should be fixed.
- Bee flag structure had the wrong id, which turned it into a rug after game reload, the way ids are set has been changed to prevent such things from happening in the future.
- Build menu did not use the standard method for spawning structures and caused issues when loading these new structures.
- Leaves no longer darken when the player goes under a sunshade.
- Furniture hitboxes were off by a few pixels of their actual visual sprite
