using System;
using UnityEngine;

[Serializable]
public abstract class State //: IState
{
    protected GuardsBehaviorController m_GC;

    protected IntrudersBehaviorController m_IC;

    public string name { get; protected set; }

    public virtual void MakeState(GuardsBehaviorController guardsController,
        IntrudersBehaviorController intrudersController)
    {
        m_GC = guardsController;
        m_IC = intrudersController;
    }

    public virtual void Enter()
    {
    }

    public virtual void Execute()
    {
    }

    public virtual void Exit()
    {
    }
}