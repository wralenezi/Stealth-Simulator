using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class VisMeshPatrolDecisionMaker
{
    private VisMeshPatrolerParams _params;

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
        float minSqrMagThreshold = 1f;

        foreach (var guardGoal in _guardGoals)
        {
            if (Equals(guardGoal.Key, guard.name)) continue;

            float sqrMag = (guardGoal.Value - goal).sqrMagnitude;

            if (minSqrMagThreshold >= sqrMag) return true;
        }

        return false;
    }

    public void SetTarget(Guard guard, List<Guard> guards, VisMeshPatrolerParams patrolerParams,
        List<VisibilityPolygon> unseenPolys)
    {
        Vector2? target = null;

        if (_guardGoals.ContainsKey(guard.name)) _guardGoals.Remove(guard.name);

        switch (patrolerParams.DecisionType)
        {
            case VMDecision.Weighted:
                target = GetWeightedSumFittestTarget(guard, guards, patrolerParams, unseenPolys);
                break;
        }


        if (Equals(target, null)) return;

        _guardGoals[guard.name] = target.Value;
        guard.SetDestination(target.Value, false, false);
    }

    private Vector2? GetWeightedSumFittestTarget(Guard guard, List<Guard> guards,
        VisMeshPatrolerParams patrolerParams, List<VisibilityPolygon> unseenPolys)
    {
        VisibilityPolygon bestTarget = null;
        float highestScore = Mathf.NegativeInfinity;

        foreach (var visPoly in unseenPolys)
        {
            if (IsGoalTaken(guard, visPoly.GetCentroidPosition())) continue;

            float score = 0f;

            score += visPoly.GetStaleness() * patrolerParams.StalenessWeight;

            score += GetAreaPortion(visPoly) * patrolerParams.AreaWeight;

            // Subtracted by 1 to reverse the relation ( higher value is closer, thus more desirable)
            score += (1f - GetNormalizedDistance(guard, visPoly)) * patrolerParams.DistanceWeight;

            score += GetClosestGuardDistance(guard, guards, visPoly) * patrolerParams.SeparationWeight;

            if (highestScore < score)
            {
                highestScore = score;
                bestTarget = visPoly;
            }
        }

        return bestTarget?.GetCentroidPosition();
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


    private float GetClosestGuardDistance(Guard guard, List<Guard> guards, Polygon polygon)
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

    public void DrawGoals()
    {
        Gizmos.color = Color.red;
        foreach (var guardGoal in _guardGoals)
        {
#if UNITY_EDITOR
            Handles.Label(guardGoal.Value, guardGoal.Key);
#endif
            Gizmos.DrawSphere(guardGoal.Value, 0.1f);
        }
    }
}

public enum VMDecision
{
    Weighted,
}