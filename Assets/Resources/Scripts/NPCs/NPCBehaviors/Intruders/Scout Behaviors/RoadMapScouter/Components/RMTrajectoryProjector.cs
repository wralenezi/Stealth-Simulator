using System.Collections.Generic;
using UnityEngine;

public class RMTrajectoryProjector
{
    private float _fovMultiplier;
    private TrajectoryType _trajectoryType;
    private List<PossibleTrajectory> _possibleTrajectories;

    public void Initiate(TrajectoryType trajectoryType, float fovMultiplier)
    {
        _possibleTrajectories = new List<PossibleTrajectory>();
        _trajectoryType = trajectoryType;
        _fovMultiplier = fovMultiplier;
    }

    public List<PossibleTrajectory> GetTrajectories()
    {
        return _possibleTrajectories;
    }

    /// <summary>
    /// Project the guards position on the road map.
    /// </summary>
    public void SetGuardTrajectories(RoadMap roadMap, List<Guard> guards)
    {
        _possibleTrajectories.Clear();
        roadMap.ClearTempWayPoints();

        // float fov = Properties.GetFovRadius(NpcType.Guard);
        foreach (var guard in guards)
        {
            // Get the closest point on the road map to the guard
            Vector2? point = roadMap.GetClosetWpPairToPoint(guard.GetTransform().position, guard.GetDirection(), true,
                out RoadMapNode wp1, out RoadMapNode wp2);

            // if there is no intersection then abort
            if (!point.HasValue) return;

            float projectionDistance = GetGuardProjectionDistance(guard);
            float stepSize = 2f;

            switch (_trajectoryType)
            {
                case TrajectoryType.Simple:
                    roadMap.ProjectPositionsInDirection(ref _possibleTrajectories, point.Value, wp1, wp2, stepSize,
                        projectionDistance, guard);
                    break;

                case TrajectoryType.AngleBased:
                    roadMap.ProjectPositionsByAngle(ref _possibleTrajectories, point.Value, wp1, wp2, stepSize,
                        projectionDistance, guard);
                    break;
            }
        }
    }

    private float GetGuardProjectionDistance(NPC npc)
    {
        float fov = Properties.GetFovRadius(NpcType.Guard);
        return fov + GetGuardProjectionOffset(npc);
    }

    private float GetGuardProjectionOffset(NPC npc)
    {
        float fov = Properties.GetFovRadius(NpcType.Guard);
        float projectionMultiplier = 33f;

        float speed = Equals(npc, null) ? Properties.NpcSpeed : npc.GetCurrentSpeed();
        return Mathf.Max(speed * fov * projectionMultiplier * _fovMultiplier, fov * 0.2f);
    }
}

public class PossibleTrajectory
{
    private List<Vector2> _path;

    public NPC npc;

    public PossibleTrajectory(NPC _npc)
    {
        _path = new List<Vector2>();
        npc = _npc;
    }

    public void AddPoint(Vector2 point)
    {
        _path.Add(point);
    }

    public void CopyTrajectory(PossibleTrajectory original)
    {
        _path.Clear();
        npc = original.npc;
        foreach (var pt in original.GetPath())
            _path.Add(pt);
    }

    public List<Vector2> GetPath()
    {
        return _path;
    }

    public Vector2 GetFirstPoint()
    {
        return _path[0];
    }

    public Vector2 GetLastPoint()
    {
        return _path[_path.Count - 1];
    }

    public float GetDistanceToPoint(Vector2 point)
    {
        return PathFinding.Instance.GetShortestPathDistance(_path[0], point);
    }

    public void Draw()
    {
        for (int i = 0; i < _path.Count - 1; i++)
        {
            Gizmos.DrawLine(_path[i], _path[i + 1]);
        }
        
        
    }
}

// Type of the trajectory the guard is formed.
public enum TrajectoryType
{
    Simple,

    AngleBased,

    None
}