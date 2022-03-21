// Guard search state (after losing sight of all intruders)

using System;

[Serializable]
public class Search : State
{
    public override void MakeState(GuardsBehaviorController gc, IntrudersBehaviorController ic)
    {
        base.MakeState(gc, ic);
        name = "Search";
    }
    
    public override void Enter()
    {
        m_GC.StartSearch(NpcsManager.Instance.GetIntruders()[0]);
        m_IC.StartHiding();
    }

    public override void Execute(GameType gameType)
    {
        m_GC.Search(NpcsManager.Instance.GetIntruders()[0]);
        m_IC.KeepHiding();
    }

    public override void Exit()
    {
        m_GC.ClearSearch();
    }
    
}