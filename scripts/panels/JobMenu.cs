using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

public partial class JobMenu : Control
{
    public static JobMenu Instance { get; private set; }
    private MenuInstance menu;

    [ExportGroup("Essentials")]
    [Export] private InteractableArea2D interactArea;
    [Export] private AnimationPlayer animation_player;
    [Export] private GridContainer slot_container;
    [Export] private PackedScene slot_prefab;
    [Export] private ShaderMaterial job_completion_material, job_current_material;
    [ExportGroup("Information Display")]
    [Export] private Control aphid_node, skill_bar_bg, request_background_frame, information_node;
    [Export] private RichTextLabel description_label;
    [Export] private Label reward_label, timer_label;
    [Export] private RichTextLabel information_label;
    [Export] private Button assign_button;
    [Export] private TextureRect request_background;
    [Export] private TextureRect[] job_skills;
    [ExportCategory("Customization")]
    [Export] private Color[] difficulty_colors;

    private JobRequest current_request = new();
    private AphidInstance current_aphid;
    private bool is_displaying_aphids = false;
    private bool is_current_done;
    private double last_unix_time;

    // Save Data
    internal static Savefile Data { get; set; }
    internal SaveSystem.SaveModule<Savefile> SaveModule;

    // Inmutables
    private const int BASE_SKILL_GAIN = 5,
        HUNGER_LOSS = 20, THIRST_LOSS = 20, REST_LOSS = 15;
    protected record JobDifficultyData
    {
        /// <summary>
        /// The variance of level depending on difficulty, a range going from -LevelRange to LevelRange, capped above MinimumLevel and below MaximumLevel.
        /// </summary>
        public int LevelRange;
        /// <summary>
        /// The minimum level required for skills, this is used as a base from which then jobs add a relative offset to have a greater and higher range.
        /// </summary>
        public int MinimumLevel;
        /// <summary>
        /// The maxmium level required for skills, caps skill level along MinimumLevel
        /// </summary>
        public int MaximumLevel;
        /// <summary>
        /// The currently max amount instances of requests that are allowed to exist for this difficulty. Both Active and Available count towards the quota.
        /// </summary>
        public int MaxQuotaAmount;
    }
    private readonly Dictionary<JobData.JobDifficulty, JobDifficultyData> JOB_DIFFICULTY_SETTINGS = new()
    {
        { JobData.JobDifficulty.Easy, new() { LevelRange = 2, MinimumLevel = 1, MaximumLevel = 6, MaxQuotaAmount = 3 } },
        { JobData.JobDifficulty.Medium, new() { LevelRange = 3, MinimumLevel = 3, MaximumLevel = 12, MaxQuotaAmount = 2 } },
        { JobData.JobDifficulty.Hard, new() { LevelRange = 5, MinimumLevel = 10, MaximumLevel = 25, MaxQuotaAmount = 1 } },
        { JobData.JobDifficulty.Expert, new() { LevelRange = 10, MinimumLevel = 20, MaximumLevel = 50, MaxQuotaAmount = 0 } },
        { JobData.JobDifficulty.Master, new() { LevelRange = 20, MinimumLevel = 50, MaximumLevel = 100, MaxQuotaAmount = 0 }  },
    };
    public void AddRequestAmount(string _source, Dictionary<JobData.JobDifficulty, int> _amounts)
    {
        if (request_amount_buffs.Contains(_source))
            return;

        request_amount_buffs.Add(_source);
        foreach (var _pair in _amounts)
            JOB_DIFFICULTY_SETTINGS[_pair.Key].MaxQuotaAmount += _pair.Value;
    }
    private readonly List<string> request_amount_buffs = [];
    private TextureRect[] job_skills_icons = new TextureRect[4];
    private Label[] job_skills_labels = new Label[4];
    private Color timer_default_color;

    public class Savefile
    {
        /// <summary>
        /// Requests currently active
        /// </summary>
        public List<JobRequest> Active { get; set; } = [];
        /// <summary>
        /// Requests available to select from
        /// </summary>
        public List<JobRequest> Available { get; set; } = [];
    }
    public class JobDataModule : SaveSystem.IDataModule<Savefile>
    {
        public Savefile Default() => new();

        public void Set(Savefile _data)
        {
            Data = _data;

            for (int i = 0; i < Data.Available.Count; i++)
                Data.Available[i].Data = GlobalManager.GetJob(Data.Available[i].ID);

            for (int i = 0; i < Data.Active.Count; i++)
            {
                Data.Active[i].IsActive = true;
                Data.Active[i].Data = GlobalManager.GetJob(Data.Active[i].ID);
                Data.Active[i].AccountForPassedTime();
            }

            Instance.FillRequestQuota();
        }
        public Savefile Get()
        {
            for (int i = 0; i < Data.Active.Count; i++)
                Data.Active[i].LastTimeLoaded = GameManager.Data.Playtime;

            return Data;
        }

        public void Dispose() => Data = null;
    }

    public override void _EnterTree()
    {
        Instance = this;
        last_unix_time = Time.GetUnixTimeFromSystem();

        SaveModule = new("jobs", new JobDataModule(), 500)
        {
            DisposeMode = SaveSystem.SaveMetadata.DisposeMethod.OnRoomTransition
        };
        SaveSystem.AddSaveModule(SaveModule);

        menu = new("jobs", animation_player,
        Open: (_) =>
        {
            CreateRequestGrid();
            DisplayRequest(Data.Active.Count > 0 ? Data.Active[0] : Data.Available[0]);
        },
        TryClose: (_) =>
        {
            if (!is_displaying_aphids)
                return true;
            else
            {
                CreateRequestGrid();
                DisplayRequest(Data.Active.Count > 0 ? Data.Active[0] : Data.Available[0]);
                SoundManager.CreateSound("ui/button_switch");
                return false;
            }
        },
        null,
        Dispose: () => ClearInterface(true));

        for (int i = 0; i < job_skills.Length; i++)
        {
            job_skills_icons[i] = job_skills[i].GetChild<TextureRect>(0);
            job_skills_labels[i] = job_skills[i].GetChild<Label>(1);
        }
        timer_default_color = timer_label.SelfModulate;
        assign_button.Pressed += OnAssignPressed;
        interactArea.OnInteractOnly.Add(SetMenu);
    }
    public override void _ExitTree()
    {
        Instance = null;
    }
    public override void _PhysicsProcess(double delta)
    {
        if (!SaveModule.Loaded || GlobalManager.IsBusy)
            return;
        // ticks down time left while active
        double _tick = Time.GetUnixTimeFromSystem() - last_unix_time;
        last_unix_time = Time.GetUnixTimeFromSystem();

        for (int i = 0; i < Data.Active.Count; i++)
        {
            if (Data.Active[i].IsDone)
                continue;

            Data.Active[i].TimeLeft -= _tick;
            if (Data.Active[i].TimeLeft <= 0)
                Data.Active[i].IsDone = true;
        }

        // show information for the currently selected slot
        if (current_request != null && IsInstanceValid(current_request.Slot))
        {
            if (!current_request.IsDone)
                timer_label.Text = current_request.GetFormattedTimeLeft();
            else if (!is_current_done)
                SetCurrentAsFinished();
        }
    }
    public override void _Notification(int what)
    {
        if (what != NotificationUnpaused)
            return;

        // updates job times back to present after resuming from pausing
        last_unix_time = Time.GetUnixTimeFromSystem();
    }

    public void SetMenu() =>
        _ = CanvasManager.Menus.SetTo(menu);
    public void OnAssignPressed()
    {
        if (current_request == null)
            return;
        if (is_displaying_aphids)
        {   // assign aphid to job request
            if (current_aphid == null)
            {
                SoundManager.CreateSound("ui/button_fail");
                return;
            }

            // too tired to do
            if (current_aphid.Status.Hunger < HUNGER_LOSS || current_aphid.Status.Thirst < THIRST_LOSS ||
                current_aphid.Status.Rest < REST_LOSS)
            {
                SoundManager.CreateSound("aphid/hurt");
                return;
            }

            AssignRequestToAphid();
            CreateRequestGrid(false);
            DisplayRequest(current_request);
            SoundManager.CreateSound("aphid/skill_gain");
        }
        // begin selecting aphids, unless you clicked on a finished request
        else if (!current_request.IsActive)
        {
            if (!CreateAphidGrid())  // there were no aphids to render
            {
                CreateRequestGrid();
                SoundManager.CreateSound("ui/button_fail");
                GlobalManager.CREATE_POPUP("lobby_job_noaphids", this);
                return;
            }
            DisplayAphid(current_aphid.GUID);
            SoundManager.CreateSound("ui/button_select");
        }
        else if (current_request.IsDone)
        {
            FulfillRequest(current_request);
            // display next request, preferably, a current one.
            DisplayRequest(Data.Active.Count > 0 ? Data.Active[0] : Data.Available[0]);
        }
    }

    public bool CreateAphidGrid()
    {
        ClearInterface(false);
        bool _wasGenerated = false;

        foreach (var _pair in GameManager.Aphids)
        {
            if (_pair.Value.Status.Mode != AphidData.EntityStatusType.Passive)
                continue;

            var _pair_clone = _pair;
            current_aphid ??= _pair_clone.Value;
            var _slot = CanvasManager.CreateAphidSlot(_pair_clone.Key, false, DisplayAphid);
            slot_container.AddChild(_slot);
            _slot.SetMeta(StringNames.IdMeta, current_aphid.ID);

            // mark as tired
            if (current_aphid.Status.Hunger < HUNGER_LOSS || current_aphid.Status.Thirst < THIRST_LOSS ||
                current_aphid.Status.Rest < REST_LOSS)
                _slot.SelfModulate = new Color("darkred");

            _wasGenerated = true;
        }
        is_displaying_aphids = _wasGenerated;
        return _wasGenerated;
    }
    public void CreateRequestGrid(bool _clearCurrentRequest = true)
    {
        ClearInterface(_clearCurrentRequest);
        for (int i = 0; i < Data.Active.Count; i++)
            slot_container.AddChild(CreateRequestSlot(Data.Active[i]));

        Data.Available = [.. Data.Available.OrderBy((j) => j.Data.Difficulty)];
        for (int i = 0; i < Data.Available.Count; i++)
            slot_container.AddChild(CreateRequestSlot(Data.Available[i]));
        is_displaying_aphids = false;
    }
    public void ClearInterface(bool _clearRequest = true)
    {
        if (aphid_node.GetChildCount() > 0)
            aphid_node.GetChild(0).QueueFree();

        for (int i = 0; i < slot_container.GetChildCount(); i++)
            slot_container.GetChild(i).QueueFree();

        current_aphid = null;
        if (_clearRequest)
            current_request = null;
    }
    private void SetCurrentAsFinished()
    {
        assign_button.Show();
        assign_button.Text = "lobby_job_finish";

        information_node.Hide();

        timer_label.SelfModulate = new Color("gold");
        timer_label.Text = "lobby_job_timerdone";

        current_request.Slot.GetChild<Control>(1).Material = job_completion_material;
    }
    /// <summary>
    /// Display the given aphid on the top of the information board.
    /// </summary>
    /// <param name="_key"></param>
    private void ShowAphidInTop(Guid _key)
    {
        // set aphid in framed window
        if (aphid_node.GetChildCount() > 0)
            aphid_node.GetChild(0).QueueFree();
        var _node = CanvasManager.CreateAphidSlot(_key, true);
        _node.SelfModulate = new Color(0);
        aphid_node.AddChild(_node);
    }

    private GlowButton CreateRequestSlot(JobRequest _request)
    {
        GlowButton _slot = slot_prefab.Instantiate() as GlowButton;
        Control _slotFrame = _slot.GetChild<Control>(1);
        TextureRect _slotBackground = _slot.GetChild<TextureRect>(0);
        _request.Slot = _slot;
        _slotBackground.Texture = _request.Data.Background;
        _slotFrame.SelfModulate = difficulty_colors[(int)_request.Data.Difficulty];

        // set custom values if is a ongoing request
        if (_request.IsActive)
        {
            _slotFrame.GetChild(0).QueueFree();
            GlowButton _aphidSlot = CanvasManager.CreateAphidSlot(_request.AssignedAphid, true);
            _aphidSlot.Scale = new(1.2f, 1.2f);
            _aphidSlot.SelfModulate = new(0);
            _aphidSlot.Position = new(10, 16);
            _slotFrame.AddChild(_aphidSlot);
            if (_request.IsDone)
                _slotFrame.Material = job_completion_material;
            else
                _slotFrame.Material = job_current_material;
        }

        // display skill icons on slot
        foreach (var _pair in _request.Data.Skills)
        {
            TextureRect _icon = _slot.GetNode("skill_icons").GetChild<TextureRect>((int)_pair.Key);
            _icon.Visible = true;
            _icon.SelfModulate = difficulty_colors[(int)_request.Data.Difficulty];
            _icon.GetChild<TextureRect>(0).Texture = GlobalManager.GetIcon(_pair.Key.ToString().ToLower());
        }

        _slot.Pressed += () => DisplayRequest(_request);
        return _slot;
    }

    // MARK: Job Generation
    /// <summary>
    /// Generates all quotas of jobs according to the number of current active and available requests
    /// </summary>
    public void FillRequestQuota()
    {
        FillRequestQuota(JobData.JobDifficulty.Easy);
        FillRequestQuota(JobData.JobDifficulty.Medium);
        FillRequestQuota(JobData.JobDifficulty.Hard);
        FillRequestQuota(JobData.JobDifficulty.Expert);
        FillRequestQuota(JobData.JobDifficulty.Master);
    }
    /// <summary>
    /// Generates a quota of jobs according to the number of current active and available requests
    /// </summary>
    /// <param name="_difficulty">The specific difficulty to fill in</param>
    private void FillRequestQuota(JobData.JobDifficulty _difficulty)
    {
        int _maxAmount = JOB_DIFFICULTY_SETTINGS[_difficulty].MaxQuotaAmount;
        List<string> _excludeList = [];
        Data.Available.ForEach((r) => _excludeList.Add(r.ID));
        Data.Active.ForEach((r) => _excludeList.Add(r.ID));

        // we count both available and ongoing requests for the total
        for (int _count = Data.Available.Count((j) => j.Data.Difficulty == _difficulty) + Data.Active.Count((j) => j.Data.Difficulty == _difficulty);
                _count < _maxAmount; _count++)
        {
            JobRequest _request = GenerateRandomRequest(_difficulty, _excludeList);
            _excludeList.Add(_request.Data.ID);
            Data.Available.Add(_request);
        }
    }
    private JobRequest GenerateRandomRequest(JobData.JobDifficulty _difficulty, List<string> _excludeList)
    {
        JobData _job = GlobalManager.GetRandomJob(_difficulty, _excludeList);

        JobRequest _request = new()
        {
            ID = _job.ID,
            Skills = new string[_job.Skills.Count],
            MinimumLevels = new int[_job.Skills.Count],
            TimeLeft = _job.BaseTime,
            ChanceToSucceed = 1,
            Data = _job
        };

        // translate values from dictionaries into usable variables
        int i = 0;
        foreach (var _pair in _job.Skills)
        {
            // use different ranges depdending on difficulty range
            int _range = JOB_DIFFICULTY_SETTINGS[_request.Data.Difficulty].LevelRange,
                _minLevel = JOB_DIFFICULTY_SETTINGS[_request.Data.Difficulty].MinimumLevel,
                _maxLevel = JOB_DIFFICULTY_SETTINGS[_request.Data.Difficulty].MaximumLevel;

            _request.MinimumLevels[i] = _minLevel + GlobalManager.RNG.RandiRange(-_range, _range);
            _request.MinimumLevels[i] = Math.Clamp(_request.MinimumLevels[i], _minLevel, _maxLevel);
            _request.Skills[i] = AphidData.SkillNames[(int)_pair.Key];
            i++;
        }

        return _request;
    }
    
    // MARK: Job Fullfilment
    /// <summary>
    /// Automatically creates a tracked job with the current aphid and request given.
    /// </summary>
    private void AssignRequestToAphid()
    {
        if (current_request == null || current_aphid == null)
            return;

        Data.Available.Remove(current_request);
        current_request.AssignedAphid = current_aphid.GUID;
        current_request.IsActive = true;
        current_aphid.EnterMode(AphidData.EntityStatusType.Busy);

        Data.Active.Add(current_request);
    }
    private void FulfillRequest(JobRequest _request)
    {
        if (Data.Active.Remove(_request))
        {
            AphidInstance _aphid = GameManager.Aphids[_request.AssignedAphid];
            _aphid.EnterMode(AphidData.EntityStatusType.Passive);
            _aphid.AddHunger(-HUNGER_LOSS);
            _aphid.AddThirst(-THIRST_LOSS);
            _aphid.AddRest(-REST_LOSS);

            _request.Fulfill();
            _request.Slot.QueueFree();
            FillRequestQuota(_request.Data.Difficulty);
            CreateRequestGrid();
        }
    }
    private void DisplayRequest(JobRequest _request)
    {
        current_request = _request;
        is_current_done = current_request.IsDone;
        SoundManager.CreateSound("ui/button_switch");

        // sets and updates all UI elements
        if (current_request.IsActive)
        {
            information_label.Text = $"{string.Format(Tr("lobby_job_active"), GameManager.Aphids[current_request.AssignedAphid].Genes.Name)}\n"
                    + $"[color=red]{Tr("lobby_job_successchance")}:[/color] {(int)(current_request.ChanceToSucceed * 100)}%";
            ShowAphidInTop(current_request.AssignedAphid);

            // mark it as done if is finised, otherwise, keep default look
            if (current_request.IsDone)
                SetCurrentAsFinished();
            else
            {
                information_node.Show();
                assign_button.Hide();
            }
        }
        else
        {
            if (aphid_node.GetChildCount() > 0)
                aphid_node.GetChild(0).QueueFree();

            timer_label.SelfModulate = timer_default_color;
            timer_label.Text = current_request.GetFormattedTimeLeft();

            information_node.Hide();

            assign_button.Text = "lobby_job_select";
            assign_button.Show();
        }

        description_label.Text = Tr(current_request.ID);
        skill_bar_bg.SelfModulate = request_background_frame.SelfModulate = difficulty_colors[(int)current_request.Data.Difficulty];
        request_background.Texture = current_request.Data.Background;
        reward_label.Text = current_request.Data.BaseReward.ToString("000");

        // display skill levels and icons
        for (int i = 0; i < job_skills.Length; i++)
        {
            if (current_request.Skills.Length <= i)
            {
                job_skills[i].Visible = false;
                continue;
            }
            job_skills_icons[i].Texture = GlobalManager.GetIcon(current_request.Skills[i]);
            job_skills_icons[i].GetParent<Control>().SelfModulate = difficulty_colors[(int)current_request.Data.Difficulty];
            job_skills_labels[i].Text = current_request.MinimumLevels[i].ToString();
            job_skills_labels[i].SelfModulate = new Color("white");
            job_skills[i].Visible = true;
        }

        TweenRequestSlot(current_request.Slot);
    }
    private void DisplayAphid(Guid _key)
    {
        current_aphid = GameManager.Aphids[_key];

        ShowAphidInTop(_key);

        // Calculate chance to succed based on if the skills below meet minimum levels requested
        current_request.ChanceToSucceed = 1;
        for (int i = 0; i < current_request.Skills.Length; i++)
        {
            float _level = current_aphid.Genes.Skills[current_request.Skills[i]].Level;
            if (_level > 0)
                current_request.ChanceToSucceed *= Math.Min((float)_level / current_request.MinimumLevels[i], 1);
            else // we cant divide by zero, so we dont, but also, we still need an accurate percentage, so plus one to everything
                current_request.ChanceToSucceed *= Math.Min((_level + 1.0f) / (current_request.MinimumLevels[i] + 1.0f), 1);
        }

        // set all labels
        assign_button.Text = "lobby_job_assign";
        string[] _list = [
                $"{Tr("lobby_job_aphidname")}: {GameManager.Aphids[_key].Genes.Name}",
                $"{Tr("lobby_job_successchance")}: {(int)(current_request.ChanceToSucceed * 100)}%",
                (current_aphid.Status.Hunger < HUNGER_LOSS ? $"[color=red]{Tr("lobby_job_toohungry")}[/color]" : string.Empty),
                (current_aphid.Status.Thirst < THIRST_LOSS ? $"[color=red]{Tr("lobby_job_toothirsty")}[/color]" : string.Empty),
                (current_aphid.Status.Rest < REST_LOSS ? $"[color=red]{Tr("lobby_job_toosleepy")}[/color]" : string.Empty)
                ];
        description_label.Text = string.Join("\n", _list);

        // display level values and wheter they are meet/unmeet/non-required
        var _keys = current_aphid.Genes.Skills.Keys.ToList();
        var _values = current_aphid.Genes.Skills.Values.ToList();

        for (int i = 0; i < current_aphid.Genes.Skills.Count; i++)
        {
            job_skills_icons[i].Texture = GlobalManager.GetIcon(_keys[i]);
            job_skills_labels[i].Text = _values[i].Level.ToString();

            int _index = Array.IndexOf(current_request.Skills, _keys[i]);
            if (_index == -1)
                job_skills_labels[i].SelfModulate = new Color(0.75f, 0.75f, 0.75f, 0.75f);
            else if (_values[i].Level < current_request.MinimumLevels[_index])
                job_skills_labels[i].SelfModulate = new Color("red");
            else
                job_skills_labels[i].SelfModulate = new Color("white");

            job_skills[i].Visible = true;
        }
    }

    private void TweenRequestSlot(Control _slot)
    {
        if (current_request.Slot.Rotation != 0)
            return;

        var _tween = _slot.CreateTween();
        _tween.SetTrans(Tween.TransitionType.Spring);
        _tween.TweenProperty(_slot, "rotation", -0.1, 0.2).FromCurrent();
        _tween.TweenProperty(_slot, "rotation", 0.05, 0.1).FromCurrent();
        _tween.TweenProperty(_slot, "rotation", 0, 0.1).FromCurrent();
        _tween.Play();
    }

    /// <summary>
    /// Data instance that stores jobs for savefile serialization
    /// </summary>
    public class JobRequest
    {
        // Savefile variables, saved per taken request
        public string ID { get; set; }
        public string[] Skills { get; set; }
        public int[] MinimumLevels { get; set; }

        /// <summary>
        /// Last playtime registered for this request, do not uses real time, instead is based on playtime
        /// </summary>
        public double LastTimeLoaded { get; set; }
        public bool IsDone { get; set; }
        public Guid AssignedAphid { get; set; }
        public float ChanceToSucceed { get; set; }
        public double TimeLeft { get; set; }

        // Runtime variables, these are not serialized on save, and are reassigned on startup
        [JsonIgnore] public JobData Data;
        [JsonIgnore] public Control Slot;
        [JsonIgnore] public bool IsActive;

        public void AccountForPassedTime()
        {
            if (IsDone)
                return;

            TimeLeft -= GameManager.Data.Playtime - LastTimeLoaded;
            LastTimeLoaded = GameManager.Data.Playtime;

            if (TimeLeft <= 0)
                IsDone = true;
        }
        public string GetFormattedTimeLeft() =>
            ((int)(TimeLeft / 60)).ToString("00") + ":" + ((int)(TimeLeft % 60)).ToString("00");
        public void Fulfill()
        {
            float _rollForInitiative = GlobalManager.RNG.Randf();
            AphidInstance _aphid = GameManager.Aphids[AssignedAphid];

            if (_rollForInitiative <= ChanceToSucceed)
            {
                Player.AddCurrency(Data.BaseReward, Player.CurrencySource.JobGain);
                SoundManager.CreateSound("ui/kitchen_success");
            }
            else
                SoundManager.CreateSound("ui/kitchen_fail");

            // rewarded skill points, for every skill involved in this request
            // if the aphid is higher level, then decrease the amount gained from a low level request
            for (int i = 0; i < Skills.Length; i++)
                _aphid.Genes.Skills[Skills[i]].GivePoints(BASE_SKILL_GAIN * /* Multiply by 1 or less */
                        Mathf.Min(1, (MinimumLevels[i] + 1) / (_aphid.Genes.Skills[Skills[i]].Level + 1)));
        }
    }
}