This is mostly a documentation reminder for myself, but I will attempt to explain how most of their AI works.

# [Aphid]
An aphid has a rudimentary state machine system.
An update on how it works will arrive sometime :tm:
# [AphidData]
This class contains all things related to saving an Aphid State, wheter be in run-time or for storage.
## Status
Stores the current status of the aphid, such as their hunger, thirst, sleepiness, etc. This stats are meant to be on-the-spot trackers for things that happen during gameplay.

## Genes
Stores an Aphids DNA and Identity, their name, body colors, preferences, etc. This arent changed often (with the exception of maybe name).

# [AphidInstance]
This renders an unique aphid instance that holds the data above, an in-game Aphid entity should NEVER carry data that is important for them. Instance exists for this very reason, it also will facilitate a switch between an active state and a passive state (Seeing them in action/Being out the resort)

# [AphidSkin]
Is in charge of displaying an Aphids appearence (color, body part, offset) and code-based animations, like jumping, flipping, squishing, etc.