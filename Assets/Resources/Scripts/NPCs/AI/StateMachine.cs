using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IState
{
    
    
    void Enter();
    void Execute();
    void Exit();
}


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


// Guard patrol state
public class Patrol : IState
{
    private Guard m_Guard;
    

    public Patrol(Guard _Guard)
    {
        m_Guard = _Guard;
    }

    public void Enter()
    {
    }

    public void Execute()
    {
        m_Guard.Patrol();
    }

    public void Exit()
    {
        m_Guard.ClearGoal();
    }
}


// Guard chase state
public class Chase : IState
{
    private Guard m_Guard;

    public Chase(Guard _Guard)
    {
        m_Guard = _Guard;
    }

    public void Enter()
    {
    }

    public void Execute()
    {
        m_Guard.Chase();
    }

    public void Exit()
    {
        m_Guard.ClearGoal();
    }
}

// Guard chase state
public class Search : IState
{
    private Guard m_Guard;

    public Search(Guard _Guard)
    {
        m_Guard = _Guard;
    }

    public void Enter()
    {
    }

    public void Execute()
    {
        m_Guard.Search();
    }

    public void Exit()
    {
        m_Guard.EndSearch();
    }
}
