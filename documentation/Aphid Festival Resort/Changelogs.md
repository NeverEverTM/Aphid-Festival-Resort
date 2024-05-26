## v0.1.1
- Added various new food items
- Fixed an issue where new games wouldnt properly clean the scene, so it would carry last save data into it
- Aphid state "Drink" has been removed as is no longer needed
- Foods now manage both drink and food values, so they can apply both if desired
- Items are now dynamically created for conveniences sake, they can still be spawned using the /items folder if it needs to hold custom data, scripts or more nodes than needed
- Tweaked food values and costs
- Shop UI improved + converted into a more modular system as foundation for other shops
### v0.1.2
- Implemented Cooking as a mechanic, players can now use two to one ingredients to create recipes.
- New Day-Night cycle, with a Morning, Noon, Sunset and Night that display and change hourly
- You can now pinpoint an Aphid location by double-clicking on their frame in the generations menu.
- Changed several pricing and food values for balancing
- Removed recipes from shop, as they are now redundant
- 1.1 and 1.0
Bug Fixes
- Aphids who died will persist even after reloading, as far as I know it doesnt affect nothing apart from having "ghost" Aphids who shortly vanished after reloading a save.
  This new update should delete all instances and properly see them as dead.
- Due to the previous issue, the save system had to be touched upon along with some improvements, moving between saves is dangerous but I did told you in the load screen.
- Issue with Z-depth has made me change how things are layered, it should be remain the same however
- Items spawned could act very funky as some properties werent intially set to be inserted when created, items should act fine now.