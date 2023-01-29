using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridPatrolerDecisionMaker
{
    private Dictionary<string, Vector2> _guardGoals;

    public GridPatrolerDecisionMaker()
    {
        _guardGoals = new Dictionary<string, Vector2>();
    }

    public void SetTarget(Guard guard, List<Guard> guards, GridPatrolerParams _params, List<Node> _nodes)
    {
        Vector2? target = null;
        if (_guardGoals.ContainsKey(guard.name)) _guardGoals.Remove(guard.name);

        target = GetWeightedSumFittestTarget(guard, guards, _params, _nodes);
        
        if (Equals(target, null)) return;
        _guardGoals[guard.name] = target.Value;
        guard.SetDestination(target.Value, false, false);
    }

    private Vector2? GetWeightedSumFittestTarget(Guard guard, List<Guard> guards, GridPatrolerParams _params,
        List<Node> _node)
    {
        Node bestTarget = null;
        float highestScore = Mathf.NegativeInfinity;


        foreach (var node in _node)
        {
            if (IsGoalTaken(guard, node.worldPosition)) continue;

            float score = 0f;

            score += node.staleness * _params.StalenessWeight;

            // Subtracted by 1 to reverse the relation ( higher value is closer, thus more desirable)
            score += (1f - GetNormalizedDistance(guard, node)) * _params.DistanceWeight;

            score += GetClosestGuardDistance(guard, guards, node) * _params.SeparationWeight;

            if (highestScore < score)
            {
                highestScore = score;
                bestTarget = node;
            }
        }
        
        return bestTarget?.worldPosition;

    }

    private float GetNormalizedDistance(Guard guard, Node node)
    {
        float longestPath = PathFinding.Instance.longestShortestPath;

        float distance =
            PathFinding.Instance.GetShortestPathDistance(guard.GetTransform().position, node.worldPosition);

        return distance / longestPath;
    }

    private float GetClosestGuardDistance(Guard guard, List<Guard> guards, Node node)
    {
        float longestPath = PathFinding.Instance.longestShortestPath;

        float closestGuardDistance = Mathf.Infinity;

        foreach (var g in guards)
        {
            if (Equals(guard, g)) continue;

            float distance =
                PathFinding.Instance.GetShortestPathDistance(g.GetTransform().position, node.worldPosition);

            if (distance < closestGuardDistance)
            {
                closestGuardDistance = distance;
            }
        }

        return closestGuardDistance / longestPath;
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
}