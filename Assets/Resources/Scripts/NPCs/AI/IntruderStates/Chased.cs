// Intruder is being chased by the guards
public class Chased : IState
{
    private Intruder m_Intruder;

    public Chased(Intruder _intruder)
    {
        m_Intruder = _intruder;
    }

    public void Enter()
    {
    }

    public void Execute()
    {
        m_Intruder.RunAway();
    }

    public void Exit()
    {
       // m_Intruder.ClearGoal();
    }
}