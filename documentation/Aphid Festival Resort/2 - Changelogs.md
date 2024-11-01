# v0.1.1
- Added various new food items
- Fixed an issue where new games wouldnt properly clean the scene, so it would carry last save data into it
- Aphid state "Drink" has been removed as is no longer needed
- Foods now manage both drink and food values, so they can apply both if desired
- Items are now dynamically created for conveniences sake, they can still be spawned using the /items folder if it needs to hold custom data, scripts or more nodes than needed
- Tweaked food values and costs
- Shop UI improved + converted into a more modular system as foundation for other shops
# v0.1.2
- Implemented Cooking as a mechanic, players can now use two to one ingredients to create recipes.
- New Day-Night cycle, with a Morning, Noon, Sunset and Night that display and change hourly
- You can now pinpoint an Aphid location by double-clicking on their frame in the generations menu.
- Changed several pricing and food values for balancing
- Removed recipes from shop, as they are now redundant
- Implemented a backwards compability system for future updates
Bug Fixes
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