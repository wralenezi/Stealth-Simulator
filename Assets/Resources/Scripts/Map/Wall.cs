using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wall : MonoBehaviour
{
    Vector2[] m_vertices;

    // Mesh for rendering the field of view
    MeshFilter m_viewMeshFilter;
    Mesh m_viewMesh;

    private float m_ColliderRadius = 0.07f; 

    EdgeCollider2D m_edgeCollider2D;
    
    // The ID of the wall
    public int WallId;

    private void Awake()
    {
        // Assign the references to the mesh
        m_viewMesh = new Mesh();
        m_viewMeshFilter = GetComponent<MeshFilter>();
        m_viewMeshFilter.mesh = m_viewMesh;


        gameObject.layer = LayerMask.NameToLayer("Wall");
        m_edgeCollider2D = GetComponent<EdgeCollider2D>(); 
        m_edgeCollider2D.edgeRadius = m_ColliderRadius;

        // Add the RigidBody
        Rigidbody2D rb = gameObject.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
    }

    Vector3[] ToVector3Array(Vector2[] v2)
    {
        return Array.ConvertAll(v2, GetV3fromV2);
    }

    Vector3 GetV3fromV2(Vector2 v2)
    {
        return v2;
    }

    public void Draw()
    {
        m_vertices = m_edgeCollider2D.points;

        // Prepare the variables to load the number of vertices and triangles that will make the visibility polygon
        Vector3[] vertices = ToVector3Array(m_vertices);
        int[] triangles;
        
        Polygon poly = new Polygon();
        
        for (int i = 0; i < vertices.Length; i++)
            poly.AddPoint(vertices[i]);
        
        poly.EnsureWindingOrder(Properties.outerPolygonWinding);
        
        triangles = EarClipDecomp.TriangulateIndex(poly).ToArray();
        
        // Assign the mesh values
        m_viewMesh.Clear();
        m_viewMesh.vertices = vertices;
        m_viewMesh.triangles = triangles;
        
        
        m_viewMesh.RecalculateNormals();
    }
}