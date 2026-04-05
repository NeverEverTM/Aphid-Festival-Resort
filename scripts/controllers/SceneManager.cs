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
    public static string CurrentScene { get; private set; } = "menu";
    /// <summary>
    /// Wheter is going into active gameplay or the main menu.
    /// </summary>
    public static bool IsSwitching { get; private set; }
    /// <summary>
    /// Wheter a scene is currently being loaded or not.
    /// </summary>
    public static bool IsBusy { get; private set; }
    public static bool CurrentlyInGame { get; internal set; } = false;
    private static bool WaitingOnDeferred = false;

    private static List<Action<SceneArgs>>
        OnPreLoad = [],
        OnGameFinish = [],
        OnGameInit = [],
        OnPostLoad = [];

    /// <summary>
    /// Events available for Scene Manager.
    /// <para>Events enums are listed by execution order.</para>
    /// </summary>
    public enum EventEnum
    {
        /// <summary>
        /// Fired before all scene load actions are taken. Event is cleared after this.
        /// </summary>
        OnPreLoad,
        /// <summary>
        /// Fired when exiting a savefile, after PreLoad but before the scene changes. Never disposed of.
        /// </summary>
        OnGameFinish,
        /// <summary>
        /// Fired when entering a savefile after the scene, canvas and player are loaded but before save data is loaded. Never disposed of.
        /// </summary>
        OnGameInit,
        /// <summary>
        /// Fired after all scene load actions are taken, including loading save data. Event is cleared after this.
        /// </summary>
        OnPostLoad,
    }

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
    /// Loads AND switches game state from out-game->in-game and viceversa.
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
        // acknowledge the transition between "playing in a game" and "not in a game" //<- what is this for?
        if (_isInGame != CurrentlyInGame)
        {
            CurrentlyInGame = _isInGame;
            IsSwitching = _isInGame;
        }
        
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

        // ==== | INITIALIZATION |====
        GlobalManager.IsBusy = IsBusy = true;
        bool _isARoomTransition = !IsSwitching && CurrentlyInGame;
        SceneArgs _args = new()
        {
            IsSwitching = IsSwitching,
            CurrentScene = CurrentScene,
            NextCurrentScene = _sceneToLoad,
            roomData = _roomData
        };
        DebugLogger.Print(DebugLogger.LogPriority.Info, $"SceneManager: Loading scene <{_sceneToLoad}>.");

        // ====| PRE SCENE LOAD |====
        LoadScreen _loadScreen = await InstantiateLoadingScreen(_loadingScreen);
        OnPostLoad.Clear(); // make sure no calls get dragged between rooms

        if (_isARoomTransition)
            PlayPlayerTransition(_goingIn: true, _roomData);

        await _loadScreen.RunIN();
        
        await GlobalManager.Utils.InvokeAwaitableEventListeners(OnPreLoad, _args, true);

        if (!_isARoomTransition)
            await GlobalManager.Utils.InvokeAwaitableEventListeners(OnGameFinish, _args);
    
        if (_isARoomTransition)
            await SaveSystem.SaveProfile(_autosave: true, _force: true);
        SoundManager.CleanAllSounds();
        GlobalManager.CleanAllParticles();
        SaveSystem.ClearSaveModules(_isARoomTransition ? SaveSystem.SaveMetadata.DisposeMethod.OnRoomTransition : SaveSystem.SaveMetadata.DisposeMethod.OnGameExit);

        // SCENE LOAD
        CurrentScene = _sceneToLoad;
        await InstantiateRootScene(_path);
        if (CurrentlyInGame)
        {
            await CanvasManager.INSTANTIATE_CANVAS();
            await Player.INSTANTIATE_PLAYER(_args);
        }

        // ====| POST SCENE LOAD |====
        if (_isARoomTransition)
            PlayPlayerTransition(_goingIn: false, _roomData);
        else if (CurrentlyInGame) // game start
            await GlobalManager.Utils.InvokeAwaitableEventListeners(OnGameInit, _args);

        if (CurrentlyInGame && !GameManager.IsANewSavefile)
            await SaveSystem.LoadProfile(_loadFully: !_isARoomTransition); // dont load everything again if we came from a room

        await GlobalManager.Utils.InvokeAwaitableEventListeners(OnPostLoad, _args, true);

        GlobalManager.IsBusy = false;
        DebugLogger.Print(DebugLogger.LogPriority.Info, $"SceneManager: <{_sceneToLoad}> has been entered.");

        await _loadScreen.RunOUT(); // await the loading scene before letting the player free
        if (_isARoomTransition)
            Player.Instance.SetDisabled(false);
        IsBusy = false;
    }

    private static async Task InstantiateRootScene(string _path)
    {
        try
        {
            PackedScene _scene = await GlobalManager.PRELOAD_RESOURCE(_path, true) as PackedScene;
            
            WaitingOnDeferred = true;
            Callable _sceneLoad = Callable.From(() =>
            {
                Instance.GetTree().ChangeSceneToPacked(_scene);
                WaitingOnDeferred = false;
            });
            _sceneLoad.CallDeferred();
            while (WaitingOnDeferred) // wait until the deferred call ends
                await Task.Delay(1);
        }
        catch(Exception _error)
        {
            DebugLogger.Print(DebugLogger.LogPriority.Error, DebugLogger.GameTermination.Complete, $"SceneManager: Unable to load room at <{_path}>", _error);
        }
        await Task.Delay(1); // game sometimes hangs if not awaited, probably something related to deferred calls
    }
    private static async Task<LoadScreen> InstantiateLoadingScreen(string uid)
    {
        LoadScreen _loadScreen = (await GlobalManager.PRELOAD_RESOURCE(uid, false) as PackedScene).Instantiate() as LoadScreen;
        Instance.GetTree().Root.AddChild(_loadScreen);
        return _loadScreen;
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
            if (RoomInstance.Instance.Doors.Length <= _roomData.EntryIndex)
            {
                DebugLogger.Print(DebugLogger.LogPriority.Error, DebugLogger.GameTermination.Complete, "CRITICAL ERROR ON SCENE MANAGER. DOOR INDEX DOES NOT EXIST IN THIS ROOM.");
                return;
            }
            RoomDoor _newDoor = RoomInstance.Instance.Doors[_roomData.EntryIndex];

            _newDoor.comingThrough = true;
            Player.Instance.GlobalPosition = _newDoor.GlobalPosition;
            Player.Instance.SetMovementDirection(_roomData.EntryDirection);
            CameraManager.ForceCameraPosition(Player.Instance.GlobalPosition);
        }
    }
}