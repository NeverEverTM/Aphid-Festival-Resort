# v0.2.2

## Work In Progress
- Lobby [WIP]
	- NPCs meant for this area are Joy (randomly enters), Frontdesk NPC, Job board NPCs, maybe some random appearances from the og game, like the owner himself. 

## Bug And Polish Tracker:
- Terrible variety in foods, some dont even make sense, rethink (<- need a debug gui for this)
- add a particle popup warning for interactables who need an aphid
- implement passive time to bushes too
- finish runmill animation, kindergarten sprite and jump rope sprite
- finish all localization files for requests
- finish tutorial localization strings
- you can zoom in and out in furniture store
- update tutorial strings
- furniture interactable does not work on this version due to refactoring, it should also account for out of screen time
- >Hungry state is acting up, needs some fixing
- Autosave should be detected on bootup of savefile

## Technical
- Refactor OptionsManager to work with easily adjustable config slot modules
- create graph for food balance
- add recipe creator gui
- modify atlas gui to make resoureces "local to scene"
- refactor aphid animations and sprites to include individual legs + hat
## Gameplay
- freaking aphid throwing, make em spin and bounce like a rubber ball for the funnies
- add custom video support for tv (later lol)
- aphid hats sold by tangy cause funny
- Kitchen Recipe Display:
	- Display grid of recipe slots
	- Maybe a search query?
+ aphid skill affinities which make them "lock in" into a single skill, plus a change of color accompanying this.
+ aphid moodlets that signalize how they feel about certain things or what thye need
+ Trade Functionality
	- Players would be able to generate a QR code with the genes (and status if it fits) of an aphid, which they can share with others. 
	-  aphid trading will be either daily procedurally generated aphids by the game or with other players via qr codes (or both)
## Furniture and Food Ideas
- require more recipes for bitter, and recipes for water in general except for Sweet
- Cake Mix: Replace cake recipe with this
- Choco Muffin: Cake Mix + Hustle Berry(?)
- Bench
- Text sign
- Ball Pit
- Water Slide
- Trampoline
- Small Push Car
- Hot Air Balloon
- Jukebox
## Music
- Project Color jingle
- Aphid Death jingle
- night 2
- lobby theme
# [Full Version Changelog]

## Gameplay
- Added the lobby, where you can sell aphids, upgrade resort services and see the game's changelog.
	- Aphids can be sold at a base price of 50 berries (25 if they are a baby), for each level in a skill, their cost goes up by 2 berries, for a maximum of 850 berries (50 base plus 100 x 2 per each one of the four skills).
	- Added a stats records tab to check in miscellaneous data about the savefile.
	- Added a news board that delivers the latest development updates. Here is where these changelogs should be seen from now on! (I will probably copy this over too)
	- Added the **job board** where you can send aphids on requests and gain money and skill from it. All of them requires certain skills in order to be finished, otherwise you lose on the money, aphids also need to be rested and well taken care off before embarking on requests.
	- Upgrades are currently not implemented fully, this will change with the release of 0.3.
- Resort layout has been once again updated. This time separating the west and east into separate islands. Brought a few of the POI's close together, and added the big lobby building up north.
- Added a few new NPC's for the lobby, including Tanjy as a future hat seller, FrontDesk at the front desk, and a few others... (<- gotta update this once release comes)
- In terms of structures:
	- ADDED Paper Lantern for water lighting
	- ADDED Combat Dummy for strength training
	- ADDED Runmill for speed training
	- ADDED Kindergarten for intelligence training
	- ADDED Jump Rope for stamina training
	- ADDED Hot Bathtub that quickly recovers aphid stamina
	- ADDED Bottlecap Cot
	- ADDED Grass Pot
	- ADDED Mothiva Poster which is placeable in the walls
	- ADDED Patterned Rug
	- *UPDATED* Sunshade to better conform with the other wooden items
	- *UPDATED* Sapling Stump with new sitting functionality
	- *UPDATED* HiTech Display, with a power and change channel button, and a few extra channels
## Graphics, QoL & Sound
- New bootup intro.
- New time of day, we have now Morning, AfterNoon, SUNSET, and Night
- Given all main menu panels some love and cheerish, including silly interactions hidden in the logo...
- Added a manual save button to the pause menu, exiting to desktop or main menu no longer saves, and instead prompts you with a warning about it aswell.
- Added an autosave! It triggers every few minutes or when switching rooms.
- The graphic shaders for backgrounds have been upgraded, allowing them to be more varied all through the game.
- Adjusted some focus elements, the game is still pretty much not gamepad compatible but keyboard experience should be better than before.
- Remastered various sounds, particularly ones used for UI.
- Item slot buttons have been standarized all over the game, and thus are nicer to interact with.
- Updated some dialogue strings to reflect the new changes, also added a few new ones.
- Updated localization strings.
- Added a few more sound cues for menus.
- Updated the kitchen menu, which now includes a WIP section reserved for a future addition.
- The aphid information panel should be much more smoother to interact when handling aphids or seeing them from the free camera. Looks and feel was also updated
- Updated player look with what should be their final look, aswell as giving them a new idle.
## Fixed
- Aphid eggs no longer lose their genes when reloading the game.
- An aphid's parents are now referenced directly by ID instead of just saving the name, this is so it reflects the parent's current status properly (before, you could change a parent's name and it would not be reflected in the bio of their children).
- Inheriting skills was *not* working before, it should do now.
- Aphids recently born no longer get 5 or more traits from parents (The biography panel only shows 4 at a time but you can notice by behaviour).
- Camera bounds would not get reset back properly if you were to zoom out and return to normal, making you unable to look near the borders of the map.
- Solved some issues with the backwards compability changes done last update. The player now properly checks for instances of being blocked and resets its position if is so.
- Generations Panel could softlock itself by either showing photos or passing page while in a busy state.
- Sounds would not get disposed off properly, and could inherit some properties of the last active sound.
- Generations panel now opens again by pressing its menu button ("G" by default)
## Development
- Now all items posses metadata that can be used to initialize states and keep data from being cleared.
- Added the InteractableArea2D and IInteractableArea2D as the standard for interactable items.
- MenuUtil has been deprecated and deleted in favour of MenuHandler and MenuInstance, which have now extra functionality to await for end of menu animation and a verification method for closing the window. This shold several problems with menus softlocking or remaining innaccesible till restart.
- Added a few scrips to manage interactions between player and structures.
- The old scene managment done by GlobalManager has been refactored into a new class called SceneManager, in charge of loading a game's state and room.
- Aphid Data has been turned into a proper savemodule, available at GameManager and loaded second highest.
- Moved the generations saved data module to GameManager as well.
- The way savedata is loaded and saved has been adjusted on all instances. This is to accomodate saving and loading data properly when switching rooms
- LoadScreen now exists as a base for load screens specific nodes, from which both the leaves and the fade to black, are part of.
- Build menu now keeps track of the material the item had before applying the outline shader so it can properly set it after is done with it.
- Moved around editor files, in particular prefabs and scenes.
- Added room-specific behaviour to FieldManager.
- Several refactoring for pickup and drop methods, added a non-awaitable option.
- PlayerInventory has been streamlined.
- Added the teleport comand to console.
- Tweaked GlowButton behaviour to account for disabled buttons.
- Deleted unneeded assets.
- Added a standarized option to create buttons that display aphid skins available in CanvasManager.
- Refactored AphidActions, including states, decays and much more, added new functionality for future content, aswell as improving performance and readibility.
- All tags have been replaced with an INT instead of a string, which is managed by the GlobalTags enum.
- Aphids in the main menu have now their own AphidFake behaviour instead of borrowing the normal Aphid one.
- Aphids now work in the background when not loaded in, as AphidPassive
- Renamed Logger to DebugLogger since 4.5 seemingly has its own internal Logger class.
- Separated Player.Data into its own class as PlayerData.
- Aphids now have a list of Flags that can influence behaviour in the states without directly having to manually find these.
- Events for a few entities have been changed to a new Event Listener format that is still in testing but hopefully makes working with them a lot easier.
- New GlobalManager.Util function to generically invoke these events has been added as well
- Created CustomTimer<T> and CustomBaseTimer for internal Timer purposes, various Timer related actions have been replaced with this.