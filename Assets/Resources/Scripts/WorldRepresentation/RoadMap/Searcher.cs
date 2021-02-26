using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Used for the search representation the game 
public class Searcher : MonoBehaviour
{
    private StealthArea m_StealthArea;
    private RoadMap m_roadMap;

    private GuardSearchPlanner m_searchType;

    public bool RenderSearchSegments;

    public void Initiate(StealthArea stealthArea)
    {
        m_StealthArea = stealthArea;
        m_roadMap = stealthArea.roadMap;

        foreach (var npcData in m_StealthArea.GetSessionInfo().GetNpcsData())
        {
            if (npcData.npcType == NpcType.Guard)
            {
                m_searchType = npcData.guardPlanner.Value.search;
                break;
            }
        }

        RenderSearchSegments = true;
    }

    // Move the interception point for the search phase
    public void PlaceSsForSearch(Vector2 position, Vector2 dir)
    {
        Clear();

        // Insert the search segment 
        m_roadMap.CreateArbitraryRoadMapLine(position, dir);
    }


    public void UpdateSearcher(float speed, List<Guard> guards, float timeDelta)
    {
        if (m_searchType == GuardSearchPlanner.RmPropSimple)
            UpdateSimpleSearch(speed, guards, timeDelta);
        else if (m_searchType == GuardSearchPlanner.RmPropOccupancyDiffusal)
            UpdateOccupancySearch(speed, guards, timeDelta);
    }


    // Start moving the phantoms across the road map and trim them if seen by guards
    // The probability is propagated with a factor.
    private void UpdateSimpleSearch(float speed, List<Guard> guards, float timeDelta)
    {
        // Spread the probability similarly to Third eye crime
        foreach (var line in m_roadMap.GetLines())
        {
            SimplePropagation(timeDelta, line);
            CheckSeenSs(guards, line);
            line.ExpandSs(speed, timeDelta);
        }
    }

    // The probability is diffused, similar to Third eye crime
    private void UpdateOccupancySearch(float speed, List<Guard> guards, float timeDelta)
    {
        foreach (var line in m_roadMap.GetLines())
        {
            DiffuseProb(line);
            CheckSeenSs(guards, line);
            line.ExpandSs(speed, timeDelta);
        }

        NormalizeProbs();
    }


    // Expand the search segments
    public void SimplePropagation(float timeDelta, RoadMapLine line)
    {
        line.PropagateProb();
        line.IncreaseProbability(timeDelta);
    }


    // Diffusing the probability among neighboring segments 
    // Source: EXPLORATION AND COMBAT IN NETHACK - Johnathan Campbell - Chapter 2.2.1
    public void DiffuseProb(RoadMapLine line)
    {
        SearchSegment sS = line.GetSearchSegment();
        float probabilitySum = 0f;
        int neighborsCount = 0;

        foreach (var con in line.GetWp1Connections())
            if (line != con)
            {
                probabilitySum += con.GetSearchSegment().GetProbability();
                neighborsCount++;
            }

        foreach (var con in line.GetWp2Connections())
            if (line != con)
            {
                probabilitySum += con.GetSearchSegment().GetProbability();
                neighborsCount++;
            }


        float newProbability = (1f - Properties.ProbDiffFac) * sS.GetProbability() +
                               (Properties.ProbDiffFac / neighborsCount) * probabilitySum;

        sS.SetProb(newProbability);
    }

    // Check for the seen search segments
    public void CheckSeenSs(List<Guard> guards, RoadMapLine line)
    {
        foreach (var guard in guards)
        {
            Polygon foV = guard.GetFoV();

            // Trim the parts seen by the guards and reset the section if it is all seen 
            line.CheckSeenSegment(foV);
        }
    }


    // Normalize the probabilities of the segments
    public void NormalizeProbs()
    {
        float maxProbability = 0f;
        foreach (var line in m_roadMap.GetLines())
        {
            float prob = line.GetSearchSegment().GetProbability();

            if (maxProbability < prob)
                maxProbability = prob;
        }

        foreach (var line in m_roadMap.GetLines())
        {
            SearchSegment sS = line.GetSearchSegment();
            sS.SetProb(sS.GetProbability() / maxProbability);
        }
    }


    // Get the best Search segment the guard show visit.
    public Vector2 GetSearchSegment(Guard requestingGuard, List<Guard> guards, Intruder intruder,
        List<MeshPolygon> navMesh, SearchWeights searchWeights)
    {
        SearchSegment bestSs = null;
        float maxFitnessValue = Mathf.NegativeInfinity;

        // Loop through the search segments in the lines
        foreach (var line in m_roadMap.GetLines())
        {
            SearchSegment sS = line.GetSearchSegment();

            // Skip the segment if it has a probability of zero or less
            if (sS.GetProbability() <= 0.2f)
                continue;

            // Get the distance of the closest goal other guards are coming to visit
            float minGoalDistance = Mathf.Infinity;

            foreach (var guard in guards)
            {
                // Skip the guard without a goal
                if (guard.GetGoal() == null)
                    continue;

                float distanceToGuardGoal =
                    PathFinding.GetShortestPathDistance(navMesh, sS.GetMidPoint(), guard.GetGoal().Value);

                if (minGoalDistance > distanceToGuardGoal)
                {
                    minGoalDistance = distanceToGuardGoal;
                }
            }

            minGoalDistance = minGoalDistance == Mathf.Infinity ? 0f : minGoalDistance;

            // Get the distance from the requesting guard
            float distanceToGuard = PathFinding.GetShortestPathDistance(navMesh, (sS.position1 + sS.position2) / 2f,
                requestingGuard.transform.position);

            // Calculate the fitness of the search segment
            // start with the probability
            float ssFitness = sS.GetFitness();

            // Calculate the overall heuristic of this search segment
            ssFitness = ssFitness * searchWeights.probWeight +
                        (sS.GetAge() / Properties.MaxAge) * searchWeights.ageWeight +
                        (minGoalDistance / Properties.MaxPathDistance) * searchWeights.dstToGuardsWeight +
                        (distanceToGuard / Properties.MaxPathDistance) * searchWeights.dstFromOwnWeight;


            if (maxFitnessValue < ssFitness)
            {
                maxFitnessValue = ssFitness;
                bestSs = sS;
            }
        }


        if (bestSs == null)
            return intruder.GetLastKnownLocation();

        return (bestSs.position1 + bestSs.position2) / 2f;
    }

    // The search is over so clear the variables
    public void Clear()
    {
        m_roadMap.ClearSearchSegments();
    }

    public void OnDrawGizmos()
    {
        if (RenderSearchSegments)
            if (m_roadMap != null)
            {
                m_roadMap.DrawSearchSegments();
            }
    }
}