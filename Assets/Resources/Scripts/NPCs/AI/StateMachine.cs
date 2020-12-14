using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IState
{
    void Enter();
    void Execute();
    void Exit();
}

// State class
public class StateMachine
{
    IState m_CurrentState;

    public IState GetState()
    {
        return m_CurrentState;
    }

    public void ChangeState(IState newState)
    {
        m_CurrentState?.Exit();
        m_CurrentState = newState;
        m_CurrentState.Enter();
    }

    public void Update()
    {
        m_CurrentState?.Execute();
    }
}


// The states are describe the Guard manager since all guards are controlled by it. 

// Guard patrol state
public class Patrol : IState
{
    private GuardsManager m_GuardsManager;

    public Patrol(GuardsManager _guardsManager)
    {
        m_GuardsManager = _guardsManager;
    }

    public void Enter()
    {
        m_GuardsManager.UpdateGuiLabel();
    }

    public void Execute()
    {
        m_GuardsManager.Patrol();
    }

    public void Exit()
    {
        m_GuardsManager.ClearGoals();
    }
}


// Guard chase state
public class Chase : IState
{
    private GuardsManager m_GuardsManager;

    public Chase(GuardsManager _guardsManager)
    {
        m_GuardsManager = _guardsManager;
        m_GuardsManager.UpdateGuiLabel();
    }

    public void Enter()
    {
        m_GuardsManager.UpdateGuiLabel();
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


// States for the intruder

// Intruder is never seen by the guards
public class Incognito : IState
{
    private Intruder m_Intruder;

    public Incognito(Intruder _intruder)
    {
        m_Intruder = _intruder;
    }

    public void Enter()
    {
    }

    public void Execute()
    {
        m_Intruder.Incognito();
    }

    public void Exit()
    {
        m_Intruder.ClearGoal();
    }
}

// Intruder is being chased by the guards
public class Chased : IState
{
    private Intruder m_Intruder;

    public Chased(Intruder _intruder)
    {
        m_Intruder = _intruder;
    }

    public void Enter()
    {
    }

    public void Execute()
    {
        m_Intruder.RunAway();
    }

    public void Exit()
    {
        m_Intruder.ClearGoal();
    }
}

// The Intruder escaped from guards and they are searching for him 
public class Hidden : IState
{
    private Intruder m_Intruder;

    public Hidden(Intruder _intruder)
    {
        m_Intruder = _intruder;
    }

    public void Enter()
    {
    }

    public void Execute()
    {
        m_Intruder.Hide();
    }

    public void Exit()
    {
        m_Intruder.ClearGoal();
    }
}