using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class VisMeshPatrolDecisionMaker
{
    private VisMeshPatrolerParams _params;

    // Guard goals
    private Dictionary<string, Vector2> _guardGoals;

    public void Initiate(VisMeshPatrolerParams param)
    {
        _guardGoals = new Dictionary<string, Vector2>();
        _params = param;
    }

    public void Reset()
    {
        _guardGoals.Clear();
    }

    private bool IsGoalTaken(Guard guard, Vector2 goal)
    {
        float minSqrMagThreshold = 0.25f;

        foreach (var guardGoal in _guardGoals)
        {
            if (Equals(guardGoal.Key, guard.name)) continue;

            float sqrMag = (guardGoal.Value - goal).sqrMagnitude;

            if (minSqrMagThreshold >= sqrMag) return true;
        }


        return false;
    }

    public void SetTarget(Guard guard, List<VisibilityPolygon> unseenPolys)
    {
        VisibilityPolygon bestVisbilityPolygon = null;
        float highestScore = Mathf.Infinity;

        if (_guardGoals.ContainsKey(guard.name)) _guardGoals.Remove(guard.name);

        foreach (var visPoly in unseenPolys)
        {
            if (IsGoalTaken(guard, visPoly.GetCentroidPosition())) continue;

            float score = visPoly.GetTimestamp();

            if (highestScore > score)
            {
                highestScore = score;
                bestVisbilityPolygon = visPoly;
            }
        }

        if (Equals(bestVisbilityPolygon, null)) return;

        _guardGoals[guard.name] = bestVisbilityPolygon.GetCentroidPosition();
        guard.SetDestination(bestVisbilityPolygon.GetCentroidPosition(), false, false);
    }

    public void SetFittestTarget(Guard guard, VisMeshPatrolerParams patrolerParams, List<VisibilityPolygon> unseenPolys)
    {
        VisibilityPolygon bestVisbilityPolygon = null;
        float highestScore = Mathf.NegativeInfinity;

        foreach (var visPoly in unseenPolys)
        {
            float score = 0f;

            score += visPoly.GetStaleness() * patrolerParams.stalenessWeight;

            score += GetAreaPortion(visPoly) * patrolerParams.areaWeight;


            if (highestScore < score)
            {
                highestScore = score;
                bestVisbilityPolygon = visPoly;
            }
        }

        if (Equals(bestVisbilityPolygon, null)) return;

        guard.SetDestination(bestVisbilityPolygon.GetCentroidPosition(), false, false);
    }


    private float GetAreaPortion(Polygon polygon)
    {
        float totalArea = MapManager.Instance.mapDecomposer.GetNavMeshArea();

        return polygon.GetArea() / totalArea;
    }

    private float GetNormalizedDistance(Guard guard, Polygon goal)
    {
        float longestPath = PathFinding.Instance.longestShortestPath;

        float distance =
            PathFinding.Instance.GetShortestPathDistance(guard.GetTransform().position, goal.GetCentroidPosition());

        return distance / longestPath;
    }


    public float GetClosestGuardDistance(Guard guard, List<Guard> guards, Polygon polygon)
    {
        float longestPath = PathFinding.Instance.longestShortestPath;

        float closestGuardDistance = Mathf.Infinity;

        foreach (var g in guards)
        {
            if (Equals(guard, g)) continue;

            float distance =
                PathFinding.Instance.GetShortestPathDistance(g.GetTransform().position, polygon.GetCentroidPosition());

            if (distance < closestGuardDistance)
            {
                closestGuardDistance = distance;
            }
        }

        return closestGuardDistance / longestPath;
    }
}

public enum VMDecision
{
    Weighted,
}