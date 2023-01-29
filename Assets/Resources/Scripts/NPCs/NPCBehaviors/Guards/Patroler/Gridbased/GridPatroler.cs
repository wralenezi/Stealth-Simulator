using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class GridPatroler : Patroler
{
    [SerializeField]
    private GridPatrolerParams _params;
    private GridPatrolerDecisionMaker _decisionMaker;

    public bool RenderGrid;
    private Vector3 _nodeDimensions;
    private MapGrid<Node> _heatMap;
    private List<Node> _heatNodes;

    public override void Initiate(MapManager mapManager, GuardBehaviorParams patrolParams)
    {
        _params = (GridPatrolerParams)patrolParams.patrolerParams;
        _nodeDimensions = new Vector3(_params.CellSide, _params.CellSide, _params.CellSide) * 0.95f;

        _decisionMaker = new GridPatrolerDecisionMaker();
        SetupGrid(MapManager.Instance.mapRenderer.GetMapBoundingBox(), _params.CellSide);

        RenderGrid = true;
    }

    private void SetupGrid(Bounds bounds, float cellSide)
    {
        _heatMap = new MapGrid<Node>(bounds, cellSide, cellSide);
        _heatNodes = new List<Node>();

        Node[,] map = _heatMap.GetGrid();

        for (int i = 0; i < map.GetLength(0); i++)
        for (int j = 0; j < map.GetLength(1); j++)
        {
            map[i, j].worldPosition = _heatMap.GetWorldPosition(i, j);

            if (_heatMap.IsNodeInMap(map[i, j].worldPosition, cellSide * 0.5f))
                _heatNodes.Add(map[i, j]);
        }
    }

    public override void Start()
    {
        foreach (var node in _heatNodes)
            node.Reset();
    }

    private void IncrementHeatMapVisibility(List<Guard> guards, float timeDelta)
    {
        foreach (var node in _heatNodes)
            CheckIfNodeVisible(guards, timeDelta, node);
    }

    private void CheckIfNodeVisible(List<Guard> guards, float timeDelta, Node node)
    {
        bool isVisible = false;

        foreach (var guard in guards)
        {
            isVisible = guard.GetFovPolygon().IsPointInPolygon(node.worldPosition, true);
            if (isVisible) break;
        }

        if (isVisible) node.Spotted(StealthArea.GetElapsedTimeInSeconds());
    }

    // Set the normalized heat values
    private void CalculateHeatValues()
    {
        float minValue = Mathf.Infinity;
        float maxValue = Mathf.NegativeInfinity;
    
        foreach (var node in _heatNodes)
        {
            float spottedTime = node.GetLastSpottedTime();
    
            if (spottedTime < minValue) minValue = spottedTime;
    
            if (spottedTime > maxValue) maxValue = spottedTime;
        }
    
        foreach (var node in _heatNodes)
        {
            float spottedTime = node.GetLastSpottedTime();
            node.staleness = 1f - (spottedTime - minValue) / (maxValue - minValue);
        }
    }

    public override void UpdatePatroler(List<Guard> guards, float speed, float timeDelta)
    {
        IncrementHeatMapVisibility(guards, timeDelta);
        CalculateHeatValues();
    }

    public override void Patrol(List<Guard> guards)
    {
        AssignGoals(guards);
    }

    
    private void AssignGoals(List<Guard> guards)
    {
        foreach (var guard in guards)
        {
            if (!guard.IsBusy())
                _decisionMaker.SetTarget(guard, guards, _params, _heatNodes);
        }
    }

    
    public void OnDrawGizmos()
    {
        if (!RenderGrid) return;
        foreach (var node in _heatNodes)
        {
            Gizmos.color = new Color32(0, 0, 0, (byte)(node.staleness * 255));
            Gizmos.DrawCube(node.worldPosition, _nodeDimensions);
        }
    }
}

[Serializable]
public class GridPatrolerParams : PatrolerParams
{
    // The length of the cell side
    public readonly float CellSide;

    public readonly float StalenessWeight;
    public readonly float DistanceWeight;
    public readonly float SeparationWeight;

    public GridPatrolerParams(float _cellSide, float _stalenessWeight, float _distanceWeight, float _separationWeight)
    {
        CellSide = _cellSide;
        StalenessWeight = _stalenessWeight;
        DistanceWeight = _distanceWeight;
        SeparationWeight = _separationWeight;
    }

    public override string ToString()
    {
        string output = "";
        string sep = "_";

        output += CellSide;
        output += sep;

        output += StalenessWeight;
        output += sep;

        output += DistanceWeight;
        output += sep;

        output += SeparationWeight;
        output += sep;

        return output;
    }
}