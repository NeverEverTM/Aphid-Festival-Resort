
# Current Work (0.2v)

- Draw bench and bee flag structure sprite
## Bug Tracker:
- Music randomly stops when reloading a game, comes back after you pause and unpause
- Furniture clips below cliff floor, not a big deal but it annoys me big time
- Food items sometimes arent consumed and are just dropped on the floor, reloading the save will make them re-pickuable
## Technical
- Refactor OptionsManager to work with easily adjustable config slot modules
- add event handlers for save modules
- controls menu layout update to sort out build mode interactions + add "pull" as a rebindable
- add skin cache resource preloader
## Gameplay
- freaking aphid throwing, make em spin and bounce like a rubber ball for the funnies
- Wider interactions for aphids, such as with structures, items, other aphids or the player itself
- make food items have multiple flavors, and add skill bonuses
- allow to sell items of the inventory
## Music
- Project Color jingle
- Aphid Death jingle
# [Full Version Changelog]

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

## Bugfixes
- Resolved issue with continue button not working properly on aplication reboot
- Load Game menu no longer clones and stacks savefiles slots everytime it closes and reopens
- Areas with lighting should be much more performant
- Fixed harvest particles showing somewhere else in the map instead of being on the aphid
- You can no longer select items that were behind the build menu by accident
- Resolved some weird edge case issue with particles not being properly disposed of.
- Furniture can no longer be placed inside walls or water
- Fixed food preferences not properly being applied to due to an error