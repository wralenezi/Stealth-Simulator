using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VisibilityGraph : MonoBehaviour
{
    public bool IsRenderVisibilityGraph;

    // To get the map geometry
    private MapRenderer m_mapRenderer;

    // RoadMap of the World
    private List<WayPoint> m_graphNodes;


    // The radius the determine the neighborhood of a node
    private float m_NeighborhoodRadius = 10f;

    // The merge threshold
    private float m_MergeThreshold = 1f;

    // Initiate 
    public void Initiate(MapRenderer mapRenderer)
    {
        m_mapRenderer = mapRenderer;
        m_graphNodes = new List<WayPoint>();

        // Create the nodes on the interior of the map for each point in the map.
        CreateNodes();

        SimplifyNodes();

        ConnectNodes();
    }

    // Distribute the nodes on the angles of the graph
    private void CreateNodes()
    {
        // Get the walls data
        List<Polygon> walls = m_mapRenderer.GetWalls();

        // Go through the points
        for (int i = 0; i < walls.Count; i++)
        for (int j = 0; j < walls[i].GetVerticesCount(); j++)
        {
            Polygon wall = walls[i];

            // Get the normal vector of that angle (point)
            Vector2 angleNormal =
                GeometryHelper.GetNormal(wall.GetPoint(j - 1), wall.GetPoint(j), wall.GetPoint(j + 1));

            // Shorten the distance
            angleNormal *= 0.5f;

            float distanceFromCorner = 2f;

            Vector2 position = wall.GetPoint(j);

            if (GeometryHelper.IsReflex(wall.GetPoint(j - 1), wall.GetPoint(j), wall.GetPoint(j + 1)))
                position -= angleNormal;
            else
                position += angleNormal;

            WayPoint wp = new WayPoint(position) {WallId = i};

            m_graphNodes.Add(wp);
        }
    }

    // Merge the nearby nodes by removing them and place on node in the middle
    private void SimplifyNodes()
    {
        for (int i = 0; i < m_graphNodes.Count; i++)
        for (int j = i + 1; j < m_graphNodes.Count; j++)
        {
            if (m_mapRenderer.VisibilityCheck(m_graphNodes[i].GetPosition(), m_graphNodes[j].GetPosition()) &&
                Vector2.Distance(m_graphNodes[i].GetPosition(), m_graphNodes[j].GetPosition()) <= m_MergeThreshold)
            {
                Vector2 midPoint = (m_graphNodes[i].GetPosition() + m_graphNodes[j].GetPosition()) / 2f;
                m_graphNodes.RemoveAt(i);
                m_graphNodes.RemoveAt(j - 1);

                m_graphNodes.Add(new WayPoint(midPoint));
                i = 0;
                j = 1;
            }
        }
    }


    // Connect the nodes together to make a graph
    private void ConnectNodes()
    {
        for (int i = 0; i < m_graphNodes.Count; i++)
        for (int j = i + 1; j < m_graphNodes.Count; j++)
        {
            // Check if the are mutually visible

            // Get the direction
            Vector2 direction = (m_graphNodes[i].GetPosition() - m_graphNodes[j].GetPosition()).normalized;


            float extension = 2f;

            // Extend the length of the line check by both sides
            Vector2 firstPoint = m_graphNodes[i].GetPosition() + direction * extension;
            Vector2 secPoint = m_graphNodes[j].GetPosition() - direction * extension;

            bool isVisible = true;

            RaycastHit2D[] hits = Physics2D.LinecastAll(firstPoint, secPoint);

            foreach (var hit in hits)
            {
                int hitWallId = hit.collider.gameObject.GetComponent<Wall>().WallId;

                if (hitWallId == i)
                {
                    isVisible = false;
                    break;
                }
            }

            bool isClose = Vector2.Distance(m_graphNodes[i].GetPosition(), m_graphNodes[j].GetPosition()) <
                           m_NeighborhoodRadius;

            if (isVisible) // && isClose)
            {
                m_graphNodes[i].AddEdge(m_graphNodes[j]);
                m_graphNodes[j].AddEdge(m_graphNodes[i]);
            }
        }
    }

    public List<WayPoint> GetRoadMap()
    {
        return m_graphNodes;
    }


    public void OnDrawGizmos()
    {
        if (IsRenderVisibilityGraph)
            foreach (var spot in m_graphNodes)
            {
                Gizmos.DrawSphere(spot.GetPosition(), 0.1f);

                foreach (var node1 in spot.GetConnections())
                {
                    Gizmos.DrawLine(spot.GetPosition(), node1.GetPosition());
                }
            }
    }
}