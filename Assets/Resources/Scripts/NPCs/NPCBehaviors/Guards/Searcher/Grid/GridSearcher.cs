using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridSearcher : Searcher
{
    private GridSearcherDecisionMaker _decisionMaker;
    [SerializeField] private GridSearcherParams _params;

    public bool RenderGrid;
    private Vector3 _nodeDimensions;
    private MapGrid<Node> _heatMap;
    private List<Node> _heatNodes;


    public override void Initiate(MapManager mapManager, GuardBehaviorParams guardParams)
    {
        base.Initiate(mapManager, guardParams);
        _params = (GridSearcherParams) guardParams.searcherParams;
        _nodeDimensions = new Vector3(_params.CellSide, _params.CellSide, _params.CellSide);
        SetupGrid(MapManager.Instance.mapRenderer.GetMapBoundingBox(), _params.CellSide);
        _decisionMaker = new GridSearcherDecisionMaker();

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

            for (int k = Mathf.Max(0, i - 1); k < Mathf.Min(map.GetLength(0), i + 2); k++)
            for (int l = Mathf.Max(0, j - 1); l < Mathf.Min(map.GetLength(1), j + 2); l++)
            {
                if (i != k || j != l)
                    map[i, j].AddNeighbours(map[k, l]);
            }
        }
    }


    private void ResetGrid()
    {
        foreach (var node in _heatNodes)
            node.Reset();
    }

    public override void CommenceSearch(NPC target)
    {
        ResetGrid();
        SetStaleness(target.transform.position);
    }

    public override void Clear()
    {
        base.Clear();
        ResetGrid();
    }

    protected override void UpdateSearcher(float speed, List<Guard> guards, float timeDelta)
    {
        switch (_params.updateMethod)
        {
            case ProbabilityFlowMethod.Propagation:
                PropagateProbability(timeDelta);
                break;

            case ProbabilityFlowMethod.Diffuse:
                DiffuseProbability();
                break;
        }
        
        CheckHeatMapSpotting(guards, timeDelta);

        NormalizeSegments();
    }

    private void SetStaleness(Vector3 lastPosition)
    {
        float closestDistanceNode = Mathf.Infinity;
        Node closestNode = null;
        foreach (var node in _heatNodes)
        {
            float sqrMag = Vector2.SqrMagnitude((Vector2) lastPosition - node.worldPosition);
            if (sqrMag < closestDistanceNode)
            {
                closestNode = node;
                closestDistanceNode = sqrMag;
            }
        }

        if (Equals(closestNode, null)) return;

        closestNode.staleness = 1f;
        closestNode.oldStaleness = 1f;
    }


    protected override void Search(List<Guard> guards)
    {
        AssignGoals(guards);
    }

    private void CheckHeatMapSpotting(List<Guard> guards, float timeDelta)
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

    private void PropagateProbability(float deltaTime)
    {
        foreach (var node in _heatNodes)
        {
            if (!node.isExpansionDone)
            {
                if (!Equals(node.staleness, 1f)) continue;

                bool allExpanded = true;
                foreach (var neighbor in node.GetNeighbors())
                {
                    if (neighbor.isIncrementedThisRound) continue;

                    neighbor.staleness += m_Intruder.GetNpcSpeed() * 1.8f * deltaTime;
                    neighbor.staleness = Mathf.Clamp(neighbor.staleness, 0f, 1f);
                    if (neighbor.staleness < 1f) allExpanded = false;
                    neighbor.isIncrementedThisRound = true;
                }

                if (allExpanded) node.isExpansionDone = true;
            }
            else
            {
                node.staleness += m_Intruder.GetNpcSpeed() * 0.001f * deltaTime;
                node.staleness = Mathf.Clamp(node.staleness, 0f, 1f);
            }
        }
    }


    // Diffusing the probability among neighboring segments 
    // Source: EXPLORATION AND COMBAT IN NETHACK - Johnathan Campbell - Chapter 2.2.1
    private void DiffuseProbability()
    {
        foreach (var node in _heatNodes)
        {
            float probabilitySum = 0f;
            int neighborsCount = 0;
            foreach (var con in node.GetNeighbors())
            {
                probabilitySum += con.oldStaleness;
                neighborsCount++;
            }

            node.staleness = (1f - _params.ProbabilityDiffuseFactor) * node.oldStaleness +
                             _params.ProbabilityDiffuseFactor * probabilitySum / neighborsCount;
        }
    }

    // Normalize the probabilities of the segments
    // if the max prob is zero, then find the max prob
    protected void NormalizeSegments()
    {
        float maxStaleness = 0f;
        float minStaleness = 1f;
        foreach (var node in _heatNodes)
        {
            if (maxStaleness < node.staleness) maxStaleness = node.staleness;
            if (minStaleness > node.staleness) minStaleness = node.staleness;
        }

        maxStaleness = Equals(maxStaleness, 0f) ? 1f : maxStaleness;

        foreach (var node in _heatNodes)
        {
            node.isIncrementedThisRound = false;
            node.staleness = (node.staleness - minStaleness) / (maxStaleness - minStaleness);
            node.oldStaleness = node.staleness;
        }
    }

    private void AssignGoals(List<Guard> guards)
    {
        foreach (var guard in guards)
        {
            if (!guard.IsBusy())
                _decisionMaker.SetTarget(guard, guards, _params, _heatNodes);
        }
    }

    private void OnDrawGizmos()
    {
        if (!RenderGrid) return;
        foreach (var node in _heatNodes)
        {
            Gizmos.color = new Color32(255, 0, 0, (byte) (node.staleness * 200));
            Gizmos.DrawCube(node.worldPosition, _nodeDimensions);
        }
    }
}

public class GridSearcherParams : SearcherParams
{
    // The length of the cell side
    public readonly float CellSide;

    public readonly ProbabilityFlowMethod updateMethod;

    public readonly float ProbabilityDiffuseFactor;
    public readonly float StalenessWeight;
    public readonly float DistanceWeight;
    public readonly float SeparationWeight;

    public GridSearcherParams(float _cellSide, ProbabilityFlowMethod _method, float _stalenessWeight, float _distanceWeight, float _separationWeight)
    {
        CellSide = _cellSide;
        updateMethod = _method;
        ProbabilityDiffuseFactor = 0.05f;
        StalenessWeight = _stalenessWeight;
        DistanceWeight = _distanceWeight;
        SeparationWeight = _separationWeight;
    }
    
    public override string ToString()
    {
        string output = "";
        string sep = "_";

        output += GetType();
        output += sep;
        
        output += CellSide;
        output += sep;

        output += updateMethod;
        output += sep;

        output += StalenessWeight;
        output += sep;

        output += DistanceWeight;
        output += sep;

        output += SeparationWeight;

        return output;
    }

}

public enum ProbabilityFlowMethod
{
    Diffuse,
    Propagation
}