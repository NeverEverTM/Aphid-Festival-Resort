
## Current planned features:
Check the README.md for more details.
## Current ONGOING Implementations:
- Create popups for warnings, such as invalid resort names
- Implement debug mode, and its utilities (fun key combination to activate it included)
- Introduce cutscene manager implementation.
- Implement Cooking as a game mechanic

## Bug Tracker:
- [No bugs currently tracked]

## v0.1.1.0
- Added various new food items
- Fixed an issue where new games wouldnt properly clean the scene, so it would carry last save data into it
- Aphid state "Drink" has been removed as is no longer needed
- Foods now manage both drink and food values, so they can apply both if desired
- Items are now dynamically created for conveniences sake, they can still be spawned using the /items folder if it needs to hold custom data, scripts or more nodes than needed
- Tweaked food values and costs
- Shop UI improved + converted into a more modular system as foundation for other shops