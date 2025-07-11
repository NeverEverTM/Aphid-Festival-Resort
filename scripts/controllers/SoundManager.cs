using System.Collections.Generic;
using Godot;

public partial class SoundManager : Node
{
	public static SoundManager Instance { get; private set; }
	public static AudioStreamPlayer MusicPlayer { get; private set; }
	public static AudioStreamPlayer SFXPlayer { get; private set; }
	public static AudioStreamPlayer2D SFXPlayer2D { get; private set; }
	public static AudioBusLayout AudioBus { get; private set; }

	private static readonly List<AudioStreamPlayer> sound_entities = [];
	private static readonly List<AudioStreamPlayer2D> sound2d_entities = [];
	private static Tween transitionTween;

	public override void _Ready()
	{
		Instance = this;
		MusicPlayer = new()
		{
			Bus = "Music"
		};
		SFXPlayer = new()
		{
			Bus = "UI"
		};
		SFXPlayer2D = new()
		{
			MaxDistance = 400,
			Attenuation = 2.0f,
			MaxPolyphony = 20,
			Bus = "Sounds"
		};

		AudioBus = ResourceLoader.Load<AudioBusLayout>("uid://lm4k6xpr7uhu");
		ProcessMode = ProcessModeEnum.Always;

		Instance.AddChild(MusicPlayer);
		MusicPlayer.Finished += () =>
		{
			if (GlobalManager.IsInGame)
				SetRandomSong();
		};
		SceneManager.OnPostLoad += (_scene, _switch) =>
		{
			if (_switch && _scene != "menu")
				SetRandomSong();
		};
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

	public static void SetRandomSong()
	{
		string _song;
		if (FieldManager.TimeOfDay == FieldManager.DayHours.Night)
			_song = "night_" + GlobalManager.RNG.RandiRange(0, 0);
		else
			_song = "day_" + GlobalManager.RNG.RandiRange(0, 1);

		PlaySong("music/" + _song);
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
	/// <param name="_file">Must be a relative file name path (ex. "misc/title.wav")</param>
	public static void PlaySong(string _file)
	{
		MusicPlayer.Stream = GetAudioStream(_file);
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

	// MARK: SOUND
	/// <summary>
	/// Fetches an AudioStream from the globally loaded sound effect dictionary.
	/// </summary>
	/// <param name="_key">Key to the audio stream. Key must be a relative path from the SFX folder, Ex. "ui/button_fail"</param>
	/// <returns>The audio stream requested. If key was invalid it will return the default fail sound.</returns>
	public static AudioStream GetAudioStream(string _key)
	{
		if (GlobalManager.G_AUDIO.TryGetValue(_key, out AudioStream value))
			return value;
		else
			return GlobalManager.G_AUDIO["ui/button_fail"];
	}
	/// <summary>
	/// <para>Creates a sound that will be heard globally.</para>
	/// <para>To create a sound that can only be heard positionally in 2D, use CreateSound2D instead.</para>
	/// </summary>
	/// <param name="_audioplayer">The base audio player from which duplicate</param>
	/// <returns>The duplicated player acting as the current sound source for the audio.</returns>
	public static AudioStreamPlayer CreateSound(AudioStream _stream, AudioStreamPlayer _audioplayer, bool _pitchRand = true)
	{
		AudioStreamPlayer _player = _audioplayer.Duplicate() as AudioStreamPlayer;
		_player.Stream = _stream;
		if (_pitchRand)
			_player.PitchScale += GlobalManager.RNG.RandfRange(-0.15f, 0.15f);

		GlobalManager.Instance.AddChild(_player);
		sound_entities.Add(_player);
		_player.Play();
		return _player;
	}
	/// <summary>
	/// <para>Creates a sound that will be heard globally.</para>
	/// <para>To create a sound that can only be heard positionally in 2D, use CreateSound2D instead.</para>
	/// </summary>
	/// <returns>The duplicated player acting as the current sound source for the audio.</returns>
	public static AudioStreamPlayer CreateSound(AudioStream _stream, bool _pitchRand = true) =>
		CreateSound(_stream, SFXPlayer, _pitchRand);
	/// <summary>
	/// <para>Creates a sound that will be heard globally.</para>
	/// <para>To create a sound that can only be heard positionally in 2D, use CreateSound2D instead.</para>
	/// </summary>
	/// <returns>The duplicated player acting as the current sound source for the audio.</returns>
	public static AudioStreamPlayer CreateSound(string _name, bool _pitchRand = true) =>
		CreateSound(GetAudioStream(_name), SFXPlayer, _pitchRand);

	// MARK: 2D SOUND
	/// <summary>
	/// <para>Creates a sound that can be heard positionally at a given location.</para>
	///	<para>For a sound that needs to be heard globally, use CreateSound instead.</para>
	/// </summary>
	/// <returns>The duplicated player acting as the current sound source for this audio.</returns>
	public static AudioStreamPlayer2D CreateSound2D(AudioStream _stream, AudioStreamPlayer2D _audioplayer, Vector2 _position, bool _pitchRand = true)
	{
		AudioStreamPlayer2D _player = _audioplayer.Duplicate() as AudioStreamPlayer2D;
		_player.Stream = _stream;
		_player.GlobalPosition = _position;
		if (_pitchRand)
			_player.PitchScale += GlobalManager.RNG.RandfRange(-0.15f, 0.15f);

		GlobalManager.Instance.AddChild(_player);
		sound2d_entities.Add(_player);
		_player.Play();
		return _player;
	}
	/// <summary>
	/// <para>Creates a sound that can be heard positionally at a given location.</para>
	/// <para>For a sound that needs to be heard globally, use CreateSound instead.</para>
	/// </summary>
	/// <returns>The duplicated player acting as the current sound source for this audio.</returns>
	public static AudioStreamPlayer2D CreateSound2D(string _name, AudioStreamPlayer2D _player, Vector2 _position, bool _pitchRand = true) =>
		CreateSound2D(GetAudioStream(_name), _player, _position, _pitchRand);
	/// <summary>
	/// <para>Creates a sound that can be heard positionally at a given location.</para>
	/// <para>For a sound that needs to be heard globally, use CreateSound instead.</para>
	/// </summary>
	/// <returns>The duplicated player acting as the current sound source for this audio.</returns>
	public static AudioStreamPlayer2D CreateSound2D(AudioStream _stream, Vector2 _position, bool _pitchRand = true, string _bus = "Sounds") =>
		CreateSound2D(_stream, SFXPlayer2D, _position, _pitchRand);

	/// <summary>
	/// <para>Creates a sound that can be heard positionally at a given location.</para>
	/// <para>For a sound that needs to be heard globally, use CreateSound instead.</para>
	/// </summary>
	/// <returns>The duplicated player acting as the current sound source for this audio.</returns>
	public static AudioStreamPlayer2D CreateSound2D(string _name, Vector2 _position, bool _pitchRand = true, string _bus = "Sounds") =>
		CreateSound2D(GetAudioStream(_name), SFXPlayer2D, _position, _pitchRand);

}
