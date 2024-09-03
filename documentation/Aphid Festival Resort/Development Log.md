
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
- Furniture clips below cliff floor, not a big deal but it annoys me big time
# [Pending Issues]

- game intro Project Color lacks polish, it requires a bright white flash transition into full logo and motted bg, with small jingle
- commands to change money, spawn aphids, change time and print a dialogue line

- Update UI elements to use higher resolution art
- Pause menu button does not play a sound [DONE]
- change bg color and font color of pause buttons to a different color
- options should have focus highlight color for bg and font [FONT DONE]
- options combomenu icons are small [COULDNT FIX]
- furniture menu could be a book? storage some sort of checklist
- furniture icons missing

- increase player sprite framerate [DONE]
- add pickup anim [DONE]
- add step sounds
- different color for harvest outline + particles
- interaction area should be a 360 circle
- pet should also flip player towards aphid

- create item shop shelf
- populate item shop area
- populate kitchen area
- add decoration back
- add menu triggers back to the player [DONE]

## ISSUES
- way to quickly close storage without closing menu
- mouse hover while building is selected, does NOT work
- 


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
- Hopefully fixed resolutions problems once and for all
- Logger now works properly (oops)
- Light no longer interacts with most UI elements
- Item shop text not showing up correctly
- You can no longer multi-interact with various entities at the same time
- The sea no longer GLOWS during the night (shader shenanigans)
- You can no longer use non-food items to create recipes (you could use your two first eggs and softlock yourself)
- You can no longer interact with things while busy (inside a menu, etc)
### Gameplay
- Resort map has been again redesigned to be smaller and more structured
- Added a free camera mode which can be toggled with Q
- You can now take screenshots, any photos taken are stored inside the current savefile in a folder called "screenshots"
- A few UI elements have been added for ease of interaction
- Added a whole buncha of furniture to decorte with, along with the already existing props
- Redesigned the load game screen to be prettier and more functional, you can now see aphids in it, and the time spent in total
- Petting is faster, hooray
- More dialogue tidbits for NPCs, and just general spice up
- Added more and more fluid animations for characters
- Updated visuals to match better
- Title theme, not final but just enough to break the silence
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