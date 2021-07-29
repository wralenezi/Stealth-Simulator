// Guard patrol state
public class Patrol : IState
{
    private GuardsManager m_GuardsManager;

    public Patrol(GuardsManager _guardsManager)
    {
        m_GuardsManager = _guardsManager;
    }

    public void Enter()
    {
    }

    public void Execute()
    {
        m_GuardsManager.Patrol();
    }

    public void Exit()
    {
        m_GuardsManager.ClearGoals();
    }
}