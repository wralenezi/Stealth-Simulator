using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshManager : MonoBehaviour
{
    private GameManager m_gameManager;

    private StealthArea m_StealthArea;

    // List of the materials of the nodes in the grid
    private List<Material> m_materials;

    // Gameobject for the walkable area mesh
    private List<GameObject> m_walkableAreaMeshes;

    // Area Mesh GameObject (for rendering)
    GameObject m_AreaMeshPrefab;

    string MeshPrefabAddress = "Prefabs/MeshArea";
    string PixelPath = "Sprites/white_pixel";

    // Start is called before the first frame update
    public void Initiate(StealthArea stealthArea)
    {
        m_AreaMeshPrefab = (GameObject) Resources.Load(MeshPrefabAddress);
        m_gameManager = transform.parent.parent.GetComponent<GameManager>();
        m_StealthArea = stealthArea;
        TileFloor(m_StealthArea.mapRenderer.GetWalls());
    }


    // Tile the walkable area with meshes
    public void TileFloor(List<Polygon> walls)
    {
        m_walkableAreaMeshes = new List<GameObject>();
        
        // Cut the holes in the map
        Polygon simplePolygon = PolygonHelper.CutHoles(walls);

        // Decompose Space
        List<MeshPolygon> convexPolys = HertelMelDecomp.ConvexPartition(simplePolygon);
        
        foreach (var p in convexPolys)
        {
            GameObject areaMesh = Instantiate(m_AreaMeshPrefab, transform, true);
            areaMesh.GetComponent<AreaMesh>().Draw(GetVertices(p));
            areaMesh.GetComponent<Renderer>().sortingOrder = -2;
            m_walkableAreaMeshes.Add(areaMesh);
        }
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

    // Draw the grid
    public void InitializeGrid(List<Node> nodes)
    {
        if (m_gameManager.Render)
        {
            DrawGrid(nodes);
        }
    }

    // Test code for drawing the transform distance
    public void DrawGrid(List<Node> nodes)
    {
        m_materials = new List<Material>();
        foreach (var node in nodes)
            InitiateNode(node.worldPosition, node.GetStaleness());
    }

    public void DrawGrid(DtNode[,] nodes)
    {
        m_materials = new List<Material>();
        for (int x = 0; x < nodes.GetLength(0); x++)
        for (int y = 0; y < nodes.GetLength(1); y++)
        {
            if (nodes[x, y].code != '0')
                InitiateNode(nodes[x, y].worldPosition, nodes[x, y].distanceTransform);
        }
    }


    public void InitiateNode(Vector2 position, float value)
    {
        var sphere = new GameObject();
        var spriteRenderer = sphere.AddComponent<SpriteRenderer>();
        var sprite = Resources.Load<Sprite>(PixelPath);
        spriteRenderer.sprite = sprite;
        sphere.transform.parent = transform;
        sphere.transform.position = position;
        float diameter = Properties.NodeRadius * 200f;
        sphere.transform.localScale =
            new Vector3(diameter, diameter, diameter);

        Material material = sphere.GetComponent<Renderer>().material;

        m_materials.Add(material);
        material.color = Properties.GetStalenessColor(value);
    }


    private void RenderNode(Node n, Material material)
    {
        material.color = Properties.GetStalenessColor(n.GetStaleness());
    }

    public void RenderGrid(List<Node> nodes)
    {
        if (m_gameManager.Render)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                RenderNode(nodes[i], m_materials[i]);
            }
        }
    }
}