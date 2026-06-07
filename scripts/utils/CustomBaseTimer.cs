using System;
using System.Collections.Generic;
using Godot;

public abstract class CustomBaseTimer<E>
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
    public bool IsPaused { get; protected set; } = false;
    /// <summary>
    /// If the timer was cut short.
    /// </summary>
    public bool IsStopped { get; protected set; } = false;
    /// <summary>
    /// If the timer has succesfully finished. Only set if the timer was a one shot.
    /// </summary>
    public bool IsFinished { get; protected set; } = false;

    protected readonly E TInstance;

    public abstract void Finish();
    
    // ===============| Base Methods |==================
    /// <summary>
    /// Creates a timer instance, you must make sure to assign both BaseTime and TimeLeft manually, the latter can be set using GetTimerLeft()
    /// </summary>
    /// <param name="TInstance">The object instance to which the timer has access to locally.</param>
    public CustomBaseTimer(E TInstance)
    {
        this.TInstance = TInstance;
    }
    public CustomBaseTimer(E Instance, float BaseTime, float TimeLeft = -1, bool OneShot = false, bool autostart = true)
    {
        this.BaseTime = BaseTime;
        this.TimeLeft = TimeLeft < 0 ? GetTimerTime() : TimeLeft;
        this.OneShot = OneShot;
        IsStopped = !autostart;
        TInstance = Instance;
    }
    public CustomBaseTimer(E Entity, float BaseTime, bool OneShot = false, bool autostart = true)
    {
        this.BaseTime = BaseTime;
        TimeLeft = BaseTime;
        this.OneShot = OneShot;
        IsStopped = !autostart;
        TInstance = Entity;
    }

    /// <summary>
    /// Process method, must be called in order to advance a timer.
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="_timePassed"></param>
    public virtual void Update(float _timePassed)
    {
        if (!CanUpdate())
            return;

        if (TimeLeft > 0)
            TimeLeft -= _timePassed;
        else
        {
            if (!CanFinish())
                return;

            if (OneShot)
            {
                TimeLeft = 0;
                IsFinished = true;
            }
            else
                TimeLeft = GetTimerTime();
            Finish();
        }
    }
    public virtual bool CanUpdate()
    {
        if (IsStopped || IsPaused || IsFinished)
            return false;
        else
            return true;
    }
    /// <summary>
    /// Resets a timer and resumes it if stopped or finished. Paused timers will only have their values reset, and they will not unpause.
    /// </summary>
    /// <param name="_baseTime">Base value of timer to which it resets</param>
    /// <param name="_timeLeft">Predetermined time left. Defaults to given base time.</param>
    public virtual void Start(float BaseTime = -1, float TimeLeft = -1)
    {
        this.BaseTime = BaseTime < 0 ? this.BaseTime : BaseTime;
        this.TimeLeft = TimeLeft < 0 ? GetTimerTime() : TimeLeft;
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
    /// Checks if the finish action can be executed now. Override this method to add custom requirements, ex. to avoid execution until it is in a valid state to do the finish action.
    /// </summary>
    /// <returns></returns>
    public virtual bool CanFinish()
    {
        return true;
    }
}

public class CustomTimer<E>(E Entity, float BaseTime, bool OneShot = false, bool autostart = true) : CustomBaseTimer<E>(Entity, BaseTime, OneShot, autostart)
{
    public readonly List<Action<E>> OnFinish = [];

    public override void Finish()
    {
        for (int i = 0; i < OnFinish.Count; i++)
        {
            try
            {
                OnFinish[i](TInstance);
            }
            catch(Exception _error)
            {
                GD.PrintErr(_error);
            }
        }
    }
}