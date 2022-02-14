using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class RegionLabelsManager : MonoBehaviour
{
    // Label to show the regions radius
    public bool ShowRegionAreas;

    // Regions
    private static List<Region> m_Regions;

    public void Initiate()
    {
        m_Regions = new List<Region>();
        // ShowRegionAreas = true;
    }

    public void SetRegions(MapData map)
    {
        while (m_Regions.Count > 0)
        {
            Region region = m_Regions[0];
            Destroy(region.gameObject);
            m_Regions.RemoveAt(0);
        }

        if (Equals(map.name, "amongUs")) CreateRegionsForAmongUs();
    }

    private void CreateRegionsForAmongUs()
    {
        float labelSize = 6.5f;
        m_Regions.Add(new Region(new Vector2(-26.17f, 0.4f), 4.5f, "Reactor", labelSize));
        m_Regions.Add(new Region(new Vector2(-19.42f, 7.31f), 3.5f, "Upper Engine", labelSize));
        m_Regions.Add(new Region(new Vector2(-20.08f, -6.37f), 3.5f, "Lower Engine", labelSize));
        m_Regions.Add(new Region(new Vector2(-12.18f, 1.73f), 2.5f, "Medbay", labelSize));
        m_Regions.Add(new Region(new Vector2(-17.5f, 0.72f), 3f, "Security", labelSize));
        m_Regions.Add(new Region(new Vector2(-12.55f, -3.62f), 2.5f, "Electrical", labelSize));
        m_Regions.Add(new Region(new Vector2(-6.49f, -8.5f), 5f, "Storage", labelSize));
        m_Regions.Add(new Region(new Vector2(-5.73f, 7.75f), 6f, "Cafeteria", labelSize));
        m_Regions.Add(new Region(new Vector2(3.34f, 8.07f), 3f, "Weapons", labelSize));
        m_Regions.Add(new Region(new Vector2(-1.27f, -2.55f), 2.5f, "Admin", labelSize));
        m_Regions.Add(new Region(new Vector2(5.1f, -11.04f), 3f, "Communications", labelSize));
        m_Regions.Add(new Region(new Vector2(-0.01f, 2.15f), 2f, "O2", labelSize));
        m_Regions.Add(new Region(new Vector2(11.11f, 2.38f), 3f, "Navigation", labelSize));
        m_Regions.Add(new Region(new Vector2(11.83f, -4.71f), 3f, "Shields", labelSize));

        // For a map scale of 1f
        // m_Regions.Add(new Region(new Vector2(-52.73f, 1.1f), 9f, "Reactor", labelSize));
        // m_Regions.Add(new Region(new Vector2(-39.18f, 14.66f), 6f, "Upper Engine", labelSize));
        // m_Regions.Add(new Region(new Vector2(-40.53f, -13.03f), 6f, "Lower Engine", labelSize));
        // m_Regions.Add(new Region(new Vector2(-23.9f, 3.69f), 4.5f, "Medbay", labelSize));
        // m_Regions.Add(new Region(new Vector2(-34.5f, 1.5f), 6f, "Security", labelSize));
        // m_Regions.Add(new Region(new Vector2(-25f, -7.24f), 5f, "Electrical", labelSize));
        // m_Regions.Add(new Region(new Vector2(-13f, -16f), 9.5f, "Storage", labelSize));
        // m_Regions.Add(new Region(new Vector2(-11.4f, 15.2f), 11.5f, "Cafeteria", labelSize));
        // m_Regions.Add(new Region(new Vector2(6.67f, 16f), 6.5f, "Weapons", labelSize));
        // m_Regions.Add(new Region(new Vector2(-2.41f, -4.77f), 5f, "Admin", labelSize));
        // m_Regions.Add(new Region(new Vector2(10.01f, -22.09f), 6.5f, "Communications", labelSize));
        // m_Regions.Add(new Region(new Vector2(-0.05f, 4.13f), 4f, "O2", labelSize));
        // m_Regions.Add(new Region(new Vector2(22.11f, 4.76f), 6.5f, "Navigation", labelSize));
        // m_Regions.Add(new Region(new Vector2(23.81f, -9.31f), 6.5f, "Shields", labelSize));


        foreach (var region in m_Regions)
            CreateRegion(region);
    }

    private void CreateRegion(Region _region)
    {
        GameObject regionLabelOG = new GameObject(_region.name);

        regionLabelOG.transform.position = _region.position;
        regionLabelOG.transform.parent = transform;

        _region.gameObject = regionLabelOG;
        _region.AddTextMesh(regionLabelOG.AddComponent<TextMeshPro>());
    }

    public static void SetRegions(NPC npc)
    {
        List<Vector2> path = npc.GetPath();

        string startRegion = GetRegion(npc.GetTransform().position);

        string goalRegion = !Equals(npc.GetGoal(), null) ? GetRegion(npc.GetGoal().Value) : WorldState.EMPTY_VALUE;

        // Get in between regions
        string middleRegion = WorldState.EMPTY_VALUE;
        foreach (var point in path)
        {
            string region = GetRegion(point);
            if (!Equals(region, WorldState.EMPTY_VALUE) && !Equals(goalRegion, region) && !Equals(startRegion, region))
            {
                middleRegion = region;
                break;
            }
        }

        // Get the normalized remaining distance to the goal
        float normalizedDistance = npc.GetRemainingDistanceToGoal() / PathFinding.Instance.longestPath;
        WorldState.Set(npc.name + "_path_distance", normalizedDistance.ToString());
        // Mark the regions the npc will pass through
        WorldState.Set(npc.name + "_start_region", startRegion);
        WorldState.Set(npc.name + "_middle_region", middleRegion);
        WorldState.Set(npc.name + "_goal_region", goalRegion);
    }

    public static string GetRegion(Vector2 position)
    {
        foreach (var region in m_Regions)
        {
            string output = region.GetClosestRegion(position);

            if (!Equals(output, WorldState.EMPTY_VALUE))
                return output;
        }

        return WorldState.EMPTY_VALUE;
    }


    private void OnDrawGizmos()
    {
        if (ShowRegionAreas && !Equals(m_Regions, null))
        {
            Gizmos.color = Color.yellow;
            foreach (var region in m_Regions)
                Gizmos.DrawWireSphere(region.position, region.radius);
        }
    }
}

// A region that is a circle assigned a name 
public class Region
{
    // the center of the region
    public Vector2 position;

    // Radius of the region
    public float radius;

    // Name of the region
    public string name;

    public GameObject gameObject;

    private TextMeshPro m_textMesh;

    // Label size
    private float m_labelSize;

    public Region(Vector2 _position, float _radius, string _name, float _labelSize)
    {
        position = _position;
        radius = _radius;
        name = _name;
        m_labelSize = _labelSize;
    }

    public string GetClosestRegion(Vector2 _position)
    {
        float distance = Vector2.Distance(position, _position);

        return distance <= radius ? name : WorldState.EMPTY_VALUE;
    }

    public void AddTextMesh(TextMeshPro _textMesh)
    {
        m_textMesh = _textMesh;
        SetUpTextMesh();
    }

    private void SetUpTextMesh()
    {
        m_textMesh.text = name;
        m_textMesh.color = Color.black;
        m_textMesh.fontWeight = FontWeight.Bold;
        m_textMesh.alignment = TextAlignmentOptions.Center;
        m_textMesh.alignment = TextAlignmentOptions.Midline;
        m_textMesh.fontSize = m_labelSize;
    }
}