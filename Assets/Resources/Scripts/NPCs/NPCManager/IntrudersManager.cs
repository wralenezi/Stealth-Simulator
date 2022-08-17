using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;

public class IntrudersManager : Agent
{
    // List of Intruders
    private List<Intruder> _intruders;

    private IntrudersBehaviorController m_iCtrl;

    // The npc layer to ignore collisions between NPCs
    private LayerMask m_npcLayer;

    public void Initiate(Session session, MapManager mapManager)
    {
        _intruders = new List<Intruder>();

        // Initiate the intruder behavior controller
        m_iCtrl = gameObject.AddComponent<IntrudersBehaviorController>();
        m_iCtrl.Initiate(session, mapManager);

        // Ignore collision between NPCs
        m_npcLayer = LayerMask.NameToLayer("NPC");
        Physics2D.IgnoreLayerCollision(m_npcLayer, m_npcLayer);
    }

    public void Reset(List<MeshPolygon> navMesh, List<Intruder> intruders, List<Guard> guards, Session session)
    {
        m_iCtrl.Reset();
        
        // Reset Intruders
        foreach (var intruder in _intruders)
        {
            intruder.ResetLocation(navMesh, intruders, guards, session);
            intruder.ResetNpc();
        }
    }

    // Create the intruder
    private void CreateIntruder(NpcData npcData, List<MeshPolygon> navMesh, List<Guard> guards, Session session)
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

        float myScale = 1f; //0.6f;
        npcGameObject.transform.localScale = new Vector3(myScale, myScale, myScale);

        // Add the RigidBody
        Rigidbody2D rb = npcGameObject.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;


        // Add Collider to the NPC
        CircleCollider2D cd = npcGameObject.AddComponent<CircleCollider2D>();
        // cd.radius = npcSprite.rect.width * 0.003f;
        cd.radius = Properties.NpcRadius;

        npcGameObject.name = "Intruder" + (npcData.id).ToString().PadLeft(2, '0');
        NPC npc = npcGameObject.AddComponent<Intruder>();
        spriteRenderer.color = Color.black;

        _intruders.Add((Intruder) npc);

        npc.Initiate(npcData, GameManager.Instance.GetVoice());

        // Allocate the NPC based on the specified scenario
        npc.ResetLocation(navMesh, _intruders, guards, session);

        npcGameObject.layer = m_npcLayer;
    }

    public IntrudersBehaviorController GetController()
    {
        return m_iCtrl;
    }

    public void CreateIntruders(Session session, List<Guard> guards, List<MeshPolygon> navMesh)
    {
        foreach (var npcData in session.GetIntrudersData())
            CreateIntruder(npcData, navMesh, guards, session);
    }

    // Let NPCs cast their vision
    public void CastVision()
    {
        foreach (var intruder in _intruders)
            intruder.CastVision();
    }

    public void Move(State state, float deltaTime)
    {
        foreach (var intruder in _intruders)
        {
            intruder.ExecutePlan(state, deltaTime);
            intruder.UpdateMetrics(state, deltaTime);
        }
    }

    public List<Intruder> GetIntruders()
    {
        return _intruders;
    }

    // Set the Camera to follow the intruder
    public void FollowIntruder()
    {
        if (_intruders.Count > 0)
        {
            Vector2 pos = _intruders[0].transform.position;
            GameManager.MainCamera.transform.position = new Vector3(pos.x, pos.y, -1f);
        }
    }

    // public void HideLabels()
    // {
    //     foreach (var intruder in _intruders)
    //     {
    //         intruder.HideLabel();
    //     }
    // }
}