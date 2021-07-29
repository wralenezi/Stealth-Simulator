// Guard chase state
public class Chase : IState
{
    private GuardsManager m_GuardsManager;

    public Chase(GuardsManager _guardsManager)
    {
        m_GuardsManager = _guardsManager;
    }

    public void Enter()
    {
    }

    public void Execute()
    {
        m_GuardsManager.Chase();
    }

    public void Exit()
    {
        m_GuardsManager.ClearGoals();
    }
}