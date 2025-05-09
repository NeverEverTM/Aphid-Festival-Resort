# v0.2.1

## Work In Progress

- Aphid Play [WIP]
	- Aphids can interact with nearby objects and aphids.
	- Right now the only one properly implemented are the relationships but they arent as mechanically complex
	- Items should be more or less easy to implement custom beahviour for, though i need to implement data serialization aswell
- Aphid Training [WIP]
	- Aphids can interact with nearby structures
	- No structures currently have any interactability functionality

## Bug Tracker:
- [IMPORTANT] eggs that are either, stored and put back, or are spawned after reload, LOSE their genes, with bought eggs it doesnt matter but it matters with any egg produced from breeding
	- This issue has been partially softened by disallowing storing an egg
	- A better solution may be either implement item data serialization or spawn eggs as structures
- father and mother references should be refactored to use the relationship system instead, so one can refer to the live value if it changes, as of now, it keeps displays a possibly old name they had when the aphid was breed
- traits get repeated when inheriting [probably fixed]
## Technical
- Refactor OptionsManager to work with easily adjustable config slot modules
- add event handlers for save modules
- implement cutscene manager, which connects itself to dialog manager to manage custom animation events ([Action]animationFunction , [bool]essential)
- separate cosmetic loading interface from main resoure load from INTIALIZE_GAME_PROCESS, so MainMenu simply watches its state without being necessary of GlobalManager to showcase
- refactor aphid save/load into a savemodule
## Gameplay
- freaking aphid throwing, make em spin and bounce like a rubber ball for the funnies
- add custom video support for tv (later lol)
- job system for aphids to earn money based on skills
- aphid hats sold by tangy cause funny
- tangy dummy target, cause extra funny
- Kitchen Recipe Display:
	- Display grid of recipe slots
	- Maybe a search query?
+ aphid trading will be either daily procedurally generated aphids by the game or with other players via qr codes (or both)
+ aphid skill affinities which make them "lock in" into a single skill, plus a change of color accompanying this.
## Furniture and Food Ideas
- require more recipes for bitter, and recipes for water in general except for Sweet
- Cake Mix: Replace cake recipe with this
- Choco Muffin: Cake Mix + Hustle Berry(?)
- Sweet Tea: Plain Tea + Honey
- bottle cap aphid house (interactable)
- aphid bathtub...
- Bench
- Text sign
## Music
- Project Color jingle
- Aphid Death jingle
# [Full Version Changelog]

# v0.2.1
## Gameplay
- Added the photo camera mode, allows to zoom in and out, focus on any aphid in the resort and to ping their current location.
- Added the generations album, which shows screenshots taken in the resort, or for a specific aphid.
- New cutscene for a new game and a proper "gameover"
- [Aphid inheritance of colors has been "fixed"], and it should be much more interesting and less dull than the last version, still a bit of adjustment is needed.
- Aphids now gain(or lose!) skills from food items, mix and match to see which food works best for your own.
- Aphid stats are now shown by pressing the "Pull" button, it also finally shows their current skill level and points for the next.
- New Aphid Skin available in blue eggs.
- You can now sell items in your inventory by activating sell mode, just like structures, items are sold for half their price.
- Aphid traits and preferences are now revelead in the generations album depending on how much bondship you have with them (thats what the big pink circle meter is for!)
- Added "Pull Item" and "Align to Grid" as rebindable controls.
- Added 6 new furniture. Including custom image signs.
- Added 5 new recipes and one new ingredient.

## Technical
- Aphid skins have been packed into a spriteatlas and a heavy boost in performance should be noticeable, however, this affects how new aphid skins can be implemented, refer to [[Atlas Packager Usage]] for more information.
- Tilemaps have been properly adjusted to not block shadow lights.
- NPC's no longer repeat the same line of dialog when set to Random, and will in fact not say it again until the entire dialogue tree is exahusted.
- Build mode binds should no longer interfere with overworld binds, allowing to bind the same key to other actions in different bind groups.
- Many control prompts, buttons and shortcuts have been added for accessibility.
- Resized the resort map, furniture and aphids outside the playable area should be either stored or repositioned on bootup with no further complications.

## Graphics & Sound
- Some furniture sprite improvements, including functionality wise. 
- Adjusted some UI theme elements for better readibility.
- Grass now sways when walked over and when left by itself.
- Updated UI SFX for buying stuff, plus added a bunch more.
- Added a buy button for stores.
- Hello everyone my name is Markiplier and welcome to Five Nights at Freddys
- Retouched some menu interfaces.
- Added a backdrop to most text in the game which i think looks nice.
- Remastered some sounds to be more quiet/louder.
- Updated help image examples.
- Added confirmation popups when exiting the game or loading a game from a previous version.

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
- New Atlas packager utility, GUI included, please revise [[Atlas Packager Usage]] if you want to implement a skin or item. Various development assets have been included in the documentation folder for this purpose.
- MenuUtil has been adjusted to include better control over menu behaviour.
- Added an event handler for when the game binds a new action control.
- Moved some scenes to new folders plus added a new [Resources] folder for misc godot-specific resources.
- Created a CameraManager, which takes all the camera code from GlobalManager and FreeCameraManager and puts it in one place.
- Build menu has been adapted to include some extra data about building's behaviour, this allows them to have special properties like "being water placeable" or "non collideable with other furniture"
- Added the PostLoad and PreLoad functions to SaveModule for when data is loaded and for when the path is created, respectively. This is to allow for some of the earlier patches to live more elegantly within their respective modules.
- Backwards compability has been updated to handle down to 1.2 savefiles (more or less)
- Food items have been packaged into an atlas using the same tool ([[Atlas Packager Usage]]), for consistency and to fix some issues with bleeding sprites.
## Fixed
- [Not Fixed] Aphid eggs can lose their inherited genes if you leave the game *before* the egg hatches, this could happen when grabbing and storing them but this has been now prohibited, a proper fix for this should be up next update.
- [Not Fixed] The name of the parents of an aphid does not update if you change them later.
- [Balance] Rebalanced several gamerule and database values, including furniture and food prices, mating cooldowns and aphid need drain.
- [Balance] Fertile has been tweaked for a slight advantage rather than dividing by two the already low offspring cooldown.
- [Balance] Rebalanced and tweaked several values related to foods which are yet to be properly revised in a later update.
- You can now close various menus elements without exiting the menu itself. (Ex. build menu closes the storage tab if attempting to exit)
- [Balance] Pickyeaters can now eat neutral-flavoured foods aswell.
- Particle manager would make other classes throw errors if they attempted to spawn particles over the particle limit. This should no longer happen.
- Aphids that were breeding would sometimes "T-Pose" towards their breed target, this should be fixed.
- Bee flag structure had the wrong id, which turned it into a rug after game reload, the way ids are set has been changed to prevent such things from happening in the future.
- Build menu did not use the standard method for spawning structures and caused issues when loading new structures.
- Ambience leaves no longer darken when the player goes under a sunshade.
- Furniture hitboxes were off by a few pixels of their actual visual sprite
- One or more repeated traits could exist when being inherited from breeding, the game now checks if the trait is not already in the aphid gene and chooses another if so
- The main menu resets itself properly when there is no savefiles available to load or continue
