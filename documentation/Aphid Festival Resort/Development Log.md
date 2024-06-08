
To see roadmap go here: [[Major Version Goals]]

+ Maybe Aphids skin parts change color slightly depending on what you feed them?
  Could be skill related too
+ Rework in aphid states needs to account for traits, thinking of a "trait apply" structure, where each trait loads themselves in states, and then states check if they have any to apply
+ 
## Version Implementations:
- 1.4 Wider interactions for aphids, such as with structures, items, other aphids or the player itself
- 1.4 The base support for trait system + skills
## Current Work (0.1.3v)
- Resort Customization
  + Buy things from furniture shop, put them in storage
  + Allow placement of storaged things and buying more on the spot
  + Move/Store/Sell selected pieces
- Input Binding
- Fix outline shader to render as a single sprite
- Organize sprite folders and allow load of subfolders in database startups (SFX already does this)
## Bug Tracker:
- (Unknown) Pinpoint markers sometimes dont pinpoint!
### Pending


# Changelog
### Fixed
- Logger now works properly (oops)

### Done
- Implement cheat mode, and its utilities (fun key combination to activate it included)
- Create popups for warnings, such as invalid resort names
- Debugger Printer for logs

## Changed

### Gameplay
- Added a free camera mode which can be toggled with T
- Added a way to take photos, from within free camera mode, any photos taken are stored inside the current savefile in a folder called "screenshots"

### Technical
- Created plugin specific for color changing buttons
- Moved skins and items folders to databases folder
- Added a way for menus to do a close action
