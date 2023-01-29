using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloorTileManager : MonoBehaviour
{
    [SerializeField] 
    private bool isRender;

    // The color of the floor
    private Color32 m_MeshColor;

    // List of the materials of the nodes in the grid
    private List<Material> m_materials;

    // Gameobject for the walkable area mesh
    private List<GameObject> m_walkableAreaMeshes;

    // Area Mesh GameObject (for rendering)
    GameObject m_AreaMeshPrefab;

    string MeshPrefabAddress = "Prefabs/MeshArea";
    string PixelPath = "Sprites/white_pixel";

    // Start is called before the first frame update
    public void Initiate(List<MeshPolygon> navMesh)
    {
        m_AreaMeshPrefab = (GameObject) Resources.Load(MeshPrefabAddress);
    
        m_MeshColor = new Color32(180, 180, 180, 255);

        TileFloor(navMesh);
        // TileColoredFloor(navMesh);
    }


    // Tile the walkable area with meshes
    public void TileFloor(List<MeshPolygon> navMesh)
    {
        m_walkableAreaMeshes = new List<GameObject>();
        
        foreach (var p in navMesh)
        {
            GameObject areaMesh = Instantiate(m_AreaMeshPrefab, transform, true);
            areaMesh.GetComponent<AreaMesh>().Draw(GetVertices(p), m_MeshColor);
            areaMesh.GetComponent<Renderer>().sortingOrder = -2;
            m_walkableAreaMeshes.Add(areaMesh);
        }
    }

    // Tile the floor with each region of a different color
    private void TileColoredFloor(List<MeshPolygon> navMesh)
    {
        m_walkableAreaMeshes = new List<GameObject>();

        // The index of the current color to paint the floor with.
        int indexCurrentColor = 0;

        // The list of colors that mark the different region.
        List<Color32> regionColors = new List<Color32>();
        regionColors.Add(new Color32(255,180,180,255));
        regionColors.Add(new Color32(255,255,180,255));
        regionColors.Add(new Color32(255,180,255,255));
        regionColors.Add(new Color32(180,255,255,255));
        
        
        // Allocated area for the each color
        float colorArea = 0f;
        foreach (var p in navMesh) colorArea += p.GetArea();
        
        Queue<MeshPolygon> polygonsToColor = new Queue<MeshPolygon>();

        float totalColorArea = 0f;
        polygonsToColor.Enqueue(navMesh[0]);

        while (polygonsToColor.Count > 0)
        {
            MeshPolygon currentPoly = polygonsToColor.Dequeue();

            if (currentPoly.regionID != -1) continue;

            foreach (var neighbor in currentPoly.GetAdjcentPolygons())
            {
                if (neighbor.Value.regionID != -1) continue;

                polygonsToColor.Enqueue(neighbor.Value);
            }

            GameObject areaMesh = Instantiate(m_AreaMeshPrefab, transform, true);
            currentPoly.regionID = indexCurrentColor;
            areaMesh.GetComponent<AreaMesh>().Draw(GetVertices(currentPoly), regionColors[indexCurrentColor]);
            areaMesh.GetComponent<Renderer>().sortingOrder = -2;
            m_walkableAreaMeshes.Add(areaMesh);


            totalColorArea += currentPoly.GetArea();
            
            if (totalColorArea >= colorArea && indexCurrentColor < regionColors.Count - 1)
            {
                indexCurrentColor++;
                totalColorArea = 0f;
            }
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
        if (isRender)
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
        if (isRender)
        {
            DrawGrid(nodes);
        }
    }

    // Test code for drawing the transform distance
    public void DrawGrid(List<Node> nodes)
    {
        m_materials = new List<Material>();
        foreach (var node in nodes)
            InitiateNode(node.worldPosition, node.staleness);
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
        material.color = Properties.GetStalenessColor(n.staleness);
    }

    public void RenderGrid(List<Node> nodes)
    {
        if (isRender)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                RenderNode(nodes[i], m_materials[i]);
            }
        }
    }
}