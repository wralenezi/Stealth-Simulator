// Guard chase state
public class Chase : IState
{
    // Reference for the guards behavior controller; this controls how the guards behave in the states
    private GuardsBehaviorController m_GrdsCtrl;
    
    private IntrudersBehaviorController m_IntrdrCtrl;

    public Chase(GuardsBehaviorController _guardsManager,IntrudersBehaviorController intrdrCtrl)
    {
        m_GrdsCtrl = _guardsManager;
        m_IntrdrCtrl = intrdrCtrl;
    }

    public void Enter()
    {
    }

    public void Execute()
    {
        m_GrdsCtrl.Chase();
        m_IntrdrCtrl.KeepRunning();
    }

    public void Exit()
    {
        m_GrdsCtrl.ClearGoals();
    }
}