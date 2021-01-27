// Guard search state (after losing sight of the intruder)
public class Search : IState
{
    private GuardsManager m_GuardsManager;

    public Search(GuardsManager _guardsManager)
    {
        m_GuardsManager = _guardsManager;
    }

    public void Enter()
    {
        m_GuardsManager.UpdateGuiLabel();
    }

    public void Execute()
    {
        m_GuardsManager.Search();
    }

    public void Exit()
    {
        m_GuardsManager.EndSearch();
    }
}