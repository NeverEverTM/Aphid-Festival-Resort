This is mostly a documentation reminder for myself, but I will attempt to explain how most of their AI works.

# [Aphid.cs]
An aphid has a rudimentary state machine system, each state implement the following switch cases:
- Process functions to run in [Process()]
- State Removal procedure to dispose of it properly. [SetAphidState(AphidState)]
- State Initialization procedure to properly setup the state. [SetAphidState(AphidState)]
- Startup State procedure, if needed, this will run when first loaded to resume their action before Save&Quit

A better way to create a more modular system will be on the way later on but for now I am focused on functionality.

# [AphidData.cs]
This class contains all things related to saving an Aphid State, wheter be in run-time or for storage.
There is two classes in it (plus another one that is in a separate script for some reason)

## Status
Stores the current status of the aphid, such as their hunger, thirst, sleepiness, etc. This stats are meant to be on-the-spot trackers for things that happen during gameplay.

## Genes
Stores an Aphids DNA and Identity, their name, body colors, preferences, etc. This arent changed often (with the exception of maybe name).

## Instance (located in [AphidInstance.cs])
This renders an unique aphid instance that holds the data above, an Aphid in-game entity should NEVER carry data that is important for them. Instance exists for this very reason, it also will facilitate a switch between an active state and a passive state (Seeing them in action/Being out the resort)

# [AphidSkin.cs]
Is in charge of displaying an Aphids appearence (color, body part, offset) and code-based animations, like jumping, flipping, squishing, etc.