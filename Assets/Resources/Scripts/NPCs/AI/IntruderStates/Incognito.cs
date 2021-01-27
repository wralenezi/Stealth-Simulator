// Intruder is never seen by the guards

public class Incognito : IState
{
    private Intruder m_Intruder;

    public Incognito(Intruder _intruder)
    {
        m_Intruder = _intruder;
    }

    public void Enter()
    {
    }

    public void Execute()
    {
        m_Intruder.Incognito();
    }

    public void Exit()
    {
        m_Intruder.ClearGoal();
    }
}