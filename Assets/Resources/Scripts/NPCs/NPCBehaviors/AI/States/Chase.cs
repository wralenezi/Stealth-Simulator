// Guard chase state
public class Chase : State
{
    public override void MakeState(GuardsBehaviorController gc, IntrudersBehaviorController ic)
    {
        base.MakeState(gc, ic);
        name = "Chase";
    }
    
    public override void Enter()
    {
        m_GC.StartChase(NpcsManager.Instance.GetIntruders()[0]);
        m_IC.StartChaseEvader();
    }

    public override void Execute()
    {
        m_GC.Chase(NpcsManager.Instance.GetIntruders()[0]);
        m_IC.KeepRunning();
    }

    public override void Exit()
    {
        
    }
}