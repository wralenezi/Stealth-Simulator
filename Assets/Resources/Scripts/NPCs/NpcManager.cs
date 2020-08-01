using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NpcManager : MonoBehaviour
{
    // List of Guards
    private List<Guard> m_Guards;

    // List of Intruders
    private List<Intruder> m_Intruders;

    // Logging manager
    private PerformanceMonitor m_performanceMonitor;
    
    // The space filler for the pursuit behavior
    private SpaceFiller m_SpaceFiller;


    private LayerMask m_npcLayer;

    public void Initiate()
    {
        m_Guards = new List<Guard>();
        m_Intruders = new List<Intruder>();
        m_SpaceFiller = transform.parent.Find("Map").GetComponent<SpaceFiller>();
        m_performanceMonitor = transform.parent.Find("Map").GetComponent<PerformanceMonitor>();
        m_performanceMonitor.SetArea();
        m_performanceMonitor.ResetResults();

        if (m_performanceMonitor.IsDone())
            Destroy(transform.parent.gameObject);

        m_npcLayer = LayerMask.NameToLayer("NPC");

        Physics2D.IgnoreLayerCollision(m_npcLayer, m_npcLayer);
    }

    // Create the NPCs of the scenario
    // Param: npcsData - List of the NPCdata
    // Param: navMesh - List of polygons the NPCs will spawn on
    // Param: Area -  a reference to the main script of the instance
    public void CreateNpcs(Scenario scenario, List<MeshPolygon> navMesh, StealthArea area)
    {
        foreach (var npcData in scenario.GetNpcsData())
            CreateNpc(npcData, scenario.worldRepType, navMesh, area);
    }

    // Update the guards FoV
    public void UpdateGuardVision()
    {
        // In the case of searching for an intruder
        UpdateSearchArea();

        bool intruderSpotted = false;
        foreach (var guard in m_Guards)
        {
            // Cast the guard field of view
            guard.CastVision();

            // Accumulate the Seen Area of the guard
            guard.AccumulateSeenArea();

            // Check if any intruders are spotted
            if (!intruderSpotted)
                intruderSpotted = guard.SpotIntruders(m_Intruders);

            // // Modify the search area if an intruder is seen
            // guard.RestrictSearchArea();
        }

        // Guards knows the intruders location
        if (intruderSpotted)
            InitiateAlert();
        else
            //
            StartSearch();
    }


    // In case of intruder is seen
    public void InitiateAlert()
    {
        foreach (var guard in m_Guards)
        {
            guard.UpdateChasingTarget(m_Intruders[0].transform.position);
        }
    }

    // In case the intruder is not seen and the guards were on alert, start the search or keep doing it.
    public void StartSearch()
    {
        // Start the search region as a circle with the intruders position as its center.
        foreach (var guard in m_Guards)
        {
            if (!m_SpaceFiller.IsSearchActive() && guard.GetState() is Chase)
            {
                m_SpaceFiller.CreateExpandingCircle(m_Intruders[0].transform.position, m_Guards);
                break;
            }
        }

        foreach (var guard in m_Guards)
        {
            guard.StartSearch();
        }
    }


    // Update the search area in case the guards are searching for an intruder
    public void UpdateSearchArea()
    {
        m_SpaceFiller.Expand();
        m_SpaceFiller.Restrict(m_Guards);
    }

    public void AddInterceptionPoints()
    {
        m_SpaceFiller.CreateTargetPoints();
    }


    // NPCs decide plans if idle
    public void MakeDecision()
    {
        foreach (var guard in m_Guards)
            guard.AssignGoal();

        foreach (var intruder in m_Intruders)
            intruder.RequestDecision();
    }

    // Execute NPCs plans
    public void MoveNpcs()
    {
        foreach (var guard in m_Guards)
            guard.ExecutePlan();
    }

    // Create an NPC
    private void CreateNpc(NpcData npcData, WorldRepType world, List<MeshPolygon> navMesh, StealthArea area)
    {
        // Create the gameObject 
        GameObject npcPrefab;
        switch (npcData.npcType)
        {
            case NpcType.Intruder:
                npcPrefab = Resources.Load("Prefabs/NPCs/Intruder") as GameObject;
                break;

            case NpcType.Guard:
                if (world == WorldRepType.Grid)
                    npcPrefab = Resources.Load("Prefabs/NPCs/GridGuard") as GameObject;
                else
                    npcPrefab = Resources.Load("Prefabs/NPCs/VisMeshGuard") as GameObject;
                break;

            default:
                npcPrefab = Resources.Load("Prefabs/NPCs/Intruder") as GameObject;
                break;
        }

        // Set the NPC as a child to the manager
        var npcGameObject = Instantiate(npcPrefab, transform);

        // Randomly place the NPC on the map
        int polygonIndex = Random.Range(0, navMesh.Count);
        npcGameObject.transform.position = navMesh[polygonIndex].GetRandomPosition();

        // Add the sprite
        Sprite npcSprite = Resources.Load("Sprites/npc_sprite", typeof(Sprite)) as Sprite;
        SpriteRenderer spriteRenderer = npcGameObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = npcSprite;
        spriteRenderer.sortingOrder = 5;

        float myScale = 0.6f;
        npcGameObject.transform.localScale = new Vector3(myScale, myScale, myScale);

        // Add the RigidBody
        Rigidbody2D rb = npcGameObject.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;


        // Add Collider to the NPC
        CircleCollider2D cd = npcGameObject.AddComponent<CircleCollider2D>();
        cd.radius = npcSprite.rect.width * 0.003f;
        
        
        NPC npc;
        // Add the appropriate script according to the NPC type
        switch (npcData.npcType)
        {
            case NpcType.Intruder:
                npc = npcGameObject.GetComponent<Intruder>();
                spriteRenderer.color = Color.blue;
                m_Intruders.Add((Intruder) npc);
                break;

            case NpcType.Guard:
                npc = npcGameObject.GetComponent<Guard>();
                spriteRenderer.color = Color.red;
                m_Guards.Add((Guard) npc);
                break;

            default:
                npc = npcGameObject.GetComponent<Intruder>();
                break;
        }

        npcGameObject.layer = m_npcLayer;

        // Set the data 
        npc.SetNpcData(npcData);
        npc.SetArea(area);
    }

    public void LogGuardsPerformance()
    {
        if (m_Guards != null)
            foreach (var guard in m_Guards)
            {
                m_performanceMonitor.UpdateProgress(guard.LogNpcProgress());
            }
    }

    // Log the episode's performance and check if required number of episodes is recorded
    public bool FinalizeLogging()
    {
        LogGuardsPerformance();
        
        m_performanceMonitor.LogEpisodeFinish();

        return IsDone();
    }

    public bool IsDone()
    {
        return m_performanceMonitor.IsDone();
    }


    public void ResetGuardSeenArea(float resetThreshold)
    {
        foreach (var guard in m_Guards)
        {
            guard.RestrictSeenArea(resetThreshold);
        }
    }

    public void ResetNpcs()
    {
        foreach (var guard in m_Guards)
        {
            guard.EndEpisode();
        }
    }

    public List<Guard> GetGuards()
    {
        return m_Guards;
    }
}