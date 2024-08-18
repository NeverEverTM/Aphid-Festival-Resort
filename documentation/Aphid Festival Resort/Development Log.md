
To see roadmap go here: [[Major Version Goals]]

## Version Implementations:
- 1.4 Wider interactions for aphids, such as with structures, items, other aphids or the player itself
- 1.4 The base support for trait system + skills
## Current Work (0.1.3v)
- Resort Customization
  + Buy things from furniture shop, put them in storage
  + Allow placement of storaged things and buying more on the spot
  + Move/Store/Sell selected pieces
- Input Binding
## Bug Tracker:

- Music can stop and not come back when leaving to main menu and going back to resort, comes back after you pause and unpause
  Music in general just seems to be funky in the resort
# [Pending Issues]

- game intro Project Color lacks polish, it requires a bright white flash transition into full logo and motted bg, with small jingle

- Update UI elements to use higher resolution art
- Pause menu button does not play a sound
- change bg color and font color of pause buttons to a different color
- options should have focus highlight color for bg and font
- options combomenu icons are small

- increase player sprite framerate
- add pickup anim
- add step sounds

## ISSUES
- furniture selection boxes are lost after leaving the menu and do *not* regenerate
- way to quickly close storage without closing menu
- mouse hover over borders should move camera (there is a property in Camera2D that allows this)
- torch is not recognized as a structure BECAUSE it tries to get a Sprite2D, when this object is an AnimatedSprite2D, for this we need to detect for manually tagged data to create boundaries by ourselves instead
- furniture icons missing
- furniture menu could be a book? storage some sort of checklist

## Technical
- Fix outline shader to render as a single sprite
- Organize sprite folders and allow load of subfolders in database startups (SFX already does this)
- Escaping camera mode should not trigger pause menu
- Camera already has limits for screen boundaries (should probably replace the in-house solution and change to this?)
- Torch lights should have their own setup and be linked to day-night cycle, throw the light tween aswell
- Proper console command for debug interaction
## Gameplay
- Fully functional furniture and storage menus for resort customization
- Setting: Camera smoothing
- Setting: Skip Intro Cinematic
- Setting: Auto-run
- Controls Custom Input Binding
- Reformat screens in the main menu to fit with the new load game screen
## Music
- Title theme for main menu
- Night theme (1 or 2 songs)
- Project Color jingle
- Aphid Death jingle
- Possible Shop themes?

# [Full Version Changelog]
### Fixed
- Logger now works properly (oops)
- Light no longer interacts with most UI elements
- Item shop text not showing up correctly
- You can no longer multi-interact with various entities at the same time
### Gameplay
- Resort map has been again redesigned to be smaller and more structured
- Added a free camera mode which can be toggled with T
- You can now take screenshots, any photos taken are stored inside the current savefile in a folder called "screenshots"
- A few UI elements have been added for ease of interaction
- Added a whole buncha of furniture to decorte with, along with the already existing props
- Redesigned the load game screen to be prettier and more functional
- Petting is faster
- More dialogue tidbits for NPCs, and just general spice up
### Technical
- Created plugin specific for color changing buttons
- Moved skins and items folders to databases folder
- MenuManager is now its own instance component use for menu managment
- More changes to internal tag systems for shops
- Separated canvas into Resort Canvas and Regular Canvas, one is for exclusivally resort envionments, while the other will be used for general gameplay such as another maps
- Interaction sytem has been changed to hammer some bugs and issues with player interaction
- Tilemap has been optimized and organized
- Dialog Manager has been refactor to account for many proper features
- Added startup logo and loading screen (for mere ego and vanity)