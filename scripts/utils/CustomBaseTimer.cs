using System;
using System.Collections.Generic;
using Godot;

public abstract class CustomBaseTimer<T>
{
    public float TimeLeft { get; set; } = 1;
    /// <summary>
    /// Base time for the timer, GetTimerTime() does not affect this original value in anyway.
    /// </summary>
    public float BaseTime { get; set; } = 1;
    public bool OneShot { get; set; } = false;

    /// <summary>
    /// If the timer is still running but is currently paused.
    /// </summary>
    public bool IsPaused { get; private set; } = false;
    /// <summary>
    /// If the timer was cut short.
    /// </summary>
    public bool IsStopped { get; private set; } = false;
    /// <summary>
    /// If the timer has succesfully finished. Only set if the timer was a one shot.
    /// </summary>
    public bool IsFinished { get; private set; } = false;

    public abstract void Finish(T entity);
    
    // ===============| Base Methods |==================
    public CustomBaseTimer(float BaseTime, float TimeLeft = -1, bool OneShot = false, bool autostart = true)
    {
        this.BaseTime = BaseTime;
        this.TimeLeft = TimeLeft <= 0 ? GetTimerTime() : TimeLeft;
        this.OneShot = OneShot;
        IsStopped = !autostart;
    }
    public CustomBaseTimer(float BaseTime, bool OneShot = false, bool autostart = true)
    {
        this.BaseTime = BaseTime;
        this.OneShot = OneShot;
        IsStopped = !autostart;
    }

    /// <summary>
    /// Process method, must be called in order to advance a timer.
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="_delta"></param>
    public virtual void Process(T entity, float _delta)
    {
        if (IsStopped || IsPaused || IsFinished)
            return;

        if (TimeLeft > 0)
            TimeLeft -= _delta;
        else
        {
            if (!CanFinish(entity))
                return;

            if (OneShot)
                IsFinished = true;
            else
                TimeLeft = GetTimerTime();
            Finish(entity);
        }
    }
    /// <summary>
    /// Resets a timer and resumes it if stopped or finished. Paused timers will only have their values reset, and they will not unpause.
    /// </summary>
    /// <param name="_baseTime">Base value of timer to which it resets</param>
    /// <param name="_timeLeft">Predetermined time left. Defaults to given base time.</param>
    public virtual void Start(float BaseTime = -1, float TimeLeft = -1)
    {
        this.BaseTime = BaseTime <= 0 ? this.BaseTime : BaseTime;
        this.TimeLeft = TimeLeft <= 0 ? GetTimerTime() : TimeLeft;
        IsStopped = IsFinished = false;
    }
    /// <summary>
    /// Stops the timer and resets it back to full.
    /// </summary>
    public virtual void Stop()
    {
        TimeLeft = GetTimerTime();
        IsStopped = true;
    }
    /// <summary>
    /// Resumes a paused timer.
    /// </summary>
    public virtual void Resume()
    {
        IsPaused = false;
    }
    /// <summary>
    /// Pauses a timer. TimeLeft will be left untouched.
    /// </summary>
    public virtual void Pause()
    {
        IsPaused = true;
    }

    //  =========| Overridable Methods |==============
    /// <summary>
    /// Override this method to apply custom operations to the base time, ex. adding a slight random offset. This does not change BaseTime's original value.
    /// </summary>
    public virtual float GetTimerTime()
    {
        return BaseTime;
    }
    /// <summary>
    /// Checks if the finish action can be executed now. Override this method to add custom requirements, ex. timer should not finish if the result cannot be applied now.
    /// </summary>
    /// <returns></returns>
    public virtual bool CanFinish(T entity)
    {
        return true;
    }
}

public class CustomTimer<T>(float BaseTime, bool OneShot = false, bool autostart = true) : CustomBaseTimer<T>(BaseTime, OneShot, autostart)
{
    public readonly List<Action<T>> OnFinish = [];

    public override void Finish(T entity)
    {
        for (int i = 0; i < OnFinish.Count; i++)
        {
            try
            {
                OnFinish[i](entity);
            }
            catch(Exception _error)
            {
                GD.PrintErr(_error);
            }
        }
    }
}