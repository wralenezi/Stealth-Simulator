using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class AreaMesh : MonoBehaviour
{
    // Mesh for rendering the field of view
    private MeshFilter m_viewMeshFilter;
    private Mesh m_viewMesh;
    private MeshRenderer m_meshRenderer;

    public float m_staleness;

    // Start is called before the first frame update
    void Awake()
    {
        // Assign the references to the mesh
        m_viewMesh = new Mesh();
        m_viewMeshFilter = GetComponent<MeshFilter>();
        m_meshRenderer = GetComponent<MeshRenderer>();
        m_viewMeshFilter.mesh = m_viewMesh;
        GetComponent<Renderer>().sortingOrder = 1;
    }

    Vector3[] ToVector3Array(Vector2[] v2)
    {
        return System.Array.ConvertAll(v2, GetV3fromV2);
    }

    Vector3 GetV3fromV2(Vector2 v2)
    {
        return v2;
    }

    public void Draw(Vector2[] meshVertices, float staleness)
    {
        m_staleness = staleness;
        // Prepare the variables to load the number of vertices and triangles that will make the visibility polygon
        Vector3[] vertices = ToVector3Array(meshVertices);

        Polygon nodes = new Polygon();

        for (int i = 0; i < vertices.Length; i++)
            nodes.AddPoint(vertices[i]);

        var triangles = EarClipDecomp.TriangulateIndex(nodes).ToArray();

        // Assign the mesh values
        m_viewMesh.Clear();
        m_viewMesh.vertices = vertices;
        m_viewMesh.triangles = triangles;
        m_viewMesh.RecalculateNormals();

        Color color = Properties.GetStalenessColor(m_staleness);
        m_meshRenderer.material.color = color;
    }

    public void Draw(Vector2[] meshVertices)
    {
        // Prepare the variables to load the number of vertices and triangles that will make the visibility polygon
        Vector3[] vertices = ToVector3Array(meshVertices);
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
        m_meshRenderer.material.color = Color.white;

        m_viewMesh.RecalculateNormals();
    }
    
}