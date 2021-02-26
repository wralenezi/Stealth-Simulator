using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;
using UnityEngine.Rendering;
using Vector3 = UnityEngine.Vector3;

public class FieldOfView : MonoBehaviour
{
    // 
    private int edgeResolveIterations = 1;
    private float edgeDstThreshold = 100f;

    private int meshResolution = 1;

    private float m_ViewAngle;
    private float m_ViewRadius;

    // Mesh for rendering the field of view
    MeshFilter m_ViewMeshFilter;
    Mesh m_ViewMesh;

    // Vertices of the field of view mesh
    private List<Vector3> m_ViewPoints = new List<Vector3>();

    private List<Vector3> m_Vertices = new List<Vector3>();
    private List<int> m_Triangles = new List<int>();

    // Obstacle Layer
    private LayerMask m_ObstacleMask;


    // Initiate Vision
    public void Initiate(float viewAngle, float viewRadius, Color viewConeColor)
    {
        // Assign the references to the mesh
        m_ViewMesh = new Mesh();
        m_ViewMeshFilter = gameObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
        var material = new Material(Shader.Find("Sprites/Default")) {color = viewConeColor};
        material.renderQueue = RenderQueue.Geometry.GetHashCode();
        meshRenderer.material = material;

        m_ViewMeshFilter.mesh = m_ViewMesh;

        // Set the layer to intersect vision
        m_ObstacleMask = LayerMask.GetMask("Wall");

        m_ViewAngle = viewAngle;
        m_ViewRadius = viewRadius;
    }

    // Cast the Vision
    public void CastFieldOfView()
    {
        // The list of points the ray either hit or reach
        m_ViewPoints.Clear();

        ViewCastInfo oldViewCast = new ViewCastInfo();

        // NPC position and Y-rotation
        var npcTransform = transform;
        Vector3 source = npcTransform.position;
        float yRotation = -npcTransform.eulerAngles.z;

        m_ViewPoints.Add(source);

        // Calculate field of view
        for (int i = 0; i <= Mathf.RoundToInt(m_ViewAngle * meshResolution); i++)
        {
            float angle = yRotation - m_ViewAngle / 2 +
                          (m_ViewAngle / Mathf.RoundToInt(m_ViewAngle * meshResolution)) * i;

            ViewCastInfo newViewCast = ViewCast(source, angle, m_ViewRadius);

            if (i > 0)
            {
                if (oldViewCast.hit != newViewCast.hit ||
                    (oldViewCast.hit && newViewCast.hit &&
                     Mathf.Abs(oldViewCast.distance - newViewCast.distance) > edgeDstThreshold))
                {
                    EdgeInfo edge = FindEdge(source, oldViewCast, newViewCast, m_ViewRadius);
                    if (edge.PointA != Vector3.zero)
                    {
                        m_ViewPoints.Add(edge.PointA);
                    }

                    if (edge.PointB != Vector3.zero)
                    {
                        m_ViewPoints.Add(edge.PointB);
                    }
                }
            }


            m_ViewPoints.Add(newViewCast.point);
            oldViewCast = newViewCast;
        }

        // Draw mesh
        int vertexCount = m_ViewPoints.Count;
        m_Vertices.Clear();
        m_Triangles.Clear();
        
        m_Vertices.Add(Vector3.zero);
        for (int i = 0; i < vertexCount - 1; i++)
        {
            m_Vertices.Add(transform.InverseTransformPoint(m_ViewPoints[i]));

            if (i < vertexCount - 2)
            {
                m_Triangles.Add(0);
                m_Triangles.Add(i + 1);
                m_Triangles.Add(i + 2);
            }
        }


        m_ViewMesh.Clear();
        m_ViewMesh.SetVertices(m_Vertices);
        m_ViewMesh.SetTriangles(m_Triangles, 0);
        m_ViewMesh.RecalculateNormals();
    }

    public List<Vector3> GetFovVertices()
    {
        return m_ViewPoints;
    }

    // Get the direction from the angle
    private Vector3 DirFromAngle(float angleInDegrees)
    {
        return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
    }

    // Shoot the ray
    ViewCastInfo ViewCast(Vector3 source, float globalAngle, float viewRadius)
    {
        Vector3 dir = DirFromAngle(globalAngle);

        Physics.autoSyncTransforms = false;

        RaycastHit2D hit = Physics2D.Raycast(source, dir, viewRadius, m_ObstacleMask);
        if (hit)
        {
            Physics.autoSyncTransforms = true;
            return new ViewCastInfo(true, hit.point, hit.distance, globalAngle);
        }

        // In case the ray was not obstructed
        Physics.autoSyncTransforms = true;
        return new ViewCastInfo(false, source + dir * viewRadius, viewRadius, globalAngle);
    }

    EdgeInfo FindEdge(Vector3 source, ViewCastInfo minViewCast, ViewCastInfo maxViewCast, float viewRadius)
    {
        float minAngle = minViewCast.angle;
        float maxAngle = maxViewCast.angle;
        Vector3 minPoint = Vector3.zero;
        Vector3 maxPoint = Vector3.zero;

        for (int i = 0; i < edgeResolveIterations; i++)
        {
            float angle = (minAngle + maxAngle) / 2;
            ViewCastInfo newViewCast = ViewCast(source, angle, viewRadius);

            bool edgeDstThresholdExceeded = Mathf.Abs(minViewCast.distance - newViewCast.distance) > edgeDstThreshold;
            if (newViewCast.hit == minViewCast.hit && !edgeDstThresholdExceeded)
            {
                minAngle = angle;
                minPoint = newViewCast.point;
            }
            else
            {
                maxAngle = angle;
                maxPoint = newViewCast.point;
            }
        }

        return new EdgeInfo(minPoint, maxPoint);
    }
}

// Information about the edge
public struct EdgeInfo
{
    public Vector3 PointA;
    public Vector3 PointB;

    public EdgeInfo(Vector3 pointA, Vector3 pointB)
    {
        PointA = pointA;
        PointB = pointB;
    }
}


// Contains information about the result of ray cast behaviour.
public struct ViewCastInfo
{
    public bool hit;
    public Vector3 point;
    public float distance;
    public float angle;

    public ViewCastInfo(bool hit, Vector3 point, float distance, float angle)
    {
        this.hit = hit;
        this.point = point;
        this.distance = distance;
        this.angle = angle;
    }
}