using UnityEngine;

public interface IState
{
    public string name { get; }
    
    void Enter();
    void Execute();
    void Exit();
}

// State class
public class StateMachine
{
    private IState m_CurrentState;

    public IState GetState()
    {
        return m_CurrentState;
    }

    public void ChangeState(State newState)
    {
        WorldState.Set("last" + m_CurrentState?.name + "TimeEnd", StealthArea.GetElapsedTime().ToString());
        m_CurrentState?.Exit();

        m_CurrentState = newState;

        m_CurrentState.Enter();
        WorldState.Set("last" + m_CurrentState?.name + "TimeStart", StealthArea.GetElapsedTime().ToString());
        WorldState.Set("last" + m_CurrentState?.name + "TimeEnd", WorldState.EMPTY_VALUE);
        WorldState.Set("guard_state", GetState().ToString());
    }

    public void UpdateState()
    {
        m_CurrentState?.Execute();
    }
}