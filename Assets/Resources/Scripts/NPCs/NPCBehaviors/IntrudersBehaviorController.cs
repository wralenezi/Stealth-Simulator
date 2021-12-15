using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class IntrudersBehaviorController : MonoBehaviour
{
    private StealthArea m_SA;

    // The controller for intruders behavior when they have never been spotted.
    private Scouter m_Scouter;
    
    // Stealth navigator controller

    public void Initiate(StealthArea stealthArea,Transform map)
    {
        m_SA = stealthArea;
        
        m_Scouter = gameObject.AddComponent<RoadMapScouter>();
        m_Scouter.Initiate(m_SA);
    }


    // 
    public void StayIncognito()
    {
        foreach (var intruder in m_SA.intrdrManager.GetIntruders())
        {
            if (intruder.GetNpcData().intruderPlanner == IntruderPlanner.UserInput) return;

            // if (intruder.GetNpcData().intruderPlanner == IntruderPlanner.Random)
            //     // intruder.SetGoal(m_HidingSpots.GetRandomHidingSpot(), false);
            // else
            // {
            //     // m_HidingSpots.AssignHidingSpotsFitness(m_SA.guardsManager.GetGuards(), m_SA.mapDecomposer.GetNavMesh());
            //     // intruder.SetGoal(m_HidingSpots.GetBestHidingSpot().Value, false);
            // }
        }
    }

    // Intruder behavior when being chased
    public void KeepRunning()
    {
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