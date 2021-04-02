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

        // IsRenderVisibilityGraph = true;

        // Create the nodes on the interior of the map for each point in the map.
        CreateNodes();

        //SimplifyNodes();

        ConnectNodes();

        RemoveReplicateEdges();
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
            float extension = 1f;

            // Extend the length of the line check by both sides
            WayPoint firstWp = m_graphNodes[i];
            WayPoint secWp = m_graphNodes[j];

            // Get the direction
            Vector2 direction = (firstWp.GetPosition() - secWp.GetPosition()).normalized;

            int firstPtWallId = -1;
            int secPtWallId = -1;

            // Check if the are mutually visible 
            RaycastHit2D hit = Physics2D.Linecast(firstWp.GetPosition(), secWp.GetPosition());

            // Draw a line from the first point to the other extended.
            RaycastHit2D hit1to2 =
                Physics2D.Linecast(firstWp.GetPosition(), secWp.GetPosition() + direction * extension);

            if (hit1to2)
                firstPtWallId = hit1to2.transform.gameObject.GetComponent<Wall>().WallId;


            // Draw a line from the opposite direction
            RaycastHit2D hit2to1 =
                Physics2D.Linecast(secWp.GetPosition(), firstWp.GetPosition() - direction * extension);

            if (hit2to1)
                secPtWallId = hit2to1.transform.gameObject.GetComponent<Wall>().WallId;


            bool isVisible = false;


            if (firstPtWallId == secPtWallId && (firstPtWallId == -1 || firstWp.WallId == 0)
            ) // If there are no intersections at all.
                isVisible = true;
            else if (firstPtWallId != secPtWallId)
            {
                if ((firstPtWallId == -1 || secPtWallId == -1) && firstWp.WallId == secWp.WallId)
                    isVisible = true;
            }


            // If the points are not mutually visible
            if (hit)
                isVisible = false;


            if (isVisible)
            {
                firstWp.Connect(secWp);
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
    
    private void RemoveReplicateEdges()
    {
        for (int i = 0; i < m_graphNodes.Count; i++)
        {
            WayPoint curWp = m_graphNodes[i];

            List<WayPoint> conns = curWp.GetConnections();

            for (int j = 0; j < conns.Count; j++)
            {
                WayPoint firstCon = conns[j];

                for (int k = j + 1; k < conns.Count; k++)
                {
                    WayPoint secCon = conns[k];

                    if (!firstCon.IsConnected(secCon))
                        continue;

                    float firstToSecDistance = Vector2.Distance(firstCon.GetPosition(), secCon.GetPosition());
                    float firstToCurDistance = Vector2.Distance(firstCon.GetPosition(), curWp.GetPosition());
                    float curToSecDistance = Vector2.Distance(curWp.GetPosition(), secCon.GetPosition());

                    WayPoint firstWp = null;
                    WayPoint secWp = null;

                    if (firstToSecDistance > firstToCurDistance)
                    {
                        if (firstToSecDistance > curToSecDistance)
                            firstWp = firstCon;
                        else
                            firstWp = curWp;

                        secWp = secCon;
                    }
                    else
                    {
                        if (firstToCurDistance > curToSecDistance)
                            firstWp = firstCon;
                        else
                            firstWp = secCon;

                        secWp = curWp;
                    }

                    firstWp.RemoveConnection(secWp);
                    j = 0;
                    break;
                }
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
        {
            Gizmos.color = Color.black;

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
}