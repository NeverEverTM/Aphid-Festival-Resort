

# Current Work (0.2v)

- To implement Aphid Traits and Skills to piece out their personality
- Refactor aphid behaviour into clearly defined Aphid States
- Add debug comand to select and alter aphids remotely
## Bug Tracker:
- Music randomly stops when reloading a game, comes back after you pause and unpause
- Furniture clips below cliff floor, not a big deal but it annoys me big time

## Technical
- Fix outline shader to render as a single sprite
- Organize sprite folders and allow load of subfolders in database startups (SFX already does this)
- Refactor OptionsManager to work with easily adjustable config slot modules
- Fully implement the new save system to all other scripts
- Turn the player pickup into a proper data class
- Implement unit testing for structures, recipes, etc
- Store Aphid Skins to memory to see if performance ups
- Refactor lights into using simple overlapping sprites
## Gameplay
- freaking aphid throwing, make em spin and bounce like a rubber ball for the funnies
- Setting: Camera smoothing
- Setting: Auto-run
- Wider interactions for aphids, such as with structures, items, other aphids or the player itself
- Resolution Settings
## Music
- Project Color jingle
- Aphid Death jingle
- Possible Shop themes?
## Misc
- different color for harvest outline + particles
- New paint of coat for some menus, including new art
- Portraits for shop owners
- Dialog should use that thingy where it shows a character at a time rather than put one itself at a time
# [Full Version Changelog]

## Gameplay
- Added Aphid Traits, five traits are randomly selected from the global/parents skills pool to add onto an Aphid. List of the added ones are:
	- HyperActive
	- Lazy
	- PickyEater
	- Affectionate
	- HeavySleeper
	- Fertile
## Technical
- Aphid IState defines an aphid's state behaviour and compatibilities
- Aphid ITrait defines unique trait behaviour
- Aphid Skill comes back as a proper defined stat
- Aphid ITriggerEvent and IDecayEvent
## Graphics & Sound
### Added
- Player Step Sounds
- Player Whistling animation

## Bugfixes
- Resolved issue with continue button not working properly on aplication reboot
- Load Game menu no longer clones and stacks savefiles slots everytime it closes and reopens
- Heavily lighted areas should be much more performant