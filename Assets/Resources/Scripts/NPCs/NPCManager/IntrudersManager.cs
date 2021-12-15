using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;

public class IntrudersManager : Agent
{
    private StealthArea m_SA;

    private IntrudersBehaviorController m_iCtrl;

    // List of Intruders
    private List<Intruder> m_Intruders;

    // The npc layer to ignore collisions between Npcs
    private LayerMask m_npcLayer;


    public void Initiate(StealthArea _stealthArea, Transform map)
    {
        m_SA = _stealthArea;

        m_Intruders = new List<Intruder>();

        // Initiate the intruder behavior controller
        m_iCtrl = gameObject.AddComponent<IntrudersBehaviorController>();
        m_iCtrl.Initiate(m_SA, map);

        // Ignore collision between NPCs
        m_npcLayer = LayerMask.NameToLayer("NPC");
        Physics2D.IgnoreLayerCollision(m_npcLayer, m_npcLayer);
    }


    public void Reset(List<MeshPolygon> navMesh)
    {
        // Reset Intruders
        foreach (var intruder in m_Intruders)
        {
            intruder.ResetLocation(navMesh, m_SA.guardsManager.GetGuards(), m_SA.GetMap().GetWalls(),
                m_SA.GetSessionInfo());
            intruder.ResetNpc();
        }
    }


    // Create the intruder
    private void CreateIntruder(NpcData npcData, WorldRepType world, List<MeshPolygon> navMesh, StealthArea area)
    {
        // Create the gameObject 
        // Set the NPC as a child to the manager
        GameObject npcGameObject = new GameObject();
        npcGameObject.transform.parent = transform;

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

        npcGameObject.name = "Intruder" + npcData.id.ToString().PadLeft(2, '0');
        NPC npc = npcGameObject.AddComponent<Intruder>();
        spriteRenderer.color = Color.black;

        m_Intruders.Add((Intruder) npc);

        npc.Initiate(area, npcData);

        // Allocate the NPC based on the specified scenario
        npc.ResetLocation(navMesh, area.guardsManager.GetGuards(), area.GetMap().GetWalls(), area.GetSessionInfo());

        npcGameObject.layer = m_npcLayer;
    }

    public IntrudersBehaviorController GetController()
    {
        return m_iCtrl;
    }

    public void CreateIntruders(Session scenario, List<MeshPolygon> navMesh, StealthArea area)
    {
        foreach (var npcData in scenario.GetIntrudersData())
            CreateIntruder(npcData, scenario.worldRepType, navMesh, area);
    }

    // Let NPCs cast their vision
    public void CastVision()
    {
        foreach (var intruder in m_Intruders)
            intruder.CastVision();
    }

    public void Move(float deltaTime)
    {
        foreach (var intruder in m_Intruders)
        {
            intruder.ExecutePlan(m_SA.guardsManager.GetState(), null, deltaTime);
            intruder.UpdateMetrics(m_SA.guardsManager.GetState(), deltaTime);
        }
    }

    public List<Intruder> GetIntruders()
    {
        return m_Intruders;
    }

    // Set the Camera to follow the intruder
    public void FollowIntruder()
    {
        if (m_Intruders.Count > 0)
        {
            Vector2 pos = m_Intruders[0].transform.position;
            GameManager.MainCamera.transform.position = new Vector3(pos.x, pos.y, -1f);
        }
    }

    public void HideLabels()
    {
        foreach (var intruder in m_Intruders)
        {
            intruder.HideLabel();
        }
    }
}