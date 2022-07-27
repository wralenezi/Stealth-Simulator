using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RMProperties
{
    private RMPathFinder _pathFinder;

    private int _nodeCount;
    private int _edgeCount;
    
    // The total length of graph
    private float _totalLength;

    // Number of nodes
    private int _nodesCount;

    // The Eccentricities of the vertices.
    private Dictionary<int, float> _eccentricities;

    private float _radius;

    private float _diameter;

    private List<List<int>> _graphCycles;
    private Dictionary<int, bool> _fullyVisitedFlags;

    public RMProperties(RoadMap roadMap)
    {
        _pathFinder = new RMPathFinder();

        SetTotalLength(roadMap);
        Debug.Log("Node Count: " + _nodeCount);
        Debug.Log("Edge Count: " + _edgeCount);
        Debug.Log("Total Length: " + _totalLength);


        
        SetEccentricities(roadMap);
        _radius = GetRadius();
        _diameter = GetDiameter();

        Debug.Log("Radius: " + _radius);
        Debug.Log("Diameter: " + _diameter);

        SetCycles(roadMap);

        Debug.Log("Number of Cycles: " + _graphCycles.Count);

        Debug.Log("Circumference: " + GetCircumference(roadMap));

        Debug.Log("Girth: " + GetGirth(roadMap));
    }

    private void SetTotalLength(RoadMap roadMap)
    {
        List<RoadMapNode> nodes = roadMap.GetNode(true);

        _totalLength = 0f;
        _edgeCount = 0;
        _nodeCount = 0;

        // Go through each edge
        for (int i = 0; i < nodes.Count; i++)
        {
            // Ignore the added corner nodes
            if (Equals(nodes[i].type, NodeType.Corner)) continue;

            _nodeCount++;

            float maxDistance = Mathf.NegativeInfinity;
            for (int j = i + 1; j < nodes.Count; j++)
            {
                if (Equals(nodes[j].type, NodeType.Corner)) continue;

                List<RoadMapNode> firstConnections = nodes[i].GetConnections(true);
                bool isSecNodeConnected = firstConnections.Contains(nodes[j]);

                List<RoadMapNode> secondConnections = nodes[j].GetConnections(true);
                bool isfirstNodeConnected = secondConnections.Contains(nodes[i]);

                if (!isfirstNodeConnected || !isSecNodeConnected) continue;

                float distance = Vector2.Distance(nodes[i].GetPosition(), nodes[j].GetPosition());
                _totalLength += distance;

                _edgeCount++;
            }
        }
    }

    private void SetEccentricities(RoadMap roadMap)
    {
        _eccentricities = new Dictionary<int, float>();

        List<RoadMapNode> nodes = roadMap.GetNode(true);

        // Go through each edge
        for (int i = 0; i < nodes.Count; i++)
        {
            // Ignore the added corner nodes
            if (Equals(nodes[i].type, NodeType.Corner)) continue;

            float maxDistance = Mathf.NegativeInfinity;

            for (int j = 0; j < nodes.Count; j++)
            {
                if (Equals(nodes[j].type, NodeType.Corner)) continue;

                if (i == j) continue;

                float roadmapDistance = _pathFinder.GetPathDistance(roadMap, nodes[i], nodes[j]);
                
                // Get the max distance to another node
                if (maxDistance < roadmapDistance) maxDistance = roadmapDistance;
            }

            _eccentricities.Add(nodes[i].Id, maxDistance);
        }
    }

    private float GetRadius()
    {
        float minEcce = Mathf.Infinity;
        foreach (var ecceDic in _eccentricities)
        {
            if (ecceDic.Value < minEcce)
                minEcce = ecceDic.Value;
        }

        return minEcce;
    }


    private float GetDiameter()
    {
        float maxEcce = Mathf.NegativeInfinity;
        foreach (var ecceDic in _eccentricities)
        {
            if (ecceDic.Value > maxEcce)
                maxEcce = ecceDic.Value;
        }

        return maxEcce;
    }

    private void SetCycles(RoadMap roadMap)
    {
        _graphCycles = new List<List<int>>();
        _fullyVisitedFlags = new Dictionary<int, bool>();

        List<RoadMapNode> nodes = roadMap.GetNode(true);

        foreach (var n in nodes)
            _fullyVisitedFlags.Add(n.Id, false);

        // Go through each edge
        foreach (var node in nodes)
        {
            // Ignore the added corner nodes
            if (Equals(node.type, NodeType.Corner)) continue;

            List<int> path = new List<int>();
            ExpandCycle(node, path);

            _fullyVisitedFlags[node.Id] = true;
        }


        for (int i = 0; i < _graphCycles.Count; i++)
        {
            List<int> cycle = _graphCycles[i];

            List<int> tempCycle = new List<int>();
            tempCycle.AddRange(cycle);

            tempCycle.Reverse();

            for (int j = i + 1; j < _graphCycles.Count; j++)
            {
                if (!Enumerable.SequenceEqual(tempCycle, _graphCycles[j])) continue;

                _graphCycles.RemoveAt(j);
                break;
            }
        }
    }

    private void ExpandCycle(RoadMapNode currentNode, List<int> currentPath)
    {
        // Cycle detected
        if (currentPath.Count > 2 && Equals(currentPath[0], currentNode.Id))
        {
            currentPath.Add(currentPath[0]);
            _graphCycles.Add(currentPath);
            return;
        }

        if (currentPath.Contains(currentNode.Id)) return;

        currentPath.Add(currentNode.Id);

        List<RoadMapNode> children = currentNode.GetConnections(true);

        foreach (var child in children)
        {
            if (Equals(child.type, NodeType.Corner)) continue;

            if (_fullyVisitedFlags[child.Id]) continue;

            List<int> path = new List<int>();
            path.AddRange(currentPath);

            ExpandCycle(child, path);
        }
    }


    private float GetCircumference(RoadMap roadMap)
    {
        float longestCycle = Mathf.NegativeInfinity;

        foreach (var cycle in _graphCycles)
        {
            float length = 0f;
            for (int i = 0; i < cycle.Count - 1; i++)
            {
                length += Vector2.Distance(roadMap.GetNodeById(cycle[i]).GetPosition(),
                    roadMap.GetNodeById(cycle[i + 1]).GetPosition());
            }

            length += Vector2.Distance(roadMap.GetNodeById(cycle[0]).GetPosition(),
                roadMap.GetNodeById(cycle[cycle.Count - 1]).GetPosition());

            if (length > longestCycle)
            {
                longestCycle = length;
            }
        }

        return longestCycle;
    }

    private float GetGirth(RoadMap roadMap)
    {
        float shortestCycle = Mathf.Infinity;

        foreach (var cycle in _graphCycles)
        {
            float length = 0f;
            for (int i = 0; i < cycle.Count - 1; i++)
            {
                length += Vector2.Distance(roadMap.GetNodeById(cycle[i]).GetPosition(),
                    roadMap.GetNodeById(cycle[i + 1]).GetPosition());
            }

            length += Vector2.Distance(roadMap.GetNodeById(cycle[0]).GetPosition(),
                roadMap.GetNodeById(cycle[cycle.Count - 1]).GetPosition());

            if (length < shortestCycle)
            {
                shortestCycle = length;
            }
        }

        return shortestCycle;
    }
}