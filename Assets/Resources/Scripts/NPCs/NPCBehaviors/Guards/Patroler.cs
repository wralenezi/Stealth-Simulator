using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Patroler : MonoBehaviour
{
    private StealthArea m_SA;

    // Road map of the level
    private RoadMap m_RoadMap;
    
    
    public void Initiate(StealthArea stealthArea)
    {
        m_SA = stealthArea;
        m_RoadMap = stealthArea.roadMap;
        
    }

    // Check for the seen search segments
    private void CheckSeenSs(RoadMapLine line)
    {
        foreach (var guard in m_SA.guardsManager.GetGuards())
        {
            // Trim the parts seen by the guards and reset the section if it is all seen 
            line.CheckSeenSegment(guard);
        }
    }
    
    
    // Set the road map segments to 1. To mark the beginning of the patrol shift
    public void FillSegments()
    {
        foreach (var line in m_RoadMap.GetLines())
        {
            line.SetSearchSegment(line.wp1.GetPosition(),line.wp2.GetPosition(),1f,StealthArea.GetElapsedTime());
        }
    }


    public void UpdatePatroler(float speed, float timeDelta)
    {
        float maxProbability = 0f;

        // Spread the probability similarly to Third eye crime
        foreach (var line in m_RoadMap.GetLines())
        {
            line.PropagateProb(speed, timeDelta);
            line.IncreaseProbability(speed, timeDelta);
            line.ExpandSs(speed, timeDelta);

            // Get the max probability
            float prob = line.GetSearchSegment().GetProbability();
            if (maxProbability < prob)
                maxProbability = prob;
        }
        
        foreach (var line in m_RoadMap.GetLines())
        {
            CheckSeenSs(line);

            SearchSegment sS = line.GetSearchSegment();
            if (Math.Abs(maxProbability) > 0.0001f)
                sS.SetProb(sS.GetProbability() / maxProbability);
            else
                sS.SetProb(sS.GetProbability());
        }
    }


    public void GetPatrolPath(Guard guard)
    {
        m_RoadMap.GetPath(guard);
    }



}
