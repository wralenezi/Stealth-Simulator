// The Intruder escaped from guards and they are searching for him 
public class Hide : IState
{
    private Intruder m_Intruder;

    public Hide(Intruder _intruder)
    {
        m_Intruder = _intruder;
    }

    public void Enter()
    {
    }

    public void Execute()
    {
        m_Intruder.Hide();
    }

    public void Exit()
    {
        //m_Intruder.ClearGoal();
    }
}