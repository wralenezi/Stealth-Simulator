using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEditor;
using UnityEngine;
using Vector2 = UnityEngine.Vector2;

public class PathfindingTester : MonoBehaviour
{
    public Transform from;

    public Transform to;

    private List<Vector2> _path;
    public float PathDistance;
    public float EcludeanDistance;

    private void Start()
    {
        _path = new List<Vector2>();
    }


    public void PathFind()
    {
        _path.Clear();
        PathDistance = PathFinding.Instance.GetShortestPath(from.position, to.position, ref _path);
        EcludeanDistance = Vector2.Distance(from.position, to.position);
    }

    private void Draw(Vector2 from, Vector2 to)
    {
        Gizmos.DrawLine(from, to);
        Vector2 mid = (from + to) / 2f;
        float distance = Vector2.Distance(from, to);
#if UNITY_EDITOR
        Handles.Label(mid, distance.ToString());
#endif
    }

    public void OnDrawGizmos()
    {
        if (!Equals(_path, null))
        {
            Gizmos.color = Color.blue;
            for (int i = 0; i < _path.Count - 1; i++)
                Draw(_path[i], _path[i + 1]);

            Gizmos.color = Color.yellow;
            Draw(from.position, to.position);
        }
    }
}