using System;

// Guard chase state
[Serializable]
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

    public override void Execute(GameType gameType)
    {
        m_GC.Chase(NpcsManager.Instance.GetIntruders()[0]);
        m_IC.KeepRunning();
    }

    public override void Exit()
    {
        m_GC.ClearGoals();
    }
}