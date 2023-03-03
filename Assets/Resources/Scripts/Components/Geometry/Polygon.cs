using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Vector2 = UnityEngine.Vector2;

public class Polygon
{
    // Polygon vertices
    private CyclicalList<Vertex> m_Vertices;

    public Polygon()
    {
        m_Vertices = new CyclicalList<Vertex>();
    }

    public Polygon(Polygon _polygon)
    {
        m_Vertices = new CyclicalList<Vertex>();

        foreach (var vertex in _polygon.m_Vertices)
        {
            AddPoint(vertex.position);
        }
    }

    public void Clear()
    {
        m_Vertices.Clear();
    }

    // Add a vertex to the polygon
    public void AddPoint(Vector2 point)
    {
        m_Vertices.Add(new Vertex(point, GetVerticesCount()));
    }

    public Vector2 GetPoint(int i)
    {
        return m_Vertices[i].position;
    }

    public Vector2 GetCorner(int i, float distance = 1f)
    {
        bool isConvex = GeometryHelper.IsReflex(GetPoint(i - 1), GetPoint(i), GetPoint(i + 1));

        Vector2 normal = GeometryHelper.GetNormal(GetPoint(i - 1), GetPoint(i), GetPoint(i + 1));

        normal *= isConvex ? -1 : 1;
        
        Vector2 spotPosition = GetPoint(i) + normal * distance;
        
        return spotPosition;
    }


    public Vector2 GetAngelNormal(int i)
    {
        return GeometryHelper.GetNormal(GetPoint(i - 1), GetPoint(i), GetPoint(i + 1));
    }

    public void RemovePoint(int i)
    {
        m_Vertices.RemoveAt(i);
    }

    public Vertex GetVertex(int i)
    {
        return m_Vertices[i];
    }

    // Get the list of vertices of the polygon
    public CyclicalList<Vertex> GetPoints()
    {
        return m_Vertices;
    }

    // Get the number of the vertices of the polygon
    public int GetVerticesCount()
    {
        return m_Vertices.Count;
    }


    public float GetArea()
    {
        return Mathf.Abs(GetSignedArea());
    }


    // Calculate the area of the polygon. If the area is negative then the polygon is counterclockwise
    float GetSignedArea()
    {
        // Get the areas.
        float area = 0f;
        
        if (GetVerticesCount() <= 0) return area;
        for (int i = 0; i < GetVerticesCount(); i++)
        {
            area += (GetPoint(i + 1).x - GetPoint(i).x) *
                (GetPoint(i + 1).y + GetPoint(i).y) / 2f;
        }

        // Return the result.
        return area;
    }

    // Determine the winding order 
    public WindingOrder DetermineWindingOrder()
    {
        return GetSignedArea() < 0 ? WindingOrder.CounterClockwise : WindingOrder.Clockwise;
    }

    // Ensures that a set of vertices are wound in the desired winding order
    public void EnsureWindingOrder(WindingOrder windingOrder)
    {
        if (!DetermineWindingOrder().Equals(windingOrder))
        {
            ReverseWindingOrder();
        }
    }

    // Reverses the winding order for the polygon vertices.
    public void ReverseWindingOrder()
    {
        CyclicalList<Vertex> reverseVertices = new CyclicalList<Vertex>();

        int index = 0;
        for (int i = GetVerticesCount() - 1; i >= 0; i--)
            reverseVertices.Add(new Vertex(GetPoint(i), index++));

        m_Vertices = reverseVertices;
    }

    // Smooth the polygon
    public void SmoothPolygon(float minDistance)
    {
        float minAngle = Properties.MinAngle;
        float maxAngle = Properties.MaxAngle;

        int index = 0;
        while (index < m_Vertices.Count)
        {
            if(m_Vertices.Count < 4) break;
            
            // Remove the vertices if its angle is below the min threshold or more than the max threshold
            if (GeometryHelper.GetAngle(m_Vertices[index - 1].position, m_Vertices[index].position,
                m_Vertices[index + 1].position) >= maxAngle)
            {
                m_Vertices.RemoveAt(index);
                index = 0;
            }

            if (GeometryHelper.GetAngle(m_Vertices[index - 1].position, m_Vertices[index].position,
                m_Vertices[index + 1].position) <= minAngle)
            {
                m_Vertices.RemoveAt(index);
                index = 0;
            }
            else if (Vector2.Distance(m_Vertices[index - 1].position, m_Vertices[index + 1].position) <=
                     minDistance)
            {
                m_Vertices.RemoveAt(index);
                index = 0;
            }
            else
                index++;
        }
    }


    // Get the bounding box of polygon
    public Bounds BoundingBox()
    {
        float minX = Mathf.Infinity;
        float minY = Mathf.Infinity;
        float maxX = Mathf.NegativeInfinity;
        float maxY = Mathf.NegativeInfinity;

        for (int i = 0; i < GetVerticesCount(); i++)
        {
            Vector2 p = GetPoint(i);
            if (minX > p.x)
                minX = p.x;
            if (maxX < p.x)
                maxX = p.x;
            if (minY > p.y)
                minY = p.y;
            if (maxY < p.y)
                maxY = p.y;
        }

        Bounds bounds = new Bounds {min = new Vector3(minX, minY), max = new Vector3(maxX, maxY)};

        return bounds;
    }

    // Check if a point is in polygon 
    public bool IsPointInPolygon(Vector2 point, bool includeBorders)
    {
        bool inside = false;
        for (int i = 0, j = GetVerticesCount() - 1; i < GetVerticesCount(); j = i++)
        {
            if (includeBorders)
                if (point.Equals(GetPoint(i)))
                {
                    return true;
                }

            if ((GetPoint(i).y > point.y) != (GetPoint(j).y > point.y) && point.x <
                (GetPoint(j).x - GetPoint(i).x) * (point.y - GetPoint(i).y) / (GetPoint(j).y - GetPoint(i).y) +
                GetPoint(i).x)
            {
                inside = !inside;
            }
        }

        return inside;
    }

    // Check if a circle colliding with the polygon or inside it
    public bool IsCircleInPolygon(Vector2 center, float radius)
    {
        if (IsPointInPolygon(center, true))
            return true;

        for (int i = 0; i < GetVerticesCount(); i++)
        {
            Vector2 projection = GeometryHelper.ClosestProjectionOnSegment(GetPoint(i), GetPoint(i + 1), center);
            float distance = Vector2.Distance(center, projection);

            if (distance <= radius)
                return true;
        }

        return false;
    }


    // Check if a circle colliding with the polygon or inside it
    public bool IsCircleContainedInPolygon(Vector2 center, float radius)
    {
        bool isCenterIn = IsPointInPolygon(center, false);

        if (!isCenterIn) return false;

        for (int i = 0; i < GetVerticesCount(); i++)
        {
            Vector2 projection = GeometryHelper.ClosestProjectionOnSegment(GetPoint(i), GetPoint(i + 1), center);
            float distance = Vector2.Distance(center, projection);

            if (distance <= radius) return false;
        }

        return true;
    }


    // Get a random position inside the polygon
    public Vector2 GetRandomPosition()
    {
        Bounds bounds = BoundingBox();

        while (true)
        {
            float xPos = Random.Range(bounds.min.x, bounds.max.x);
            float yPos = Random.Range(bounds.min.y, bounds.max.y);

            Vector2 possiblePoint = new Vector2(xPos, yPos);

            if (IsPointInPolygon(possiblePoint, false)) return possiblePoint;
        }
    }

    public bool IsPolygonInside(Polygon inPoly, bool includeBorder)
    {
        for (int i = 0; i < inPoly.GetVerticesCount(); i++)
        {
            if (!IsPointInPolygon(inPoly.GetPoint(i), includeBorder))
                return false;
        }

        return true;
    }

    // Check if a line intersect a polygon and return the intersection position if exist along with a flag if the first point is outside the polygon 
    public List<Vector2> GetIntersectionWithLine(Vector2 p1, Vector2 p2, out bool isP1in, out bool isP2in)
    {
        isP1in = IsPointInPolygon(p1, false);

        isP2in = IsPointInPolygon(p2, false);

        List<Vector2> intersections = new List<Vector2>();

        if (isP1in && isP2in)
            return intersections;

        // Loop through the edges to find intersections
        for (int i = 0; i < GetVerticesCount(); i++)
        {
            Vector2 intersectionPoint =
                GeometryHelper.GetIntersectionPointCoordinates(p1, p2, GetPoint(i), GetPoint(i + 1), false,
                    out var isFound);

            if (isFound)
                intersections.Add(intersectionPoint);
        }


        return intersections;
    }


    // Check intersection polygon and the line of a point and centroid.
    public Vector2 GetClosestIntersectionPoint(Vector2 point)
    {
        return GetIntersectionWithLine(GetCentroidPosition(), point, out bool isP1In, out bool isP2In)[0];
    }


    // Draw the polygon
    public virtual void Draw(string label)
    {
        for (int i = 0; i < GetVerticesCount(); i++)
        {
            Gizmos.DrawLine(GetPoint(i), GetPoint(i + 1));
            // Handles.Label(GetPoint(i), i.ToString());
        }
#if UNITY_EDITOR
        Handles.Label(GetCentroidPosition(), label);
#endif
    }

    // Get the centroid position of the polygon
    public Vector2 GetCentroidPosition()
    {
        float x = 0f;
        float y = 0f;

        foreach (Vertex v in m_Vertices)
        {
            x += v.position.x;
            y += v.position.y;
        }

        return new Vector2(x / m_Vertices.Count, y / m_Vertices.Count);
    }

    // Enlarge or shrink the polygon based on its Winding order
    public void Enlarge(float displacementAmount)
    {
        List<Vector2> displacementDirections = new List<Vector2>();

        for (int i = 0; i < GetVerticesCount(); i++)
        {
            displacementDirections.Add(GeometryHelper.GetNormal(GetPoint(i - 1), GetPoint(i), GetPoint(i + 1)));
        }

        for (int i = 0; i < GetVerticesCount(); i++)
        {
            if (GeometryHelper.IsReflex(GetPoint(i + 1), GetPoint(i), GetPoint(i - 1)))
                m_Vertices[i].position += displacementDirections[i] * displacementAmount;
            else
                m_Vertices[i].position -= displacementDirections[i] * displacementAmount;
        }
    }
}

// Winding order of a polygon vertices
public enum WindingOrder
{
    Clockwise,
    CounterClockwise
}