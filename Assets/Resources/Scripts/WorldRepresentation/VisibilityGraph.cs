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

        IsRenderVisibilityGraph = true;

        // Create the nodes on the interior of the map for each point in the map.
        CreateNodes();

        //SimplifyNodes();

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
            angleNormal *= 0.3f;

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
            Vector2 direction = (m_graphNodes[j].GetPosition() - m_graphNodes[i].GetPosition()).normalized;


            float extension = 1f;

            // Extend the length of the line check by both sides
            Vector2 firstPoint = m_graphNodes[i].GetPosition();
            Vector2 secPoint = m_graphNodes[j].GetPosition();

            float distance = Vector2.Distance(firstPoint, secPoint);

            int firstPtWallId = -1;
            int secPtWallId = -1;

            // Check if the 
            RaycastHit2D hit = Physics2D.Linecast(firstPoint, secPoint);

            // Draw a line from the first point to the other extended.
            RaycastHit2D hit1to2 = Physics2D.Linecast(firstPoint, secPoint + direction * extension);


            if (hit1to2)
            {
                firstPtWallId = hit1to2.transform.gameObject.GetComponent<Wall>().WallId;
            }

            // Draw a line from the opposite direction
            RaycastHit2D hit2to1 = Physics2D.Linecast(secPoint, firstPoint - direction * extension);

            if (hit2to1)
            {
                secPtWallId = hit2to1.transform.gameObject.GetComponent<Wall>().WallId;
            }


            bool isClose = Vector2.Distance(m_graphNodes[i].GetPosition(), m_graphNodes[j].GetPosition()) <
                           m_NeighborhoodRadius;

            bool isVisible = false;

            if (firstPtWallId == secPtWallId && (firstPtWallId == -1 || m_graphNodes[i].WallId == 0)
            ) // If there are no intersections at all.
                isVisible = true;
            else if (firstPtWallId != secPtWallId)
            {
                if ((firstPtWallId == -1 || secPtWallId == -1) &&
                    m_graphNodes[i].WallId == m_graphNodes[j].WallId)
                    isVisible = true;
            }


            // If the points are not mutually visible
            if (hit)
                isVisible = false;


            if (isVisible)
            {
                m_graphNodes[i].AddEdge(m_graphNodes[j]);
                m_graphNodes[j].AddEdge(m_graphNodes[i]);
            }
        }
    }


    // Remove the direct edges of nodes that can be visited by another path
    private void RemoveRepetitiveEdges()
    {
        for (int i = 0; i < m_graphNodes.Count; i++)
        for (int j = i + 1; j < m_graphNodes.Count; j++)
        {

            for (int k = 0; k < m_graphNodes[i].GetConnections().Count; k++)
            {
                WayPoint conn = m_graphNodes[i].GetConnections()[k];
                
                // Search in the other direct connections if there is a connection that leads to it; a -> c, a -> b find if there is b -> c if so remove a -> c. 
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