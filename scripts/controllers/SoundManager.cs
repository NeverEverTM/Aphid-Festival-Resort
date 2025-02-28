using System.Collections.Generic;
using Godot;

public partial class SoundManager : Node
{
	public static SoundManager Instance { get; private set; }
	public static AudioStreamPlayer MusicPlayer { get; private set; }
	public static AudioStreamPlayer SFXPlayer { get; private set; }
	public static AudioStreamPlayer2D SFXPlayer2D { get; private set; }
	public static AudioBusLayout AudioBus { get; private set; }

	private static readonly List<AudioStreamPlayer> sound_entities = new();
	private static readonly List<AudioStreamPlayer2D> sound2d_entities = new();
	private static Tween transitionTween;

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

	public static void ResumeSong()
	{
		if (IsInstanceValid(transitionTween))
			transitionTween.Kill();

		MusicPlayer.StreamPaused = false;
		transitionTween = MusicPlayer.CreateTween();
		transitionTween.SetEase(Tween.EaseType.In);
		transitionTween.TweenProperty(MusicPlayer, "volume_db", -0, 1.0);
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
	/// <param name="_full_name">Must be relative file name path to SFX (ex. "misc/title.wav")</param>
	public static void PlaySong(string _full_name)
	{
		AudioStream _music = ResourceLoader.Load<AudioStream>(GlobalManager.RES_SFX_PATH + _full_name);
		MusicPlayer.Stream = _music;
		MusicPlayer.Play();
	}
	public static void PauseSong()
	{
		if (IsInstanceValid(transitionTween))
			transitionTween.Kill();

		transitionTween = MusicPlayer.CreateTween();
		transitionTween.SetEase(Tween.EaseType.Out);
		transitionTween.TweenProperty(MusicPlayer, "volume_db", -80, 1.0);
		transitionTween.Finished += () => MusicPlayer.StreamPaused = true;
	}

	public static AudioStream GetAudioStream(string _name)
	{
		if (GlobalManager.G_AUDIO.ContainsKey(_name))
			return GlobalManager.G_AUDIO[_name];
		else
			return GlobalManager.G_AUDIO["button_fail"];
	}
	/// <summary>
	/// Creates a global sound to be heard, duplicating the given audioplayer and handed back for further manipulation.
	/// To create a sound that can only be heard positionally in 2D, use CreateSound2D instead.
	/// </summary>
	/// <param name="_audioplayer">The base audio player from which duplicate</param>
	public static AudioStreamPlayer CreateSound(AudioStream _stream, AudioStreamPlayer _audioplayer, bool _pitchRand = true)
	{
		AudioStreamPlayer _player = _audioplayer.GetParent() != null ? _audioplayer.Duplicate() as AudioStreamPlayer : _audioplayer;
		GlobalManager.Instance.AddChild(_player);
		if (_pitchRand)
			_player.PitchScale += GlobalManager.RNG.RandfRange(-0.15f, 0.15f);
		_player.Stream = _stream;
		_player.Play();
		sound_entities.Add(_player);
		return _audioplayer;
	}
	/// <summary>
	/// Creates a global sound to be heard, handed back for further manipulation.
	/// To create a sound that can only be heard positionally in 2D, use CreateSound2D instead.
	/// </summary>
	public static AudioStreamPlayer CreateSound(AudioStream _stream, bool _pitchRand = true, string _bus = "UI")
	{
        AudioStreamPlayer _player = new()
        {
            Bus = _bus
        };
        return CreateSound(_stream, _player, _pitchRand);
	}
	/// <summary>
	/// Creates a global sound to be heard, handed back for further manipulation.
	/// To create a sound that can only be heard positionally in 2D, use CreateSound2D instead.
	/// </summary>
	public static AudioStreamPlayer CreateSound(string _name, bool _pitchRand = true, string _bus = "UI")
	{
        AudioStreamPlayer _player = new()
        {
            Bus = _bus
        };
        return CreateSound(GetAudioStream(_name), _player, _pitchRand);
	}

	/// <summary>
	/// Creates a sound at a given location that can be heard positionally. Duplicating the given audioplayer as a base.
	/// For audio that needs to be heard globally, use CreateSound instead.
	/// </summary>
	public static AudioStreamPlayer2D CreateSound2D(AudioStream _stream, AudioStreamPlayer2D _audioplayer, Vector2 _position, bool _pitchRand = true)
	{
		AudioStreamPlayer2D _player = _audioplayer.GetParent() != null ? _audioplayer.Duplicate() as AudioStreamPlayer2D : _audioplayer;
		GlobalManager.Instance.AddChild(_player);
		if (_pitchRand)
			_player.PitchScale += GlobalManager.RNG.RandfRange(-0.15f,0.15f);
		_player.Attenuation = 0.5f;
		_player.MaxDistance = 500;
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
	public static AudioStreamPlayer2D CreateSound2D(AudioStream _stream, Vector2 _position, bool _pitchRand = true, string _bus = "Sounds")
	{
		AudioStreamPlayer2D _player = new()
		{
			Bus = _bus
		};
		return CreateSound2D(_stream, _player, _position, _pitchRand);
	}
	/// <summary>
	/// Creates a sound at a given location that can be heard positionally.
	/// For audio that needs to be heard globally, use CreateSound instead.
	/// </summary>
	public static AudioStreamPlayer2D CreateSound2D(string _name, Vector2 _position, bool _pitchRand = true, string _bus = "Sounds")
	{
		AudioStreamPlayer2D _player = new()
		{
			Bus = _bus
		};
		return CreateSound2D(GetAudioStream(_name), _player, _position, _pitchRand);
	}
}
