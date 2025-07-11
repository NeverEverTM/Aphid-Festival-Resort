# v0.2.2

## Work In Progress

- Aphid Play & Train[WIP]
	- Aphids can be put in structures for either mere entertainment or for training.
	- Pending proper implementation of a training structure.
- Room System [WIP]
	- Lobby Finished. Functionality pending.
	- Golden Hallway Finished.
- Lobby [WIP]
	- Decoration is finished
	- No functionality added yet.
	- NPCs meant for this area are Joy (randomly enters), Frontdesk NPC, Job board NPCs, maybe some random appearances from the og game, like the owner himself. 
- Trade Functionality
	- Players would be able to generate a QR code with the genes (and status if it fits) of an aphid, which they can share with others. 
- Selling Functionality [WIP]
	- Aphids sell at a base of 50 berries (25 if they are a baby), for each level in a skill, their cost goes up by 2 berries, for a maximum of 850 berries (50 base plus 100 x 2 per each one of the four skills)
- Job Menu [WIP]
	- A set of randomly generated requests are made, this list resets per hour passed (playtime)
	- List is a finite amount of requests, of which only a portion are shown, and everytime one is completed, another is taken from the current pool to show until it rans out.
	- Each request requires a certain set of skills, with a minimum level at which they must be, an aphid can still be sent if it doesnt meet the requirements, however, they will have a chance of failing the request that increases the further the gap between the required skill floor and the current skill is.
## Bug Tracker:
- Room transition deadlocks the game, MASSIVE problem, idk why or what
## Technical
- Refactor OptionsManager to work with easily adjustable config slot modules
- add event handlers for save modules
## Gameplay
- freaking aphid throwing, make em spin and bounce like a rubber ball for the funnies
- add custom video support for tv (later lol)
- aphid hats sold by tangy cause funny
- tangy dummy target, cause extra funny
- Kitchen Recipe Display:
	- Display grid of recipe slots
	- Maybe a search query?
+ aphid trading will be either daily procedurally generated aphids by the game or with other players via qr codes (or both)
+ aphid skill affinities which make them "lock in" into a single skill, plus a change of color accompanying this.
+ aphid moodlets that signalize how they feel about certain things or what thye need
## Furniture and Food Ideas
- require more recipes for bitter, and recipes for water in general except for Sweet
- Cake Mix: Replace cake recipe with this
- Choco Muffin: Cake Mix + Hustle Berry(?)
- Bench
- resort sign
- Text sign
- Ball Pit
- Water Slide
- Trampoline
- Small Push Car
- Hot Air Balloon
## Music
- Project Color jingle
- Aphid Death jingle
- night 2
- lobby theme
# [Full Version Changelog]

## Gameplay
- Added the lobby, where you can sell aphids, ~~upgrade resort services~~ and see online news about the games development.
	- Added a stats records tab to check in miscellaneous data.
	- Added a news board that delivers the latest development update.
	- Added the **job board** where you can send aphids on requests and gain money and skill from it. 
	- Upgrades are currently not implemented fully, this will change with the release of 0.3.
- Resort layout has been once again updated. This time separating the west and east into separate islands. Brought a few of the POI's close together, and added the big lobby building up north.
- Added NPC Tanjy as a future hat seller.
- Added the following structures to the game:
	- Paper Lantern for water lighting
	- Combat Dummy for strength training
	- Runmill for speed training
	- Kindergarten for intelligence training
	- Jump Rope for stamina training
	- Hot Bathtub that quickly recovers aphid stamina
	- Bottlecap Cot
	- Grass Pot
	- Mothiva Poster which is placeable in the walls
	- Patterned Rug
	- *UPDATED* Sunshade to better conform with the other wooden items
	- *UPDATED* Sapling Stump with new sitting functionality

## Graphics, QoL & Sound
- New bootup intro.
- Given the title menu some love.
- Aphid info tab has their skills information better spaced and indented.
- The graphic shaders for backgrounds have been upgraded, allowing them to be more varied all through the game.
- Adjusted some focus elements, the game is still pretty much not gamepad compatible but keyboard experience should be good as ever.
- Remastered various sounds, particularly ones used for UI.
- Item slot buttons have been standarized all over the game, and thus are nicer to interact with.
- Updated some dialogue strings to reflect the new changes, also added a few new ones.
- Updated localization strings.

## Development
- Now all items posses metadata that can be used to initialize states and keep data from being cleared.
- MenuUtil has been deprecated and deleted in favour of MenuHandler and MenuInstance, which have now extra functionality to await for end of menu animation and a verification method for closing the window.
- Added FurnitureInteractable to manage interactions between player and structures.
- Added SkillInteractable which is used for training structures.
- The old scene managment done by GlobalManager has been refactored into a new class called SceneManager, in charge of loading a game's state and room.
- Aphid Data has been turned into a proper savemodule, available at GameManager and loaded second highest.
- Moved the generations saved data module to GameManager as well.
- The way savedata is loaded and saved has been adjusted on all instances. This is to accomodate saving and loading data properly when switching rooms
- LoadScreen now exists as a base for load screens specific nodes, from which both the leaves and the fade to black, are part of.
- Build menu now keeps track of the material the item had before applying the outline shader so it can properly set it after is done with it.
- Moved prefabs and script folders of place.
- Added room-specific behaviour to FieldManager.
- Several refactoring for pickup and drop methods, added a non-awaitable option.
- PlayerInventory has been streamlined.
- Added the teleport comand to console.
- AphidActions Train State has been removed in favour of using Play State instead to keep the same behviour under the same state.
- Tweaked GlowButton behaviour to account for disabled buttons.
- Deleted unneeded assets.
- Added a standarized option to create buttons that display aphid skins available in CanvasManager.

## Fixed
- Aphid eggs no longer lose their genes when reloading the game.
- Father and Mother now references the currently alive aphid and properly updates accordingly.
- Inheriting skills now actually works for realsies.
- Aphids recently born no longer get 5 or more traits from parents.
- Camera bounds would not get updated if you were to zoom out and quit camera mode while being leaving it like that.
- Solved some issues with the backwards compability changes done last update. The player now properly checks for instances of being blocked and resets its position if is so.
- Generations Panel could softlock itself by either showing photos or passing page while in a busy state.
- Sounds would not get disposed of properly, and could inherit some properties of the last active sound. 