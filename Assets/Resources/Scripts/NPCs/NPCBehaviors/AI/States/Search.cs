// Guard search state (after losing sight of all intruders)

public class Search : IState
{
    private GuardsBehaviorController m_GrdsCtrl;

    private IntrudersBehaviorController m_IntrdrCtrl;

    public Search(GuardsBehaviorController _guardsManager, IntrudersBehaviorController intrdrCtrl)
    {
        m_GrdsCtrl = _guardsManager;
        m_IntrdrCtrl = intrdrCtrl;
    }

    public void Enter()
    {
        m_IntrdrCtrl.StartHiding();
    }

    public void Execute()
    {
        m_GrdsCtrl.Search();
        m_IntrdrCtrl.KeepHiding();
    }

    public void Exit()
    {
        m_GrdsCtrl.EndSearch();
    }
}