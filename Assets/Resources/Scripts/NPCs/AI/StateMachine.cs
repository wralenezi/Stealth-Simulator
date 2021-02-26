public interface IState
{
    void Enter();
    void Execute();
    void Exit();
}

// State class
public class StateMachine
{
    IState m_CurrentState;

    public IState GetState()
    {
        return m_CurrentState;
    }

    public void ChangeState(IState newState)
    {
        m_CurrentState?.Exit();
        m_CurrentState = newState;
        m_CurrentState.Enter();
    }

    public void UpdateState()
    {
        m_CurrentState?.Execute();
    }
}