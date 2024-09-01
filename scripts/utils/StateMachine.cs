using System;

public class StateMachine
{
	public IState currentState;

	public void ChangeState(IState _newState, EventArgs args)
	{
		currentState?.ExitState(args);
		_newState.EnterState(currentState, args);
		currentState = _newState;
	}

	public bool IsState(string _state) =>
		currentState.Name.Equals(_state);

	public interface IState
	{
		public string Name { get; }
		public void EnterState(IState _previous, EventArgs e);
		public void UpdateState(EventArgs e);
		public void ExitState(EventArgs e);
	}
}