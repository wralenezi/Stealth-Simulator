using UnityEngine;

public interface IState
{
    void Enter();
    void Execute();
    void Exit();
}

// State class
public class StateMachine
{
    private IState m_CurrentState;

    // Time spend in the current state
    private float m_TimeElapsed;

    public IState GetState()
    {
        return m_CurrentState;
    }

    public void ChangeState(IState newState)
    {
        m_CurrentState?.Exit();
        m_CurrentState = newState;
        m_TimeElapsed = 0f;
        m_CurrentState.Enter();

        // if (GetState() is Patrol || GetState() is Chase || GetState() is Search)
        //     WorldState.Set("guardState", GetState().ToString());
    }

    public void UpdateState()
    {
        m_CurrentState?.Execute();
        m_TimeElapsed += Time.deltaTime;
    }
}