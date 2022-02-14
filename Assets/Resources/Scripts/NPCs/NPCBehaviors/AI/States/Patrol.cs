using System;

// Guard patrol state
[Serializable]
public class Patrol : State
{
    public override void MakeState(GuardsBehaviorController gc, IntrudersBehaviorController ic)
    {
        base.MakeState(gc, ic);
        name = "Patrol";
    }
    
    public override void Enter()
    {
        m_GC.StartShift();
        m_IC.StartScouter();
    }

    public override void Execute()
    {
        m_GC.Patrol();
        m_IC.StayIncognito();
    }

    public override void Exit()
    {
        m_GC.ClearGoals();
    }
}