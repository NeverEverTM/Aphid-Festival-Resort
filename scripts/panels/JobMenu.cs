using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

public partial class JobMenu : Control
{
    public static JobMenu Instance { get; private set; }
    private MenuInstance menu;

    [Export] private InteractableArea2D interactArea;
    [Export] private AnimationPlayer animation_player;
    [Export] private GridContainer slot_container;
    [Export] private PackedScene slot_prefab;
    [Export] private ShaderMaterial job_completion_material, job_current_material;
    [ExportGroup("Information Display")]
    [Export] private Control aphid_node;
    [Export] private Label description_label, reward_label, timer_label;
    [Export] private Button assign_button;
    [Export] private TextureRect request_background;
    [Export] private TextureRect[] job_skills;
    [ExportGroup("Slot Customization")]
    [Export] private Color[] difficulty_colors;
    [Export] private int[] difficulty_rand_ranges;
    [Export] private int[] difficulty_range_mins;

    public enum JobDifficulty { Easy, Medium, Hard, Expert, Master }
    private JobRequest current_request = new();
    private AphidInstance current_aphid;
    private bool is_displaying_aphids = false;
    private bool is_current_done;
    private double last_time_tick;

    private const int BASE_SKILL_GAIN = 5;
    private const int MIN_HUNGER = 20;
    private const int MIN_THIRST = 20;
    private const int MAX_TIREDNESS = 80;

    // Save Data
    internal static Savefile Data { get; set; } = new();
    internal SaveSystem.SaveModule<Savefile> SaveModule;

    // Inmutables
    private TextureRect[] job_skills_icons = new TextureRect[4];
    private Label[] job_skills_labels = new Label[4];
    private Color timer_default_color;

    public class Savefile
    {
        public List<JobRequest> Current { get; set; } = [];
        public List<JobRequest> Available { get; set; } = [];
        public Dictionary<JobDifficulty, int> MaxAmounts { get; set; }

        public Savefile()
        {
            MaxAmounts = new()
            {
                { JobDifficulty.Easy, 5 },
                { JobDifficulty.Medium, 3 },
                { JobDifficulty.Hard, 1 },
                { JobDifficulty.Expert, 0 },
                { JobDifficulty.Master, 0 },
            };
        }
    }
    public class JobDataModule : SaveSystem.IDataModule<Savefile>
    {
        public Savefile Default() => new();

        public void Set(Savefile _data)
        {
            Data = _data;

            for (int i = 0; i < Data.Available.Count; i++)
                Data.Available[i].Data = FetchJob(Data.Available[i].Difficulty, Data.Available[i].DataID);

            for (int i = 0; i < Data.Current.Count; i++)
            {
                Data.Current[i].IsCurrent = true;
                Data.Current[i].Data = FetchJob(Data.Current[i].Difficulty, Data.Current[i].DataID);
                Data.Current[i].AccountForPassedTime();
            }

            Instance.FillRequestQuota(JobDifficulty.Easy);
            Instance.FillRequestQuota(JobDifficulty.Medium);
            Instance.FillRequestQuota(JobDifficulty.Hard);
            Instance.FillRequestQuota(JobDifficulty.Expert);
            Instance.FillRequestQuota(JobDifficulty.Master);
        }
        public Savefile Get()
        {
            for (int i = 0; i < Data.Current.Count; i++)
                Data.Current[i].LastTimeLoaded = GameManager.Data.Playtime;

            return Data;
        }

        public void Dispose()
        {
            Data = new();
        }
    }

    public override void _EnterTree()
    {
        Instance = this;
        last_time_tick = Time.GetUnixTimeFromSystem();

        SaveModule = new("jobs", new JobDataModule(), 500)
        {
            DisposeMode = SaveSystem.SaveMetadata.DisposeMethod.OnRoomTransition
        };
        SaveSystem.AddSaveModule(SaveModule);

        menu = new("jobs", animation_player,
        Open: (_) =>
        {
            CreateRequestGrid();
            DisplayRequest(Data.Current.Count > 0 ? Data.Current[0] : Data.Available[0]);
        },
        TryClose: (_) =>
        {
            if (!is_displaying_aphids)
                return true;
            else
            {
                CreateRequestGrid();
                DisplayRequest(Data.Current.Count > 0 ? Data.Current[0] : Data.Available[0]);
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
    public override void _Process(double delta)
    {
        // ticks down time left while active
        double _tick = Time.GetUnixTimeFromSystem() - last_time_tick;
        for (int i = 0; i < Data.Current.Count; i++)
        {
            if (Data.Current[i].IsDone)
                continue;

            Data.Current[i].TimeLeft -= _tick;
            if (Data.Current[i].TimeLeft <= 0)
                Data.Current[i].IsDone = true;
        }
        last_time_tick = _tick;

        // show information for the currently selected slot
        if (current_request != null && IsInstanceValid(current_request.Slot))
        {
            if (!current_request.IsDone)
                timer_label.Text = current_request.GetFormattedTimeLeft();
            else if (!is_current_done)
                ShowAssignAsFinished(current_request);
        }
    }
    public override void _Notification(int what)
    {
        if (what != NotificationUnpaused)
            return;

        // updates job times back to present after resuming from pausing
        last_time_tick = Time.GetUnixTimeFromSystem();
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
            if (current_aphid.Status.Hunger < MIN_HUNGER || current_aphid.Status.Thirst < MIN_THIRST ||
                current_aphid.Status.Tiredness > MAX_TIREDNESS)
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
        else if (!current_request.IsCurrent)
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
            DisplayRequest(Data.Current.Count > 0 ? Data.Current[0] : Data.Available[0]);
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

            // mark as tired
            if (current_aphid.Status.Hunger < MIN_HUNGER || current_aphid.Status.Thirst < MIN_THIRST ||
                current_aphid.Status.Tiredness > MAX_TIREDNESS)
                _slot.SelfModulate = new Color("darkred");

            _wasGenerated = true;
        }
        is_displaying_aphids = _wasGenerated;
        return _wasGenerated;
    }
    public void CreateRequestGrid(bool _clearCurrentRequest = true)
    {
        ClearInterface(_clearCurrentRequest);
        for (int i = 0; i < Data.Current.Count; i++)
            slot_container.AddChild(CreateRequestSlot(Data.Current[i]));

        Data.Available = [.. Data.Available.OrderBy((j) => j.Difficulty)];
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
    private void ShowAssignAsFinished(JobRequest _request)
    {
        assign_button.Show();
        timer_label.SelfModulate = new Color("gold");
        timer_label.Text = "lobby_job_timerdone";
        assign_button.Text = "lobby_job_finish";
        _request.Slot.GetChild<Control>(1).Material = job_completion_material;
        is_current_done = true;
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

    /// <summary>
    /// Generate a quota of jobs according to the number of current requests available and ongoing.
    /// </summary>
    /// <param name="_difficulty"></param>
    private void FillRequestQuota(JobDifficulty _difficulty)
    {
        int _maxAmount = Data.MaxAmounts[_difficulty];

        // we count both available and ongoing requests for the total
        for (int _count = Data.Available.Count((j) => j.Difficulty == _difficulty)
                    + Data.Current.Count((j) => j.Difficulty == _difficulty);
                _count < _maxAmount; _count++)
        {
            JobRequest _request = GenerateRandomRequest(_difficulty);
            Data.Available.Add(_request);
        }
        CreateRequestGrid();
    }
    private GlowButton CreateRequestSlot(JobRequest _request)
    {
        GlowButton _slot = slot_prefab.Instantiate() as GlowButton;
        Control _slotFrame = _slot.GetChild<Control>(1);
        TextureRect _slotBackground = _slot.GetChild<TextureRect>(0);
        _request.Slot = _slot;
        _slotBackground.Texture = _request.Data.Background;
        _slotFrame.SelfModulate = difficulty_colors[(int)_request.Difficulty];

        // set custom values if is a ongoing request
        if (_request.IsCurrent)
        {
            _slotFrame.GetChild(0).QueueFree();
            GlowButton _aphidSlot = CanvasManager.CreateAphidSlot(_request.AssignedAphid, true);
            _aphidSlot.Scale = new(1.2f, 1.2f);
            _aphidSlot.SelfModulate = new(0);
            _aphidSlot.Position = new(10, 16);
            _slotFrame.AddChild(_aphidSlot);
            _slotBackground.SelfModulate = new Color(0.4f, 0.4f, 0.4f);
            if (_request.IsDone)
                _slotFrame.Material = job_completion_material;
            else
                _slotFrame.Material = job_current_material;
        }

        // display skill icons on slot
        for (int i = 0; i < _request.Skills.Length; i++)
        {
            TextureRect _icon = _slot.GetChild<TextureRect>(i + 2);
            _icon.Visible = true;
            _icon.SelfModulate = difficulty_colors[(int)_request.Difficulty];
            _icon.GetChild<TextureRect>(0).Texture = GlobalManager.GetIcon(_request.Skills[i]);
        }

        _slot.Pressed += () => DisplayRequest(_request);
        return _slot;
    }
    private JobRequest GenerateRandomRequest(JobDifficulty _difficulty)
    {
        JobData _job = FetchRandomJob(_difficulty, out int _id);

        // attempt to fetch a data pack that doesnt exist in the board already
        while (Data.Available.Exists(r => r.Difficulty == _difficulty && r.DataID == _id))
            _job = FetchRandomJob(_difficulty, out _id);

        JobRequest _request = new()
        {
            DataID = _id,
            Skills = new string[_job.Skills.Count],
            MinimumLevels = new int[_job.Skills.Count],
            TimeLeft = _job.BaseTime,
            Difficulty = _difficulty,
            ChanceToSucceed = 1,
            Data = _job
        };

        // translate values from dictionaries into usable variables
        int i = 0;
        foreach (var _pair in _job.Skills)
        {
            // use different ranges depdending on difficulty range
            _request.MinimumLevels[i] = Math.Clamp(
                GlobalManager.RNG.RandiRange(_pair.Value - difficulty_rand_ranges[(int)_request.Difficulty],
                    _pair.Value + difficulty_rand_ranges[(int)_request.Difficulty]), difficulty_range_mins[(int)_request.Difficulty], 100);
            var _value = AphidData.SkillNames[(int)_pair.Key];
            _request.Skills[i] = _value;
            i++;
        }

        return _request;
    }
    private static JobData FetchRandomJob(JobDifficulty _difficulty, out int _id)
    {
        // access the difficulty folder
        string _difficultyName = _difficulty.ToString().ToLower();
        string _path = GlobalManager.ABSOLUTE_JOBS_DB_PATH + _difficultyName + "/";
        // get a random file from that folder
        int _count = DirAccess.GetFilesAt(_path).Length;
        _id = GlobalManager.RNG.RandiRange(0, _count - 1);

        return ResourceLoader.Load<JobData>(_path + $"job_{_id}.tres");
    }
    private static JobData FetchJob(JobDifficulty _difficulty, int _id)
    {
        // access the difficulty folder
        string _difficultyName = _difficulty.ToString().ToLower();
        string _path = GlobalManager.ABSOLUTE_JOBS_DB_PATH + _difficultyName + "/";

        return ResourceLoader.Load<JobData>(_path + $"job_{_id}.tres");
    }

    /// <summary>
    /// Automatically creates a tracked job with the current aphid and request given.
    /// </summary>
    private void AssignRequestToAphid()
    {
        if (current_request == null || current_aphid == null)
            return;

        Data.Available.Remove(current_request);
        current_request.AssignedAphid = current_aphid.GUID;
        current_request.IsCurrent = true;
        current_aphid.Status.Mode = AphidData.EntityStatusType.Busy;

        Data.Current.Add(current_request);
    }
    private void FulfillRequest(JobRequest _request)
    {
        if (Data.Current.Remove(_request))
        {
            AphidInstance _aphid = GameManager.Aphids[_request.AssignedAphid];
            _aphid.Status.Mode = AphidData.EntityStatusType.Passive;
            _aphid.AddHunger(-20);
            _aphid.AddThirst(-20);
            _aphid.AddTiredness(-20);

            _request.Fulfill();
            _request.Slot.QueueFree();
            FillRequestQuota(_request.Difficulty);
        }
    }
    private void DisplayRequest(JobRequest _request)
    {
        is_current_done = false;
        // sets all label texts
        if (_request.IsCurrent)
        {
            // alternatively, if this is the current request and its done, then complete it
            if (current_request != null && _request.AssignedAphid == current_request.AssignedAphid && _request.IsDone)
            {
                FulfillRequest(_request);
                DisplayRequest(Data.Current.Count > 0 ? Data.Current[0] : Data.Available[0]);
                return;
            }

            // mark it as done if is finised, otherwise, keep default look
            if (_request.IsDone)
                ShowAssignAsFinished(_request);
            else
            {
                assign_button.Hide();
                timer_label.SelfModulate = new Color("coral");
            }

            string[] _list = [string.Format(Tr("lobby_job_active"), GameManager.Aphids[_request.AssignedAphid].Genes.Name),
                $"{Tr("lobby_job_successchance")}: {(int)(_request.ChanceToSucceed * 100)}%"];
            description_label.Text = string.Join("\n", _list);
            ShowAphidInTop(_request.AssignedAphid);
        }
        else
        {
            timer_label.SelfModulate = timer_default_color;
            timer_label.Text = _request.GetFormattedTimeLeft();
            assign_button.Text = "lobby_job_select";
            description_label.Text = Tr($"job_{_request.Difficulty.ToString().ToLower()}_{_request.DataID}");
            assign_button.Show();
            if (aphid_node.GetChildCount() > 0)
                aphid_node.GetChild(0).QueueFree();
        }

        request_background.Texture = _request.Data.Background;
        reward_label.Text = _request.Data.BaseReward.ToString("000");

        // display skill levels and icons
        for (int i = 0; i < job_skills.Length; i++)
        {
            if (_request.Skills.Length <= i)
            {
                job_skills[i].Visible = false;
                continue;
            }
            job_skills_icons[i].Texture = GlobalManager.GetIcon(_request.Skills[i]);
            job_skills_labels[i].Text = _request.MinimumLevels[i].ToString();
            job_skills_labels[i].SelfModulate = new Color("white");
            job_skills[i].Visible = true;
        }

        // set request
        current_request = _request;
        SoundManager.CreateSound("ui/button_switch");

        // tween slot container
        if (_request.Slot.Rotation != 0)
            return;

        var _tween = _request.Slot.CreateTween();
        _tween.SetTrans(Tween.TransitionType.Spring);
        _tween.TweenProperty(_request.Slot, "rotation", -0.1, 0.2).FromCurrent();
        _tween.TweenProperty(_request.Slot, "rotation", 0.05, 0.1).FromCurrent();
        _tween.TweenProperty(_request.Slot, "rotation", 0, 0.1).FromCurrent();
        _tween.Play();
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
        string[] _list = [$"{Tr("lobby_job_aphidname")}: {GameManager.Aphids[_key].Genes.Name}",
                $"{Tr("lobby_job_successchance")}: {(int)(current_request.ChanceToSucceed * 100)}%"];
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
                job_skills_labels[i].SelfModulate = new Color("gray");
            else if (_values[i].Level < current_request.MinimumLevels[_index])
                job_skills_labels[i].SelfModulate = new Color("red");
            else
                job_skills_labels[i].SelfModulate = new Color("white");

            job_skills[i].Visible = true;
        }
    }

    public class JobRequest
    {
        // Static custom values set at creation, do not change after
        public int DataID { get; set; }
        public string[] Skills { get; set; }
        public int[] MinimumLevels { get; set; }
        public JobDifficulty Difficulty { get; set; }
        public Guid AssignedAphid { get; set; }
        public float ChanceToSucceed { get; set; }

        // Dynamic variables, change constantly while the request is active
        public double TimeLeft { get; set; }
        /// <summary>
        /// Last playtime registered for this request, do not uses real time, instead is based on played time.
        /// </summary>
        public double LastTimeLoaded { get; set; }
        public bool IsDone { get; set; }

        // Runtime variables, these are not serialized on save, and are reassigned on startup
        [JsonIgnore] public JobData Data;
        [JsonIgnore] public Control Slot;
        [JsonIgnore] public bool IsCurrent;

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
                Player.AddCurrency(Data.BaseReward);
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