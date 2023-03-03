using System;
using UnityEngine;

// State class
[Serializable]
public class StateMachine
{
    [SerializeField] private string m_name;
    
    public State m_CurrentState;

    public State GetState()
    {
        return m_CurrentState;
    }

    public void ChangeState(State newState)
    {
        WorldState.Set("last" + m_CurrentState?.name + "TimeEnd", StealthArea.GetElapsedTimeInSeconds().ToString());
        m_CurrentState?.Exit();

        m_CurrentState = newState;

        m_name = m_CurrentState.name;
        m_CurrentState.Enter();
        WorldState.Set("last" + m_CurrentState?.name + "TimeStart", StealthArea.GetElapsedTimeInSeconds().ToString());
        WorldState.Set("last" + m_CurrentState?.name + "TimeEnd", WorldState.EMPTY_VALUE);
        WorldState.Set("guard_state", GetState().ToString());
    }

    public void UpdateState(GameType gameType, float deltaTime)
    {
        m_CurrentState?.Execute(gameType, deltaTime);
    }
}