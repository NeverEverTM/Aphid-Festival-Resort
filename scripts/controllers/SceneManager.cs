using System;
using System.Threading.Tasks;
using Godot;

public partial class SceneManager : Node2D
{
    public static SceneManager Instance { get; private set; }
    public static string Current { get; private set; }
    /// <summary>
    /// Wheter is going in or out of a game.
    /// </summary>
    public static bool Switching { get; private set; }
    /// <summary>
    /// Wheter a scene is currently being loaded.
    /// </summary>
    public static bool IsBusy { get; private set; }

    public delegate void RoomEvent(string _current = "", bool _is_switching = false);
    public delegate void GameStateEvent();
    public static event RoomEvent OnPreLoad, OnPostLoad, OnGameInit, OnGameFinish;

    public struct RoomData(string Name, int EntryIndex, Vector2 EntryDirection, Vector2 Position)
    {
        public string Name { get; set; } = Name;
        public int EntryIndex { get; set; } = EntryIndex;
        public Vector2 EntryDirection { get; set; } = EntryDirection;
        public Vector2 Position { get; set; } = Position;
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
    public static async Task Switch(string _sceneToLoad, bool _cache, bool _isInGame)
    {
        if (GlobalManager.IsBusy || IsBusy)
        {
            Logger.Print(Logger.LogPriority.Warning, $"SceneManager: Attempted to switch state while busy. Global: {GlobalManager.IsBusy}. Local: {IsBusy}");
            return;
        }
        // acknowledge the transition between "playing in a game" and "not in a game"
        if (_isInGame != GlobalManager.IsInGame)
        {
            GlobalManager.IsInGame = _isInGame;
            Switching = _isInGame;
        }
        SoundManager.StopSong();
        // load scene with special properties for game transition
        await Load(_sceneToLoad, new(), GlobalManager.LEAF_LOADING_SCENE, _cache);
        Switching = false;
    }
    /// <summary>
    /// Load a packed scene from a path as the root node, this includes basic room transitoning to game state transitioning.
    /// </summary>
    /// <param name="_sceneToLoad">Name of the scene to load.</param>
    /// <param name="_roomData">Door of the next room you want to access.</param>
    /// <param name="_loadingScreen">Path to the LoadScreen to instantiate and use.</param>
    /// <param name="_cache">Allows to cache the PackedScene to load it faster next time.</param>
    public static async Task Load(string _sceneToLoad, RoomData _roomData,
           string _loadingScreen = GlobalManager.FADE_LOADING_SCENE, bool _cache = false)
    {
        if (GlobalManager.IsBusy || IsBusy)
        {
            Logger.Print(Logger.LogPriority.Warning, $"SceneManager: Attempted to load scene while busy. Global: {GlobalManager.IsBusy}. Local: {IsBusy}");
            return;
        }
        GlobalManager.IsBusy = IsBusy = true;
        string _path = GlobalManager.ABSOLUTE_ROOMS_PATH + _sceneToLoad + ".tscn";

        if (!ResourceLoader.Exists(_path))
        {
            GlobalManager.IsBusy = false;
            Logger.Print(Logger.LogPriority.Error, $"SceneManager: <{_sceneToLoad}> does not exist.");
            return;
        }

        // start the load screen, but dont await for it
        Logger.Print(Logger.LogPriority.Info, $"SceneManager: Loading scene <{_sceneToLoad}>.");
        LoadScreen _loading = (await GlobalManager.PRELOAD_RESOURCE(_loadingScreen, false) as PackedScene).Instantiate() as LoadScreen;
        Instance.GetTree().Root.AddChild(_loading);
        _ = _loading.Start();

        // room transition animation
        if (GlobalManager.IsInGame && !Switching)
        {
            Player.Instance.LastPosition = _roomData.Position + (-_roomData.EntryDirection * 20);
            Player.Instance.SetDisabled(true, true); // this call will be freed along the player, so no need to disable it
            Player.Instance.SetMovementDirection(_roomData.EntryDirection);
        }

        // preload resources
        PackedScene _scene = await GlobalManager.PRELOAD_RESOURCE(_path, false) as PackedScene;
        while (!_loading.IsDone)
            await Task.Delay(1);
        OnPreLoad?.Invoke(Current, Switching);

        // dispose of scene elements and save profile data if applicable
        // cannot save if we are not in game/we are switching states or is a new game
        if (GlobalManager.IsInGame && !Switching && !GameManager.IsNewGame)
            await SaveSystem.SaveProfile();
        SoundManager.CleanAllSounds();
        GlobalManager.CleanAllParticles();

        // room setup
        Current = _sceneToLoad;
        bool _waitingOnCallback = true;
        Callable _sceneLoad = Callable.From(() =>
        {
            Instance.GetTree().ChangeSceneToPacked(_scene);
            _waitingOnCallback = false;
        });
        _sceneLoad.CallDeferred();
        while (_waitingOnCallback)
            await Task.Delay(1);

        // create player object if in game
        if (GlobalManager.IsInGame)
        {
            Node2D _player = (await GlobalManager.PRELOAD_RESOURCE(GlobalManager.PLAYER_PREFAB, true) as PackedScene).Instantiate() as Node2D;
            CanvasLayer _canvas = (await GlobalManager.PRELOAD_RESOURCE(GlobalManager.CANVAS_PREFAB, true) as PackedScene).Instantiate() as CanvasLayer;

            Instance.GetTree().CurrentScene.AddChild(_player);
            Instance.GetTree().CurrentScene.AddChild(_canvas);
            if (!Switching)
                Player.Instance.SetDisabled(true);
        }

        // postload
        if (GlobalManager.IsInGame && !GameManager.IsNewGame)
            await SaveSystem.LoadProfile();
        OnPostLoad?.Invoke(Current, Switching);
        await Task.Delay(1);

        if (Switching) // invoke game-relevant events when going in and out of game states
        {
            if (GlobalManager.IsInGame)
                OnGameInit?.Invoke(Current, Switching);
            else
                OnGameFinish?.Invoke(Current, Switching);
        }
        else if (GlobalManager.IsInGame) // otherwise, go through door exit transition
        {
            if (FieldManager.Instance.Doors.Length <= _roomData.EntryIndex)
            {
                Logger.Print(Logger.LogPriority.Error, Logger.GameTermination.Complete, "CRITICAL ERROR ON SCENE MANAGER. DOOR INDEX DOES NOT EXIST IN THIS ROOM.");
                return;
            }
            RoomDoor _newDoor = FieldManager.Instance.Doors[_roomData.EntryIndex];

            _newDoor.comingThrough = true;
            Player.Instance.GlobalPosition = _newDoor.GlobalPosition;
            Player.Instance.SetMovementDirection(_roomData.EntryDirection);
            CameraManager.ForceCameraPosition(Player.Instance.GlobalPosition);
        }

        GlobalManager.IsBusy = false;
        Logger.Print(Logger.LogPriority.Info, $"SceneManager: <{_sceneToLoad}> has been entered.");
        await _loading.Finish();
        if (GlobalManager.IsInGame && !Switching)
            Player.Instance.SetDisabled(false);
        IsBusy = false;
    }
}
