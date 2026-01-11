using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class SceneManager : Node2D
{
    public static SceneManager Instance { get; private set; }

    /// <summary>
    /// Current room that we are loaded in.
    /// </summary>
    public static string CurrentScene { get; private set; }
    /// <summary>
    /// Wheter is going into active gameplay or the main menu.
    /// </summary>
    public static bool IsSwitching { get; private set; }
    /// <summary>
    /// Wheter a scene is currently being loaded or not.
    /// </summary>
    public static bool IsBusy { get; private set; }

    private static bool WaitingOnDeferred = false;

    private static List<Action<SceneArgs>>
        OnPreLoad = [],
        OnPostLoad = [],
        OnGameInit = [],
        OnGameFinish = [];
    public enum EventEnum { OnPreLoad, OnPostLoad, OnGameInit, OnGameFinish }
    public static void AddEventListener(Action<SceneArgs> _action, EventEnum _event)
    {
        switch (_event)
        {
            case EventEnum.OnPreLoad:
                OnPreLoad.Add(_action);
                break;
            case EventEnum.OnPostLoad:
                OnPostLoad.Add(_action);
                break;
            case EventEnum.OnGameInit:
                OnGameInit.Add(_action);
                break;
            case EventEnum.OnGameFinish:
                OnGameFinish.Add(_action);
                break;
        }
    }
    public static void RemoveEventListener(Action<SceneArgs> _action, EventEnum _event)
    {
        switch (_event)
        {
            case EventEnum.OnPreLoad:
                OnPreLoad.Remove(_action);
                break;
            case EventEnum.OnPostLoad:
                OnPostLoad.Remove(_action);
                break;
            case EventEnum.OnGameInit:
                OnGameInit.Remove(_action);
                break;
            case EventEnum.OnGameFinish:
                OnGameFinish.Remove(_action);
                break;
        }
    }

    public struct RoomData(string Name, int EntryIndex, Vector2 EntryDirection, Vector2 Position)
    {
        public string Name { get; set; } = Name;
        public int EntryIndex { get; set; } = EntryIndex;
        public Vector2 EntryDirection { get; set; } = EntryDirection;
        public Vector2 Position { get; set; } = Position;
    }
    public class SceneArgs : EventArgs
    {
        public RoomData roomData;
        public string CurrentScene;
        public string NextCurrentScene;
        public bool IsSwitching;
    }

    public override void _EnterTree()
    {
        Instance = this;
    }

    /// <summary>
    /// Same as Load(), but it triggers a game state switch depending on the current state. Used for going in and out of active gameplay.
    /// </summary>
    /// <param name="_sceneToLoad">Name of the scene to load.</param>
    /// <param name="_cache">Allows to cache the PackedScene to load it faster next time.</param>
    /// <param name="_isInGame">Are we going in or out of the game? For example, are we leaving back to the main menu or into a savefile?</param>
    /// <returns></returns>
    public static async Task Switch(string _sceneToLoad, bool _isInGame)
    {
        if (GlobalManager.IsBusy || IsBusy)
        {
            DebugLogger.Print(DebugLogger.LogPriority.Warning, $"SceneManager: Attempted to switch state while busy. Global: {GlobalManager.IsBusy}. Local: {IsBusy}");
            return;
        }
        // acknowledge the transition between "playing in a game" and "not in a game"
        if (_isInGame != GlobalManager.IsInGame)
        {
            GlobalManager.IsInGame = _isInGame;
            IsSwitching = _isInGame;
        }
        SoundManager.StopSong();
        // load scene with special properties for game transition
        await Load(_sceneToLoad, new(), GlobalManager.LEAF_LOADING_SCENE);
        IsSwitching = false;
    }
    /// <summary>
    /// Load a packed scene from a path as the root node, this includes basic room transitoning to game state transitioning.
    /// </summary>
    /// <param name="_sceneToLoad">Name of the scene to load.</param>
    /// <param name="_roomData">Door of the next room you want to access.</param>
    /// <param name="_loadingScreen">Path to the LoadScreen to instantiate and use.</param>
    public static async Task Load(string _sceneToLoad, RoomData _roomData, string _loadingScreen = GlobalManager.FADE_LOADING_SCENE)
    {
        if (GlobalManager.IsBusy || IsBusy)
        {
            DebugLogger.Print(DebugLogger.LogPriority.Warning, $"SceneManager: Attempted to load scene while busy. Global: {GlobalManager.IsBusy}. Local: {IsBusy}");
            return;
        }

        string _path = GlobalManager.ABSOLUTE_ROOMS_PATH + _sceneToLoad + ".tscn";
        if (!ResourceLoader.Exists(_path))
        {
            DebugLogger.Print(DebugLogger.LogPriority.Error, $"SceneManager: <{_sceneToLoad}> does not exist.");
            return;
        }

        GlobalManager.IsBusy = IsBusy = true;
        bool _isARoomTransition = !IsSwitching && GlobalManager.IsInGame;
        SceneArgs _args = new()
        {
            IsSwitching = IsSwitching,
            CurrentScene = CurrentScene,
            NextCurrentScene = _sceneToLoad,
            roomData = _roomData
        };
        DebugLogger.Print(DebugLogger.LogPriority.Info, $"SceneManager: Loading scene <{_sceneToLoad}>.");

        LoadScreen _loadScreen = await InstantiateLoadingScreen(_loadingScreen);
        if (_isARoomTransition)
            PlayPlayerTransition(_goingIn: true, _roomData);
        else
            await GlobalManager.Utils.InvokeAwaitableEventListeners(ref OnGameFinish, _args);
        await _loadScreen.RunIN();

        await GlobalManager.Utils.InvokeAwaitableEventListeners(ref OnPreLoad, _args, true);
        if (_isARoomTransition)
            await SaveSystem.SaveProfile(true, true);
        SoundManager.CleanAllSounds();
        GlobalManager.CleanAllParticles();
        PackedScene _scene = await GlobalManager.PRELOAD_RESOURCE(_path, true) as PackedScene;

        CurrentScene = _sceneToLoad;
        WaitingOnDeferred = true;
        Callable _sceneLoad = Callable.From(() =>
        {
            Instance.GetTree().ChangeSceneToPacked(_scene);
            WaitingOnDeferred = false;
        });
        _sceneLoad.CallDeferred();
        while (WaitingOnDeferred) // wait until the deferred call ends
            await Task.Delay(1);

        if (GlobalManager.IsInGame)
            await InstantiateEssentials();
        if (GlobalManager.IsInGame && !GameManager.IsNewGame)
            await SaveSystem.LoadProfile();
        await GlobalManager.Utils.InvokeAwaitableEventListeners(ref OnPostLoad, _args, true);
        await Task.Delay(1); // game sometimes hangs if not awaited, probably something related to deferred calls

        if (_isARoomTransition) 
            PlayPlayerTransition(_goingIn: false, _roomData);
        else if (GlobalManager.IsInGame)
            await GlobalManager.Utils.InvokeAwaitableEventListeners(ref OnGameInit, _args);

        GlobalManager.IsBusy = false;
        DebugLogger.Print(DebugLogger.LogPriority.Info, $"SceneManager: <{_sceneToLoad}> has been entered.");

        await _loadScreen.RunOUT(); // await the loading scene before letting the player free
        if (_isARoomTransition)
            Player.Instance.SetDisabled(false);
        IsBusy = false;
    }

    public static async Task<LoadScreen> InstantiateLoadingScreen(string uid)
    {
        LoadScreen _loadScreen = (await GlobalManager.PRELOAD_RESOURCE(uid, false) as PackedScene).Instantiate() as LoadScreen;
        Instance.GetTree().Root.AddChild(_loadScreen);
        return _loadScreen;
    }
    private static async Task InstantiateEssentials()
    {
        Node2D _player = (await GlobalManager.PRELOAD_RESOURCE(GlobalManager.PLAYER_PREFAB, true) as PackedScene).Instantiate() as Node2D;
        CanvasLayer _canvas = (await GlobalManager.PRELOAD_RESOURCE(GlobalManager.CANVAS_PREFAB, true) as PackedScene).Instantiate() as CanvasLayer;

        Instance.GetTree().CurrentScene.AddChild(_player);
        Instance.GetTree().CurrentScene.AddChild(_canvas);

        if (!IsSwitching) // only disable if we came from a room transition, not from the main menu
            Player.Instance.SetDisabled(true);
    }
    private static void PlayPlayerTransition(bool _goingIn, RoomData _roomData)
    {
        if (_goingIn)
        {
            Player.Instance.LastPosition = _roomData.Position + (-_roomData.EntryDirection * 20);
            Player.Instance.SetDisabled(true, true); // the player will be freed along with this call, so no need to reenable it again later
            Player.Instance.SetMovementDirection(_roomData.EntryDirection);
        }
        else
        {
            if (FieldManager.Instance.Doors.Length <= _roomData.EntryIndex)
            {
                DebugLogger.Print(DebugLogger.LogPriority.Error, DebugLogger.GameTermination.Complete, "CRITICAL ERROR ON SCENE MANAGER. DOOR INDEX DOES NOT EXIST IN THIS ROOM.");
                return;
            }
            RoomDoor _newDoor = FieldManager.Instance.Doors[_roomData.EntryIndex];

            _newDoor.comingThrough = true;
            Player.Instance.GlobalPosition = _newDoor.GlobalPosition;
            Player.Instance.SetMovementDirection(_roomData.EntryDirection);
            CameraManager.ForceCameraPosition(Player.Instance.GlobalPosition);
        }
    }
}