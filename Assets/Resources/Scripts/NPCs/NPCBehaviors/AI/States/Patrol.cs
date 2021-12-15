// Guard patrol state

public class Patrol : IState
{
    private GuardsBehaviorController m_GrdsCtrl;

    private IntrudersBehaviorController m_IntrdrCtrl;

    public Patrol(GuardsBehaviorController _guardsManager, IntrudersBehaviorController intrdrCtrl)
    {
        m_GrdsCtrl = _guardsManager;
        m_IntrdrCtrl = intrdrCtrl;
    }

    public void Enter()
    {
        m_GrdsCtrl.StartShift();
    }

    public void Execute()
    {
        m_GrdsCtrl.Patrol();
        m_IntrdrCtrl.StayIncognito();
    }

    public void Exit()
    {
        m_GrdsCtrl.ClearGoals();
    }
}