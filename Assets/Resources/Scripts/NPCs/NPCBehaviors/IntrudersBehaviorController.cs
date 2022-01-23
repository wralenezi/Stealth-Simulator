using System.Collections.Generic;
using UnityEngine;

public class IntrudersBehaviorController : MonoBehaviour
{
    private Behavior m_behavior;

    public  Behavior behavior { get { return m_behavior; } }
    
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

        // foreach (var intruder in m_SA.intrdrManager.GetIntruders())
        // {
        m_Scouter.Refresh();

        // if (intruder.GetNpcData().intruderPlanner == IntruderPlanner.UserInput) return;

        // m_Scouter.

        // if (intruder.GetNpcData().intruderPlanner == IntruderPlanner.Random)
        //     // intruder.SetGoal(m_HidingSpots.GetRandomHidingSpot(), false);
        // else
        // {
        //     // m_HidingSpots.AssignHidingSpotsFitness(m_SA.guardsManager.GetGuards(), m_SA.mapDecomposer.GetNavMesh());
        //     // intruder.SetGoal(m_HidingSpots.GetBestHidingSpot().Value, false);
        // }
        // }
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

        // foreach (var intruder in m_SA.intrdrManager.GetIntruders())
        // {
        //     if (intruder.GetNpcData().intruderPlanner == IntruderPlanner.Random)
        //         intruder.SetGoal(m_HidingSpots.GetRandomHidingSpot(), false);
        //     else //if (GetNpcData().intruderPlanner == IntruderPlanner.Heuristic)
        //     if (!intruder.IsBusy())
        //     {
        //         // m_HidingSpots.AssignHidingSpotsFitness(m_SA.guardsManager.GetGuards(), m_SA.mapDecomposer.GetNavMesh());
        //         // intruder.SetGoal(m_HidingSpots.GetBestHidingSpot().Value, false);
        //     }
        // }
    }


    public void StartHiding()
    {
        if (Equals(behavior.search, SearchPlanner.UserInput)) return;

        m_SearchEvader.Begin();

        // foreach (var intruder in m_SA.intrdrManager.GetIntruders())
        // {
        //     // Find a place to hide
        //     if (intruder.GetNpcData().intruderPlanner == IntruderPlanner.Random ||
        //         intruder.GetNpcData().intruderPlanner == IntruderPlanner.RandomMoving)
        //         intruder.SetGoal(m_HidingSpots.GetRandomHidingSpot(), false);
        //     else if (intruder.GetNpcData().intruderPlanner == IntruderPlanner.Heuristic ||
        //              intruder.GetNpcData().intruderPlanner == IntruderPlanner.HeuristicMoving)
        //         if (!intruder.IsBusy())
        //         {
        //             m_HidingSpots.AssignHidingSpotsFitness(m_SA.guardsManager.GetGuards(), m_SA.mapDecomposer.GetNavMesh());
        //             intruder.SetGoal(m_HidingSpots.GetBestHidingSpot().Value, false);
        //         }
        // }
    }


    // Intruder behavior after escaping guards
    public void KeepHiding()
    {
        if (Equals(behavior.search, SearchPlanner.UserInput)) return;

        m_SearchEvader.Refresh();

        // foreach (var intruder in m_SA.intrdrManager.GetIntruders().Where(intruder => !intruder.IsBusy()))
        // {
        //     if (intruder.GetNpcData().intruderPlanner == IntruderPlanner.HeuristicMoving)
        //     {
        //         m_HidingSpots.AssignHidingSpotsFitness(m_SA.guardsManager.GetGuards(), m_SA.mapDecomposer.GetNavMesh());
        //         StartCoroutine(intruder.waitThenMove(m_HidingSpots.GetBestHidingSpot().Value));
        //     }
        //     else if (intruder.GetNpcData().intruderPlanner == IntruderPlanner.RandomMoving)
        //         StartCoroutine(intruder.waitThenMove(m_HidingSpots.GetRandomHidingSpot()));
        // }
    }
}