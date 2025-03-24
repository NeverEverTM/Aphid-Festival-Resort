# v0.1.1
- Added various new food items
- Fixed an issue where new games wouldnt properly clean the scene, so it would carry last save data into it
- Aphid state "Drink" has been removed as is no longer needed
- Foods now manage both drink and food values, so they can apply both if desired
- Items are now dynamically created for conveniences sake, they can still be spawned using the /items folder if it needs to hold custom data, scripts or more nodes than needed
- Tweaked food values and costs
- Shop UI improved + converted into a more modular system as foundation for other shops
# v0.1.2
## Changes
- Implemented Cooking as a mechanic, players can now use two to one ingredients to create recipes.
- New Day-Night cycle, with a Morning, Noon, Sunset and Night that display and change hourly
- You can now pinpoint an Aphid location by double-clicking on their frame in the generations menu.
- Changed several pricing and food values for balancing
- Removed recipes from shop, as they are now redundant
- Implemented a backwards compability system for future updates
## Fixed
- Aphids who died will persist even after reloading, as far as I know it doesnt affect nothing apart from having "ghost" Aphids who shortly vanished after reloading a save.
  This new update should delete all instances and properly see them as dead.
- Due to the previous issue, the save system had to be touched upon along with some improvements, moving between saves is dangerous but I did told you in the load screen.
- Issue with Z-depth has made me change how things are layered, it should be remain the same however
- Items spawned could act very funky as some properties werent intially set to be inserted when created, items should act fine now.
# v0.1.3
### Fixed
- Hopefully fixed resolutions problems once and for all
- Logger now works properly (oops)
- Light no longer interacts with most UI elements
- Item shop text not showing up correctly
- You can no longer multi-interact with various entities at the same time
- The sea no longer GLOWS during the night (shader shenanigans)
- You can no longer use non-food items to create recipes (you could use your two first eggs and softlock yourself)
- You can no longer interact with things while busy (inside a menu, etc)
- Aphid sounds did not respect Sound settings
### Gameplay
- Resort customization! Buy furniture and place it, all within the free camera mode
-  Added a free camera mode which can be toggled with Q
- Now you can customize input binding freely
- Resort map has been again redesigned to be smaller and more structured
- You can now take screenshots, any photos taken are stored inside the current savefile in a folder called "screenshots"
- A few UI elements have been added for ease of interaction
- Redesigned the load game screen to be prettier and more functional, you can now see aphids in it, and the time spent in total
- Petting is faster, hooray
- More dialogue tidbits for NPCs, and just general spice up
- Added more and more fluid animations for characters
- Updated visuals on certain props to match better the current aesthetic
- Title theme, not final but just enough to break the silence
- Day and Night cycle transitions are less abrupt
### Technical
- Created plugin specific for color changing buttons
- Moved skins and items folders to databases folder
- MenuManager is now its own instance component use for menu managment
- Changes to internal tag systems for shops
- Separated canvas into Resort Canvas and Regular Canvas, one is for exclusivally resort envionments, while the other will be used for general gameplay such as another maps
- Interaction sytem has been changed to hammer some bugs and issues with player interaction
- Tilemap has been optimized and organized
- Dialog Manager has been refactor to account for many proper features
- Added startup logo and loading screen (for mere ego and vanity)
- Debug console now available (with secret code and all), not much commands for now
# v0.2.0
## Fixed
- Resolved issue with continue button not working properly on aplication reboot
- Load Game menu no longer clones and stacks savefiles slots everytime it closes and reopens
- Areas with lighting should be much more performant
- Fixed harvest particles showing somewhere else in the map instead of being on the aphid
- You can no longer select items that were behind the build menu by accident
- Resolved some weird edge case issue with particles not being properly disposed of.
- Furniture can no longer be placed inside walls or water
- Fixed food preferences not properly being applied to due to a rounding error
## Gameplay
- Added Aphid Traits, five traits are randomly selected from the global/parents skills pool to add onto an Aphid. List of the added ones are:
	- HyperActive
	- Lazy
	- PickyEater
	- Affectionate
	- HeavySleeper
	- Fertile
	- Loyal
	- Glutton
- Added the following furnitures:
  - Sunshade
  - Berry Bush: Grows berries which can be fed every so often
  - Bee Banner
- Added more food and item recipes.
- Added control prompts gui for various actions that appear at relevant times (aka near an interactable)
- Added new volume settings for Master, Ambience and User Interface
- Added AutoRun setting
- Added a new npc "Echo", guard of the gate, gate which is still in construction
## Technical
- SaveSystem.ISaveData has been discarded in favour of SaveModule[T], refer to the [[SaveData Implementation]] documentation.
- The apex of this update. Several Aphid behaviour related scripts have been modified and added
  - IState defines an aphid's state behaviour and compatibilities
  - ITrait defines unique trait behaviour
  - Aphid.Skill (previously known as ability) comes back as a properly defined stat
  - ITriggerEvent and IDecayEvent for aphid interactables and decay timers accordingly
- Added NPCPhysicsBody which is a CharacterBody-inherited manager for NPCs that move around. Crystal has been given one aswell.
- Several touchups in Dialog Manager, along with new commands
- Organized the way some assets are loaded and cached
- SoundManager refactoring for better sound re-usage
- Moved savefile responsabilities to a brand new GameManager, and renamed the formal old GameManager to GlobalManager

## Graphics & Sound

- Added player sounds for walking and running
- Added a new player whistling animation
- Updated several UI elements
- A whole new aphid skin available in some eggs.
- All menus in the game have been updated, some have more clear and prettier layouts but others also count with extra functionality, for example; 
  - the load menu sorts by and displays last time played
  - furniture menu actually shows its items real sprite
  - the kitchen now needs to learn a recipe before it shows a result, but otherwise has a more ux friendly interface
- Added a leaves particles overlay
- Added ambience sounds for the resort

# v0.2.0.1
## Fixed
- Mushroom gummies had the wrong display sprite
- (Spanish) User interface label did not position itself correctly
- Leaving the build or furniture menu would turn back back the normal HUD while in free camera mode.
- Aphids left their harvest particles when they died.