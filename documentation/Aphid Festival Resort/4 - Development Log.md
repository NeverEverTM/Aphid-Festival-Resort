# v0.2.2

## Work In Progress
- Lobby [WIP]
	- NPCs meant for this area are Joy (randomly enters), Frontdesk NPC, Job board NPCs, maybe some random appearances from the og game, like the owner himself. 
- 
# Resort Upgrades
Can be purchased per resort/globally
Can have multiple levels
Can be resort-dependant (but be purchased anywhere, so categorized?)

Upgrade Slot
- Name
- Cost
- Clicking shows purchase button and description
- Color depends on category
- Gain a star depending on level

# Aphid Info
When aphid is nearby/aphid is picked up
- Show tab to pull
- If R is pressed, pull automatically
When aphid is dropped or too far from shown aphid:
- Tab is pushed away
When all aphids dissapear and too far from shown aphid:
- Tab is hidden
# Aphid
Check if:
- Breed works
- Eating works
- Social works
- Train works
- That aphids are saved correctly between room instances
## Bug And Polish Tracker:
- Terrible variety in foods, some dont even make sense, rethink (<- need a debug gui for this)
- add a particle popup warning for interactables who need an aphid
- finish runmill animation, kindergarten sprite and jump rope sprite
- finish all localization files for requests
- finish tutorial localization strings
- you can zoom in and out in furniture store
- update tutorial strings
- furniture interactable does not work on this version due to refactoring, it should also account for out of screen time
- Hungry state is acting up, needs some fixing

## Technical
- create graph for food balance
- add recipe creator gui
- modify atlas gui to make resoureces "local to scene"
- refactor aphid animations and sprites to include individual legs + hat
- Add an option to stop moving backgrounds
## Gameplay
- freaking aphid throwing, make em spin and bounce like a rubber ball for the funnies
- add custom video support for tv (later lol)
- aphid hats sold by tangy cause funny
- Kitchen Recipe Display:
	- Display grid of recipe slots
	- Maybe a search query?
+ aphid skill affinities which make them "lock in" into a single skill, plus a change of color accompanying this.
+ aphid moodlets that signalize how they feel about certain things or what thye need
+ Trade Functionality
	- Players would be able to generate a QR code with the genes (and status if it fits) of an aphid, which they can share with others. 
	-  aphid trading will be either daily procedurally generated aphids by the game or with other players via qr codes (or both)
## Furniture and Food Ideas
- require more recipes for bitter, and recipes for water in general except for Sweet
- Cake Mix: Replace cake recipe with this
- Choco Muffin: Cake Mix + Hustle Berry(?)
- Bench
- Text sign
- Ball Pit
- Water Slide
- Trampoline
- Small Push Car
- Hot Air Balloon
- Jukebox
## Music
- Project Color jingle
- Aphid Death jingle
- night 2
- lobby theme