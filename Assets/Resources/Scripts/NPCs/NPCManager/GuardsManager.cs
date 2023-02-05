using System;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.UI;


public class GuardsManager : MonoBehaviour
{
    // List of Guards
    private List<Guard> _guards;

    // Guards behavior controller
    private GuardsBehaviorController m_gCtrl;

    // For voicing the npcs
    private Scriptor m_script;

    private readonly float AlarmCooldown = 2f;
    private AudioSource m_alarmAudio;

    // The npc layer to ignore collisions between Npcs
    private LayerMask m_npcLayer;

    // The number of updates done for the possible interception points
    private int m_UpdateCount = 0;

    // Total time of guards overlapping each other
    public static float GuardsOverlapTime;


    // Start the NPC manager
    public void Initiate(Session session, MapManager mapManager)
    {
        _guards = new List<Guard>();

        // Initiate the guard behavior controller
        m_gCtrl = gameObject.AddComponent<GuardsBehaviorController>();
        m_gCtrl.Initiate(session, mapManager);

        m_script = gameObject.AddComponent<Scriptor>();
        m_script.Initialize(_guards, session.speechType);

        // m_StealthArea.AreaUiManager.UpdateGuardLabel(GetState());

        // Ignore collision between NPCs
        m_npcLayer = LayerMask.NameToLayer("NPC");
        Physics2D.IgnoreLayerCollision(m_npcLayer, m_npcLayer);
    }


    // This part controls the Reinforcement Learning part of the behavior

    #region RL behavior

    //
    // public override void OnEpisodeBegin()
    // {
    //     base.OnEpisodeBegin();
    //     RequestDecision();
    // }
    //
    // public override void CollectObservations(VectorSensor sensor)
    // {
    //     base.CollectObservations(sensor);
    //
    //     // // The fraction of the guards count.
    //     // float guardsPresence = m_SA.GetSessionInfo().guardsCount / Properties.MaxGuardCount;
    //     // sensor.AddObservation(guardsPresence);
    //     //
    //     // // The normalized area of the map
    //     // float mapsRelativeArea = m_SA.mapDecomposer.GetNavMeshArea() / Properties.MaxWalkableArea;
    //     // sensor.AddObservation(mapsRelativeArea);
    //
    //     // Debug.Log("  Map Area: " + m_StealthArea.mapDecomposer.GetNavMeshArea());
    // }
    //
    // // Called when the action is received.
    // public override void OnActionReceived(ActionBuffers actionBuffers)
    // {
    //     base.OnActionReceived(actionBuffers);
    //     //
    //     // float minBound = -1f;
    //     // float maxBound = 1f;
    //     //
    //     // float scaleFactor = 10f;
    //     //
    //     // searchWeights.probWeight = Mathf.Clamp(actionBuffers.ContinuousActions[0], minBound, maxBound) * scaleFactor;
    //     // searchWeights.ageWeight = Mathf.Clamp(actionBuffers.ContinuousActions[1], minBound, maxBound) * scaleFactor;
    //     // searchWeights.dstToGuardsWeight =
    //     //     Mathf.Clamp(actionBuffers.ContinuousActions[2], minBound, maxBound) * scaleFactor;
    //     // searchWeights.dstFromOwnWeight =
    //     //     Mathf.Clamp(actionBuffers.ContinuousActions[3], minBound, maxBound) * scaleFactor;
    // }
    //
    // // What to do when there is no Learning behavior
    // public override void Heuristic(in ActionBuffers actionsOut)
    // {
    //     base.Heuristic(actionsOut);
    //
    //     var continuousActionsOut = actionsOut.ContinuousActions;
    //
    //     // Default weight for the probability of the segment
    //     continuousActionsOut[0] = 1f;
    //     // Default weight for the age of the segment
    //     continuousActionsOut[1] = 0f;
    //     // Default weight for the distance to other guards' closest goal of the segment
    //     continuousActionsOut[2] = 1f;
    //     // Default weight for the distance of the segment
    //     continuousActionsOut[3] = -1f;
    // }
    //
    // // End the episode.
    // public void Done()
    // {
    //     // Set the reward for how many times the intruder has been spotted.
    //     // float reward = m_Intruders[0].GetPercentAlertTime();
    //     // reward += m_Intruders[0].GetNumberOfTimesSpotted() * 0.01f;
    //     // SetReward(reward);
    //
    //
    //     EndEpisode();
    // }
    //

    #endregion

    #region NPC creation

    // Create an NPC
    private void CreateGuard(Session session, NpcData npcData, List<MeshPolygon> navMesh)
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

        float myScale = 1f;
        npcGameObject.transform.localScale = new Vector3(myScale, myScale, myScale);

        // Add the RigidBody
        Rigidbody2D rb = npcGameObject.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        // Add Collider to the NPC
        CircleCollider2D cd = npcGameObject.AddComponent<CircleCollider2D>();
        cd.radius = Properties.NpcRadius;

        NPC npc;
        // Add the appropriate script according to the NPC type
        switch (npcData.npcType)
        {
            case NpcType.Guard:
                npcGameObject.name = "Guard" + (npcData.id).ToString().PadLeft(2, '0');
                npc = npcGameObject.AddComponent<Guard>();

                Color color = Color.clear;
                ColorUtility.TryParseHtmlString(session.guardColor, out color);
                spriteRenderer.color = color;

                // m_SA.AreaUiManager.UpdateGuardLabel(session.guardColor, spriteRenderer.color);
                _guards.Add((Guard)npc);
                break;

            default:
                npc = npcGameObject.GetComponent<Intruder>();
                break;
        }

        // 
        npc.Initiate(npcData, GameManager.Instance.GetVoice());

        // npc.ControlledByUser = session.guardBehaviorParams.alertPlanner == AlertPlanner.UserInput;

        NpcsManager.Instance.GetIntruders();

        // Allocate the NPC based on the specified scenario
        npc.ResetLocation(navMesh, NpcsManager.Instance.GetIntruders(), _guards, session);

        npcGameObject.layer = m_npcLayer;
    }

    // Create the NPCs of the scenario
    // Param: npcsData - List of the NPCdata
    // Param: navMesh - List of polygons the NPCs will spawn on
    // Param: Area -  a reference to the main script of the instance
    public void CreateGuards(Session session, List<MeshPolygon> navMesh)
    {
        foreach (var npcData in session.GetGuardsData())
            CreateGuard(session, npcData, navMesh);
    }

    // Reset NPCs at the end of the round
    public void Reset(List<MeshPolygon> navMesh, List<Intruder> intruders, Session session)
    {
        m_gCtrl.Reset();

        GuardsOverlapTime = 0f;
        // Reset guards
        foreach (var guard in _guards)
        {
            guard.ResetLocation(navMesh, intruders, _guards, session);
            guard.ResetNpc();
        }
    }

    #endregion


    public void Speak(NPC speaker, string lineType, float prob)
    {
        m_script.ChooseDialog(speaker, null, lineType, prob, GetGuards());
    }


    public GuardsBehaviorController GetController()
    {
        return m_gCtrl;
    }

    // // Update the variables for the guards
    // public void UpdateWldStNpcs()
    // {
    //     // WorldState.Set("guard_state", GetState().ToString());
    //
    //     foreach (var guard in m_Guards)
    //         guard.UpdateWldStatV(m_Guards);
    // }


    #region FSM functions

    // Get current state
    public State GetState()
    {
        return m_gCtrl.GetState();
    }

    // private NPC GetLastGuard()
    // {
    //     NPC intruder = m_SA.intrdrManager.GetIntruders()[0];
    //
    //     // The last time an opponent was seen
    //     float maxTime = Mathf.NegativeInfinity;
    //     Guard lastGuard = null;
    //
    //     foreach (var guard in m_Guards)
    //     {
    //         string timeString = WorldState.Get("last_time_" + guard.name + "_saw_" + intruder.name);
    //         timeString = Equals(timeString, WorldState.EMPTY_VALUE) ? "0" : timeString;
    //         float lastTime = float.Parse(timeString);
    //
    //         if (lastTime > maxTime)
    //         {
    //             maxTime = lastTime;
    //             lastGuard = guard;
    //         }
    //     }
    //
    //     return lastGuard;
    // }

    #endregion


    // Let NPCs cast their vision
    public void CastVision()
    {
        foreach (var guard in _guards)
            guard.CastVision();
    }

    // Execute NPCs plans
    public void Move(State state, float deltaTime)
    {
        foreach (var guard in _guards)
        {
            guard.ExecutePlan(state, deltaTime);
            guard.UpdateMetrics(deltaTime);
        }
    }


    // Update performance metrics
    public void UpdateMetrics(float deltaTime)
    {
        if (IsGuardsOverlapping())
            GuardsOverlapTime += deltaTime;
    }


    public bool IsGuardsOverlapping()
    {
        for (int i = 0; i < _guards.Count; i++)
        for (int j = i + 1; j < _guards.Count; j++)
        {
            if (Vector2.Distance(_guards[i].GetTransform().position, _guards[j].GetTransform().position) < 0.4f)
                return true;
        }

        return false;
    }

    // Update the guard manager for every 
    public void UpdateGuardManager(Session session)
    {
        // Restrict Guards Seen Area
        // ResetGuardSeenArea(session.coveredRegionResetThreshold);
    }

    public List<Guard> GetGuards()
    {
        return _guards;
    }
}


// Roles set to the guards
// Chaser: focuses on traversing towards the intruders last position
// Interceptor: Ambushes the intruder 
public enum GuardRole
{
    Patrol,
    Chase,
    Intercept
}

[Serializable]
// the weights for the feature functions of the search segments
public struct SearchWeights
{
    // The staleness of the search segment
    public float probWeight;

    // The search segment's age weight
    public float ageWeight;

    // Path distance of the search segment to the guard
    public float dstToGuardsWeight;

    // Path distance of the closest goal other guards are coming to visit
    public float dstFromOwnWeight;

    public SearchWeights(float _probWeight, float _ageWeight, float _dstToGuardsWeight, float _dstFromOwnWeight)
    {
        probWeight = _probWeight;
        ageWeight = _ageWeight;
        dstToGuardsWeight = _dstToGuardsWeight;
        dstFromOwnWeight = _dstFromOwnWeight;
    }
}