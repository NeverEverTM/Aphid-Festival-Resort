using System.Collections.Generic;
using Godot;

public static class SoundManager
{
	public static AudioStreamPlayer MusicPlayer = new(), SFXPlayer = new();
	public static AudioStreamPlayer2D SFXPlayer2D = new();

	internal readonly static List<AudioStreamPlayer> SoundEntities = new();
	internal readonly static List<AudioStreamPlayer2D> Sound2DEntities = new();

	public static void PlaySong(AudioStream _song)
	{
		if (MusicPlayer.Stream != null)
		{
			StopSong().Finished += () => PlaySong(_song);
			return;
		}
		MusicPlayer.Stream = _song;
		MusicPlayer.Play();
	}
	public static Tween StopSong()
	{
		Tween tween = MusicPlayer.CreateTween();
		tween.SetEase(Tween.EaseType.Out);
		tween.TweenProperty(MusicPlayer, "volume_db", -80, 1.0);
		tween.Finished += () =>
		{
			MusicPlayer.Stop();
			MusicPlayer.VolumeDb = -20;
			MusicPlayer.Stream = null;
		};
		return tween;
	}

	/// <summary>
	/// Creates a global sound to be heard, duplicating the given audioplayer and handed back for further manipulation.
	/// To create a sound that can only be heard positionally in 2D, use CreateSound2D instead.
	/// </summary>
	/// <param name="_stream">The audio to be streamed</param>
	/// <param name="_audioplayer">The base audio player from which duplicate</param>
	/// <param name="_pitchRand">Should this sound have a slight randomized pitch?</param>
	/// <returns>The new audioplayer duplicated from the base</returns>
	public static AudioStreamPlayer CreateSound(AudioStream _stream, AudioStreamPlayer _audioplayer, bool _pitchRand = false)
	{
		AudioStreamPlayer _player = _audioplayer.Duplicate() as AudioStreamPlayer;
		GameManager.Instance.AddChild(_player);
		if (_pitchRand)
			_player.PitchScale = GameManager.RNG.RandfRange(0.81f, 1.27f);
		_player.Stream = _stream;
		_player.Play();
		SoundEntities.Add(_player);
		return _audioplayer;
	}
	/// <summary>
	/// Creates a global sound to be heard, handed back for further manipulation.
	/// To create a sound that can only be heard positionally in 2D, use CreateSound2D instead.
	/// </summary>
	/// <param name="_stream">The audio to be streamed</param>
	/// <param name="_pitchRand">Should this sound have a slight randomized pitch?</param>
	/// <returns>The audioplayer created for the sound</returns>
	public static AudioStreamPlayer CreateSound(AudioStream _stream, bool _pitchRand) =>
		CreateSound(_stream, SFXPlayer.Duplicate() as AudioStreamPlayer, _pitchRand);

	/// <summary>
	/// Creates a sound at a given location that can be heard positionally. Duplicating the given audioplayer as a base.
	/// For audio that needs to be heard globally, use CreateSound instead.
	/// </summary>
	/// <param name="_stream">The audio to be streamed</param>
	/// <param name="_audioplayer">The base audio player from which duplicate</param>
	/// <param name="_position">The position at which spawn the sound</param>
	/// <param name="_pitchRand">Should this sound have a slight randomized pitch?</param>
	/// <returns>The new audioplayer duplicated from the base</returns>
	public static AudioStreamPlayer2D CreateSound2D(AudioStream _stream, AudioStreamPlayer2D _audioplayer, Vector2 _position, bool _pitchRand = false)
	{
		AudioStreamPlayer2D _player = _audioplayer.Duplicate() as AudioStreamPlayer2D;
		GameManager.Instance.AddChild(_player);
		if (_pitchRand)
			_player.PitchScale = GameManager.RNG.RandfRange(0.81f, 1.27f);
		_player.Stream = _stream;
		_player.GlobalPosition = _position;
		_player.Play();
		Sound2DEntities.Add(_player);
		return _player;
	}
	/// <summary>
	/// Creates a sound at a given location that can be heard positionally.
	/// For audio that needs to be heard globally, use CreateSound instead.
	/// </summary>
	/// <param name="_stream">The audio to be streamed</param>
	/// <param name="_position">The position at which spawn the sound</param>
	/// <param name="_pitchRand">Should this sound have a slight randomized pitch?</param>
	/// <returns>The new audioplayer created for the sound</returns>
	public static AudioStreamPlayer2D CreateSound2D(AudioStream _stream, Vector2 _position, bool _pitchRand) =>
		CreateSound2D(_stream, SFXPlayer2D.Duplicate() as AudioStreamPlayer2D, _position, _pitchRand);
}
