using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshManager : MonoBehaviour
{
    private GameManager m_gameManager;
    
    // List of the materials of the nodes in the grid
    private List<Material> m_materials;

    // Area Mesh GameObject (for rendering)
    GameObject m_AreaMeshPrefab;

    string MeshPrefabAddress = "Prefabs/MeshArea";
    string PixelPath = "Sprites/white_pixel";

    // Start is called before the first frame update
    public void Initiate()
    {
        m_AreaMeshPrefab = (GameObject) Resources.Load(MeshPrefabAddress);
        m_gameManager = transform.parent.parent.GetComponent<GameManager>();
    }

    public void ClearMeshes()
    {
        foreach (Transform mesh in transform)
            Destroy(mesh.gameObject);
    }

    // Create the mesh, and color it based on seen and un seen
    private void CreateMesh(Polygon polygon, float staleness)
    {
        // Instantiate the Mesh game object
        GameObject areaMesh = Instantiate(m_AreaMeshPrefab, transform, true);
        areaMesh.GetComponent<AreaMesh>().Draw(GetVertices(polygon), staleness);
    }

    // Get the overall seen area vertices to render its mesh
    Vector2[] GetVertices(Polygon polygon)
    {
        Vector2[] result = new Vector2[polygon.GetVerticesCount()];
    
        for (int i = 0; i < polygon.GetVerticesCount(); i++)
            result[i] = polygon.GetPoint(i);
        
        return result;
    }


    public void RenderVisibilityMesh(List<VisibilityPolygon> visibilityMesh)
    {
        if (m_gameManager.Render)
        {
            ClearMeshes();
            foreach (var polygon in visibilityMesh)
            {
                polygon.EnsureWindingOrder(Properties.outerPolygonWinding);
                CreateMesh(polygon, polygon.GetStaleness());
            }
        }
    }


    public void InitializeGrid(List<Node> nodes)
    {
        if (m_gameManager.Render)
        {
            m_materials = new List<Material>();
            foreach (var node in nodes)
                InitiateNode(node);
        }
    }

    public void InitiateNode(Node n)
    {
        var sphere = new GameObject();
        var spriteRenderer = sphere.AddComponent<SpriteRenderer>();
        var sprite = Resources.Load<Sprite>(PixelPath);
        spriteRenderer.sprite = sprite;
        sphere.transform.parent = transform;
        sphere.transform.position = n.worldPosition;
        float diameter = Properties.NodeRadius * 200f;
        sphere.transform.localScale =
            new Vector3(diameter, diameter, diameter);

        Material material = sphere.GetComponent<Renderer>().material;
        
        m_materials.Add(material);
        material.color = Properties.GetStalenessColor(n.GetStaleness());
    }


    private void RenderNode(Node n,Material material)
    {
        material.color = Properties.GetStalenessColor(n.GetStaleness());
    }

    public void RenderGrid(List<Node> nodes)
    {
        if (m_gameManager.Render)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                RenderNode(nodes[i],m_materials[i]);
            }
        }
    }
}