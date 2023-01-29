using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleGridSearcher : Searcher
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
        _params = (GridSearcherParams)guardParams.searcherParams;
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

    public override void UpdateSearcher(float speed, List<Guard> guards, float timeDelta)
    {
        CheckHeatMapSpotting(guards, timeDelta);

        switch (_params.updateMethod)
        {
            case GridStalenessMethod.Propagation:
                PropagateProbability();
                break;
            
            case GridStalenessMethod.Diffuse:
                DiffuseProbability();
                break;
        }
        
    }

    private void SetStaleness(Vector3 lastPosition)
    {
        float closestDistanceNode = Mathf.Infinity;
        Node closestNode = null;
        foreach (var node in _heatNodes)
        {
            float sqrMag = Vector2.SqrMagnitude((Vector2)lastPosition - node.worldPosition);
            if (sqrMag < closestDistanceNode)
            {
                closestNode = node;
                closestDistanceNode = sqrMag;
            }
        }

        if (Equals(closestNode, null)) return;

        closestNode.staleness = 1f;
    }


    public override void Search(List<Guard> guards)
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


    private void PropagateProbability()
    {
        foreach (var node in _heatNodes)
        {
            float maxStaleness = 0f;
            foreach (var neighbor in node.GetNeighbors())
            {
                if (maxStaleness < neighbor.staleness && !neighbor.isSeen)
                {
                    maxStaleness = neighbor.staleness;
                }
            }

            if (maxStaleness < node.staleness) maxStaleness = node.staleness;

            if (!node.isSeen)
            {
                node.staleness += maxStaleness * m_Intruder.GetNpcSpeed() * 0.5f * Time.deltaTime;
            }
            else
            {
                node.staleness +=  m_Intruder.GetNpcSpeed() * 0.01f * Time.deltaTime;
            }
            node.staleness = Mathf.Clamp(node.staleness, 0f, 1f);
        }
    }


    // Diffusing the probability among neighboring segments 
    // Source: EXPLORATION AND COMBAT IN NETHACK - Johnathan Campbell - Chapter 2.2.1
    private void DiffuseProbability()
    {
        float maxProb = 0f;
        foreach (var node in _heatNodes)
        {
            float probabilitySum = 0f;
            int neighborsCount = 0;
            foreach (var con in node.GetNeighbors())
            {
                probabilitySum += con.staleness;
                neighborsCount++;
            }

            float newProbability = (1f - _params.ProbabilityDiffuseFactor) * node.staleness +
                                   _params.ProbabilityDiffuseFactor * probabilitySum / neighborsCount;

            node.staleness = Mathf.Clamp(newProbability, 0f, 1f);

            if (maxProb < node.staleness) maxProb = node.staleness;
        }

        NormalizeSegments(Equals(maxProb, 0f) ? 1f : maxProb);
    }

    // Normalize the probabilities of the segments
    // if the max prob is zero, then find the max prob
    protected void NormalizeSegments(float maxProb)
    {
        foreach (var node in _heatNodes)
        {
            node.staleness /= maxProb;
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
            Gizmos.color = new Color32(0, 0, 0, (byte)(node.staleness * 255));
            Gizmos.DrawCube(node.worldPosition, _nodeDimensions);
        }
    }
}

public class GridSearcherParams : SearcherParams
{
    // The length of the cell side
    public readonly float CellSide;

    public readonly GridStalenessMethod updateMethod;

    public readonly float ProbabilityDiffuseFactor;
    public readonly float StalenessWeight;
    public readonly float DistanceWeight;
    public readonly float SeparationWeight;

    public GridSearcherParams(float _cellSide, GridStalenessMethod _method)
    {
        CellSide = _cellSide;
        updateMethod = _method;
        ProbabilityDiffuseFactor = 0.2f;
        StalenessWeight = 1f;
    }
}

public enum GridStalenessMethod
{
    Diffuse,
    Propagation
}