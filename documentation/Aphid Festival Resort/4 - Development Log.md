
- Draw water torch, bench and bee flag structure sprite
- update aphid info panel
- update kitchen panel

optional
- Add berry bush animations and effects
- add event handlers for save modules
- update a few item and structure descriptions
- add shop in and out sounds
- add to dialog manager the (they) (them) (o/a) dialog commands for pronouns
- add head bobbing followup for picked up items
# Current Work (0.2v)

- To implement Aphid Traits and Skills to piece out their personality
- Refactor aphid behaviour into clearly defined Aphid States
- Add debug comand to select and alter aphids remotely
## Bug Tracker:
- Music randomly stops when reloading a game, comes back after you pause and unpause
- Furniture clips below cliff floor, not a big deal but it annoys me big time

## Technical
- Refactor OptionsManager to work with easily adjustable config slot modules
- Turn the player pickup into a proper data class
- Implement unit testing for structures, recipes, etc
## Gameplay
- freaking aphid throwing, make em spin and bounce like a rubber ball for the funnies
- Setting: Camera smoothing
- Wider interactions for aphids, such as with structures, items, other aphids or the player itself
- Resolution Settings
## Music
- Project Color jingle
- Aphid Death jingle
- Possible Shop themes?
# [Full Version Changelog]

## Gameplay
- Added Aphid Traits, five traits are randomly selected from the global/parents skills pool to add onto an Aphid. List of the added ones are:
	- HyperActive
	- Lazy
	- PickyEater
	- Affectionate
	- HeavySleeper
	- Fertile
- Added the following furnitures:
  - Sunshade
  - Berry Bush: Grows berries which can be fed every so often
- Added control prompts gui for various actions that appear at relevant times (aka near an interactable)
- Added new volume settings for Master, Ambience and User Interface
- Added AutoRun setting
- Added a new npc "Echo", guard of the gate, gate which is still in construction
## Technical
- SaveSystem.ISaveData has been discarded in favour of SaveModule[T], refer to the [[SaveData Implementation]] documentation.
- Several Aphid behaviour related scripts have been modified and added
  - IState defines an aphid's state behaviour and compatibilities
  - ITrait defines unique trait behaviour
  - Aphid.Skill (previously known as ability) comes back as a properly defined stat
  - ITriggerEvent and IDecayEvent for aphid interactables and decay timers accordingly
- Added NPCPhysicsBody which is a CharacterBody-inherited manager for NPCs that move around. Crystal has been given one aswell.
- Several touchups in Dialog Manager, most importantly, changed how dialog is displayed.

## Graphics & Sound

- Added player Step Sounds for walk and run
- Added a new player Whistling animation
- Updated several icons in settings as well as its layout
- A whole new aphid skin available in some eggs.
- All menus in the game have been updated, some have a more clear and prettier layout but others also count with extra functionality (for example, the load menu sorts by and displays last time played, furniture menu actually shows its items real sprite)

## Bugfixes
- Resolved issue with continue button not working properly on aplication reboot
- Load Game menu no longer clones and stacks savefiles slots everytime it closes and reopens
- Lighting should be much more performant
- Fixed harvest particles showing somewhere else in the map instead of being on the aphid
- You can no longer select items that were behind the build menu by accident
- Resolved some weird edge case issue with particles not being properly disposed of.
- Furniture can no longer be placed inside walls or water