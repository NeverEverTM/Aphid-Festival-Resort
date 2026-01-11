using System.Collections.Generic;
using Godot;
using static AphidActions;
using static AphidData;

public class AphidPassive
{
    public AphidInstance Instance;
    public readonly List<CustomBaseTimer<AphidPassive>> DecayActions = [];

    public AphidPassive(AphidInstance Instance)
    {
        this.Instance = Instance;
        Start();
    }

    public void Start()
    {
        DecayActions.Add(new HungerDecay.Passive(BASE_FOOD_DECAY));
        DecayActions.Add(new ThirstDecay.Passive(BASE_THIRST_DECAY));
        DecayActions.Add(new RestDecay.Passive(BASE_SLEEP_DECAY));
        DecayActions.Add(new RestGain.Passive(BASE_SLEEP_GAIN));
        DecayActions.Add(new AffectionDecay.Passive(BASE_AFFECTION_DECAY));

        DecayActions.Add(new BreedTimer.Passive(Breed_Cooldown, Breed_Cooldown - Instance.Status.BreedBuildup, true));
        DecayActions.Add(new HarvestTimer.Passive(Harvest_Cooldown, Harvest_Cooldown - Instance.Status.HarvestBuildup, true));
    }

    public void Process(float delta)
    {
        for (int i = 0; i < DecayActions.Count; i++)
            DecayActions[i].Process(this, delta);

        // TODO: IMPLEMENT PASSIVE TRAITS
        // Aphid Hover better control
        // Aphid Info better control
    }
}
