using System.Collections.Generic;
using UnityEngine;

public class IntrudersBehaviorController : MonoBehaviour
{
    private Behavior m_behavior;

    public Behavior behavior
    {
        get { return m_behavior; }
    }

    // The controller for intruders behavior when they have never been spotted.
    private Scouter m_Scouter;

    private ChaseEvader m_ChaseEvader;

    private SearchEvader m_SearchEvader;

    public void Initiate(Behavior _behavior, MapManager mapManager)
    {
        m_behavior = _behavior;

        switch (behavior.patrol)
        {
            case PatrolPlanner.iSimple:
                m_Scouter = gameObject.AddComponent<SimpleGreedyScouter>();
                break;
            
            case PatrolPlanner.iRoadMap:
                m_Scouter = gameObject.AddComponent<RoadMapScouter>();
                break;
            
        
            case PatrolPlanner.UserInput:
                break;
        }
        
        switch (behavior.alert)
        {
            case AlertPlanner.iHeuristic:
                m_ChaseEvader = gameObject.AddComponent<SimpleChaseEvader>();
                break;
        
            case AlertPlanner.UserInput:
                break;
        }
        
        switch (behavior.search)
        {
            case SearchPlanner.iHeuristic:
                m_SearchEvader = gameObject.AddComponent<SimpleSearchEvader>();
                break;
        
            case SearchPlanner.UserInput:
                return;
        }
        
        
        m_Scouter?.Initiate(mapManager);
        m_ChaseEvader?.Initiate(mapManager);
        m_SearchEvader?.Initiate(mapManager);
    }


    public void StartScouter()
    {
        if (Equals(behavior.patrol, PatrolPlanner.UserInput)) return;

        m_Scouter.Begin();
    }

    public void StayIncognito()
    {
        if (Equals(behavior.search, SearchPlanner.UserInput)) return;

        m_Scouter?.Refresh();
    }

    public void StartChaseEvader()
    {
        if (Equals(behavior.alert, AlertPlanner.UserInput)) return;

        m_ChaseEvader.Begin();
    }

    // Intruder behavior when being chased
    public void KeepRunning()
    {
        if (Equals(behavior.alert, AlertPlanner.UserInput)) return;

        m_ChaseEvader.Refresh();
    }


    public void StartHiding()
    {
        if (Equals(behavior.search, SearchPlanner.UserInput)) return;

        m_SearchEvader.Begin();
    }


    // Intruder behavior after escaping guards
    public void KeepHiding()
    {
        if (Equals(behavior.search, SearchPlanner.UserInput)) return;

        m_SearchEvader.Refresh();
    }
}