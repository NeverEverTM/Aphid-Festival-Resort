using Godot;
using Godot.Collections;

[GlobalClass]
public partial class JobData : Resource
{
    public enum JobDifficulty { Easy, Medium, Hard, Expert, Master }
    [Export(PropertyHint.Enum)]
    public JobDifficulty Difficulty { get; set; }
    [Export] public float BaseTime { get; set; }
    [Export] public int BaseReward { get; set; }
    [Export(PropertyHint.Enum)]
    public Dictionary<AphidData.SkillEnum, int> Skills { get; set; } = [];
    [Export] public Texture2D Background { get; set; }
    public string ID;

#pragma warning disable IDE0290 // Use primary constructor
    public JobData() : this(JobDifficulty.Easy, [], 0, 0, new PlaceholderTexture2D()) { }

    public JobData(JobDifficulty Difficulty, Dictionary<AphidData.SkillEnum, int> Skills, float BaseTime, int BaseReward,Texture2D Background)
    {
        this.Difficulty = Difficulty;
        this.BaseTime = BaseTime;
        this.BaseReward = BaseReward;
        this.Skills = Skills;
        this.Background = Background;
    }
#pragma warning restore IDE0290 // Use primary constructor
}
