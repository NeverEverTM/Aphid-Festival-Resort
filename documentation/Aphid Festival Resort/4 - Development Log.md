# v0.2.1

## Work In Progress
- Photo Album:
	- Camera Mode with Snippet function
	- Album menu with photo grid
	- Store album per aphid Neko-Atsume style
- Kitchen Recipe Display:
	- Display grid of recipe slots
	- Maybe a search query?
- Aphid Play
	- Play State triggers custom play behaviour when nearby play objects
	- Play objects consist of custom classes dictating what aphid and the object should do
	- Adapt items to automatically find their play behaviour and allow repeats
- Aphid Training
	- Food grants skills points
	- Train State triggers custom behaviour when nearby training objects
	- Train objects consist of custom clases dictating what aphid and the structure should do
	- Train Structures require to find the skill being trained and by how much
- Aphid Better Breeding
	- Adjust color lerp to have better looking inherited colors
	- Inherit skills
	- New Aphid skin

- exiting storage mode acts weird, hovering over an ui makes exit storage mode properly but hover over nothing and it instantly exits camera mode
- icon for aphid spectating
- spectating should show a label that indicates that you can switch and who you are seeing
- adjust slider padding to fit end of value better
- rendering is not adjusted for high zoom out
- draw bench
- draw rock

## Bug Tracker:
- Furniture clips below cliff floor, not a big deal but it annoys me big time
## Technical
- Refactor OptionsManager to work with easily adjustable config slot modules
- add event handlers for save modules
- controls menu layout update to sort out build mode interactions + add "pull" as a rebindable
- implement cutscene manager, which connects itself to dialog manager to manage custom animation events ([Action]animationFunction , [bool]essential)
- separate cosmetic loading interface from main resoure load from INTIALIZE_GAME_PROCESS, so MainMenu simply watches its state without being necessary of GlobalManager to showcase
## Gameplay
- freaking aphid throwing, make em spin and bounce like a rubber ball for the funnies
- allow to sell items of the inventory
- allow pickyeaters to eat neutral flavor foods
- View aphid status even if not picked up
- High bondship reveals hidden aphid personality
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

## Gameplay
- Added the photo tab, allows to zoom in and out, and to select an aphid to follow, focused aphids will have the screenshot in their own unique folder.
## Technical
- Updated to Godot 4.4. With this, comes the new meta reference system, thus, all resources have been given a ".uid"
- Added the dialog console command to display translation texts
- Updated several string references to use StringNames when proper
- Updated several instances of sound managing to use the new system and keep in memory frequent ones.
- Updated the way Screen Size is calculated to allow for modifications to camera and viewport and still retain mouse tracking
- Renamed ResortGui to FreeCameraManager, as its purpose wasnt as resort general but more free camera mode focused.
- Aphid skins have been packed into a spriteatlas. a heavy boost in performance should be noticeable, however, this affects how new aphid skins can be implemented, refer to [[Aphid Skin Implementation]] for more information
## Graphics & Sound
- Aphids ready for harvest are now unaffected by lighting while highlighted

## Fixed
- Mushroom gummies had the wrong display sprite
- (Spanish) User interface label did not position itself correctly
- Leaving the build or furniture menu would turn back back the normal HUD while in free camera mode
- Aphids left their harvest particles when they died