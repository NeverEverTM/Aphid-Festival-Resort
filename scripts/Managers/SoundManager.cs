using System.Collections.Generic;
using Godot;

public partial class SoundManager : Node
{
	private static SoundManager Instance;
	public static AudioStreamPlayer MusicPlayer, SFXPlayer;
	public static AudioStreamPlayer2D SFXPlayer2D;
	public static AudioBusLayout AudioBus;

	private static readonly List<AudioStreamPlayer> sound_entities = new();
	private static readonly List<AudioStreamPlayer2D> sound2d_entities = new();

	public override void _Ready()
	{
		Instance = this;
		MusicPlayer = new()
		{
			Bus = "Music"
		};
		SFXPlayer = new();
		SFXPlayer2D = new();
		AudioBus = ResourceLoader.Load<AudioBusLayout>("res://default_bus_layout.tres");
		ProcessMode = ProcessModeEnum.Always;

		Instance.AddChild(MusicPlayer);
		MusicPlayer.Finished += () => MusicPlayer.Stream = null;
	}
	public override void _Process(double delta)
	{
		// Cleans sounds periodically once finished
		for (int i = 0; i < sound_entities.Count; i++)
		{
			if (sound_entities[i].Playing)
				continue;
			sound_entities[i].QueueFree();
			sound_entities.RemoveAt(i);
		}
		for (int i = 0; i < sound2d_entities.Count; i++)
		{
			if (sound2d_entities[i].Playing)
				continue;
			sound2d_entities[i].QueueFree();
			sound2d_entities.RemoveAt(i);
		}
	}

	public static void CleanAllSounds()
	{
		for (int i = 0; i < sound_entities.Count; i++)
			sound_entities[i].QueueFree();
		sound_entities.Clear();
		for (int i = 0; i < sound2d_entities.Count; i++)
			sound2d_entities[i].QueueFree();
		sound2d_entities.Clear();
	}

	public static Tween ResumeSong()
	{
		MusicPlayer.StreamPaused = false;
		Tween tween = MusicPlayer.CreateTween();
		tween.SetEase(Tween.EaseType.In);
		tween.TweenProperty(MusicPlayer, "volume_db", -0, 1.0);
		return tween;
	}
	public static Tween StopSong()
	{
		Tween tween = MusicPlayer.CreateTween();
		tween.SetEase(Tween.EaseType.Out);
		tween.TweenProperty(MusicPlayer, "volume_db", -80, 1.0);
		tween.Finished += () =>
		{
			MusicPlayer.Stop();
			MusicPlayer.VolumeDb = 0;
			MusicPlayer.Stream = null;
		};
		return tween;
	}
	public static void PlaySong(string _name)
	{
		AudioStream _music = ResourceLoader.Load<AudioStream>($"{GameManager.MusicPath}/{_name}");
		MusicPlayer.Stream = _music;
		MusicPlayer.Play();
	}
	public static Tween PauseSong()
	{
		Tween tween = MusicPlayer.CreateTween();
		tween.SetEase(Tween.EaseType.Out);
		tween.TweenProperty(MusicPlayer, "volume_db", -80, 1.0);
		tween.Finished += () => MusicPlayer.StreamPaused = true;

		return tween;
	}

	/// <summary>
	/// Creates a global sound to be heard, duplicating the given audioplayer and handed back for further manipulation.
	/// To create a sound that can only be heard positionally in 2D, use CreateSound2D instead.
	/// </summary>
	/// <param name="_audioplayer">The base audio player from which duplicate</param>
	public static AudioStreamPlayer CreateSound(AudioStream _stream, AudioStreamPlayer _audioplayer, bool _pitchRand = true)
	{
		AudioStreamPlayer _player = _audioplayer.Duplicate() as AudioStreamPlayer;
		GameManager.Instance.AddChild(_player);
		if (_pitchRand)
			_player.PitchScale = GameManager.RNG.RandfRange(0.81f, 1.27f);
		_player.Stream = _stream;
		_player.Bus = "Sounds";
		_player.Play();
		sound_entities.Add(_player);
		return _audioplayer;
	}
	/// <summary>
	/// Creates a global sound to be heard, handed back for further manipulation.
	/// To create a sound that can only be heard positionally in 2D, use CreateSound2D instead.
	/// </summary>
	public static AudioStreamPlayer CreateSound(AudioStream _stream, bool _pitchRand = true) =>
		CreateSound(_stream, SFXPlayer.Duplicate() as AudioStreamPlayer, _pitchRand);

	/// <summary>
	/// Creates a sound at a given location that can be heard positionally. Duplicating the given audioplayer as a base.
	/// For audio that needs to be heard globally, use CreateSound instead.
	/// </summary>
	public static AudioStreamPlayer2D CreateSound2D(AudioStream _stream, AudioStreamPlayer2D _audioplayer, Vector2 _position, bool _pitchRand = true)
	{
		AudioStreamPlayer2D _player = _audioplayer.Duplicate() as AudioStreamPlayer2D;
		GameManager.Instance.AddChild(_player);
		if (_pitchRand)
			_player.PitchScale = GameManager.RNG.RandfRange(0.81f, 1.27f);
		_player.Stream = _stream;
		_player.GlobalPosition = _position;
		_player.Play();
		sound2d_entities.Add(_player);
		return _player;
	}
	/// <summary>
	/// Creates a sound at a given location that can be heard positionally.
	/// For audio that needs to be heard globally, use CreateSound instead.
	/// </summary>
	public static AudioStreamPlayer2D CreateSound2D(AudioStream _stream, Vector2 _position, bool _pitchRand = true) =>
		CreateSound2D(_stream, SFXPlayer2D.Duplicate() as AudioStreamPlayer2D, _position, _pitchRand);
}
