Details related to how jobs work and how they should be implemented
As of v0.3, there is not a proper GUI to work with job implementation yet.

Adding a job is really easy, go to res://databases/jobs and add a [JobData] resource, this contains all data related to the job that can be tweaked.

### Name
ID is based on the filename of the resource, make sure to make it unique and concise.

### Skills and Levels
You can add between 0 to 4 different skills to each job, while there isn't a limit or minimum, a standard is set based on difficulty:
- Easy has 0 to 1 skills
- Medium has 1 to 2 skills
- Hard has 2 to 3 skills
- Expert has 3 to 4 skills
- Master has all 4 skills
Levels in itself are a [relative value] added on top of the minimum level for each difficulty, plus a random level variance, all of this configuration is available in [JobMenu.JOB_DIFFICULTY_SETTINGS]
Jobs also grant a 5 point gain in all skills involved, if the required skill level is far lower than that of the aphid's actual skill level, then the skill reward is decreased.

### Base Reward and Base Time
By default, aphids produce berries every 3 minutes, babies produce 2 and adults 4,
this increases by 1.5x if the aphid is well taken care of, plus 1.5x if they have the appropiate trait, PLUS 1.5x if the upgrade is bought (first two get rounded, last one is floor rounded).

This means rewards and times must be adjusted to this ratio to balance out both gain sources, in the best case scenario and aphid produces the following formula:
$$
 (4 * 1.5 * 1.5) = 9.0(Rounded) * 1.5 = 13.5(FloorRounded)
$$
Meaning they would be able to produce 13 berries every 3 minutes, with only care and no skills required.