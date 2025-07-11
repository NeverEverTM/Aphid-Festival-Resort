using Godot;
using Godot.Collections;

[GlobalClass]
public partial class JobData : Resource
{
    [Export(PropertyHint.Enum)]
    public Dictionary<AphidData.SkillEnum, int> Skills { get; set; } = [];
    [Export] public float BaseTime { get; set; }
    [Export] public int BaseReward { get; set; }
    [Export] public Texture2D Background { get; set; }

#pragma warning disable IDE0290 // Use primary constructor
    public JobData() : this([], 120, 10, new PlaceholderTexture2D()) { }

    public JobData(Dictionary<AphidData.SkillEnum, int> Skills, float BaseTime, int BaseReward,
            Texture2D Background)
    {
        this.Skills = Skills;
        this.BaseTime = BaseTime;
        this.BaseReward = BaseReward;
        this.Background = Background;
    }
#pragma warning restore IDE0290 // Use primary constructor
}
