using System;
using System.Collections.Generic;
using System.Linq;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.UI;


public class GuardsManager : Agent
{
    private StealthArea m_SA;
    
    // Guards behavior controller
    private GuardsBehaviorController m_gCtrl;

    // public GuardDecisionStyle decisionStyle;

    // List of Guards
    private List<Guard> m_Guards;

    // Score 
    private float m_score;

    private readonly float AlarmCooldown = 2f;
    private AudioSource m_alarmAudio;

    // The npc layer to ignore collisions between Npcs
    private LayerMask m_npcLayer;

    // The number of updates done for the possible interception points
    private int m_UpdateCount = 0;

    // Total time of guards overlapping each other
    public static float GuardsOverlapTime;

    // The weights for deciding the heuristic
    public SearchWeights searchWeights;


    // Start the NPC manager
    public void Initiate(StealthArea _stealthArea, Transform map)
    {
        m_SA = _stealthArea;

        m_Guards = new List<Guard>();

        // Initiate the guard behavior controller
        m_gCtrl = gameObject.AddComponent<GuardsBehaviorController>();
        m_gCtrl.Initiate(_stealthArea, map);
        // m_StealthArea.AreaUiManager.UpdateGuardLabel(GetState());

        // Ignore collision between NPCs
        m_npcLayer = LayerMask.NameToLayer("NPC");
        Physics2D.IgnoreLayerCollision(m_npcLayer, m_npcLayer);
    }


    // This part controls the Reinforcement Learning part of the behavior

    #region RL behavior

    public override void OnEpisodeBegin()
    {
        base.OnEpisodeBegin();
        RequestDecision();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        base.CollectObservations(sensor);

        // The fraction of the guards count.
        float guardsPresence = m_SA.GetSessionInfo().guardsCount / Properties.MaxGuardCount;
        sensor.AddObservation(guardsPresence);

        // The normalized area of the map
        float mapsRelativeArea = m_SA.mapDecomposer.GetNavMeshArea() / Properties.MaxWalkableArea;
        sensor.AddObservation(mapsRelativeArea);

        // Debug.Log("  Map Area: " + m_StealthArea.mapDecomposer.GetNavMeshArea());
    }

    // Called when the action is received.
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        base.OnActionReceived(actionBuffers);

        float minBound = -1f;
        float maxBound = 1f;

        float scaleFactor = 10f;

        searchWeights.probWeight = Mathf.Clamp(actionBuffers.ContinuousActions[0], minBound, maxBound) * scaleFactor;
        searchWeights.ageWeight = Mathf.Clamp(actionBuffers.ContinuousActions[1], minBound, maxBound) * scaleFactor;
        searchWeights.dstToGuardsWeight =
            Mathf.Clamp(actionBuffers.ContinuousActions[2], minBound, maxBound) * scaleFactor;
        searchWeights.dstFromOwnWeight =
            Mathf.Clamp(actionBuffers.ContinuousActions[3], minBound, maxBound) * scaleFactor;
    }

    // What to do when there is no Learning behavior
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        base.Heuristic(actionsOut);

        var continuousActionsOut = actionsOut.ContinuousActions;

        // Default weight for the probability of the segment
        continuousActionsOut[0] = 1f;
        // Default weight for the age of the segment
        continuousActionsOut[1] = 0f;
        // Default weight for the distance to other guards' closest goal of the segment
        continuousActionsOut[2] = 1f;
        // Default weight for the distance of the segment
        continuousActionsOut[3] = -1f;
    }

    // End the episode.
    public void Done()
    {
        // Set the reward for how many times the intruder has been spotted.
        // float reward = m_Intruders[0].GetPercentAlertTime();
        // reward += m_Intruders[0].GetNumberOfTimesSpotted() * 0.01f;
        // SetReward(reward);


        EndEpisode();
    }

    #endregion

    #region NPC creation

    // Create an NPC
    private void CreateGuard(NpcData npcData, WorldRepType world, List<MeshPolygon> navMesh, StealthArea area)
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

        NPC npc;
        // Add the appropriate script according to the NPC type
        switch (npcData.npcType)
        {
            case NpcType.Guard:
                npcGameObject.name = "Guard" + npcData.id.ToString().PadLeft(2, '0');
                if (world != WorldRepType.Grid)
                    npc = npcGameObject.AddComponent<VisMeshGuard>();
                else
                    npc = npcGameObject.AddComponent<GridGuard>();

                Color color = Color.clear;
                ColorUtility.TryParseHtmlString(area.GetSessionInfo().guardColor, out color);
                spriteRenderer.color = color;

                m_SA.AreaUiManager.UpdateGuardLabel(area.GetSessionInfo().guardColor, spriteRenderer.color);

                m_Guards.Add((Guard) npc);
                break;

            default:
                npc = npcGameObject.GetComponent<Intruder>();
                break;
        }

        // 
        npc.Initiate(area, npcData);

        // Allocate the NPC based on the specified scenario
        npc.ResetLocation(navMesh, m_Guards, area.GetMap().GetWalls(), area.GetSessionInfo());

        npcGameObject.layer = m_npcLayer;
    }

    // Create the NPCs of the scenario
    // Param: npcsData - List of the NPCdata
    // Param: navMesh - List of polygons the NPCs will spawn on
    // Param: Area -  a reference to the main script of the instance
    public void CreateGuards(Session scenario, List<MeshPolygon> navMesh, StealthArea area)
    {
        foreach (var npcData in scenario.GetGuardsData())
            CreateGuard(npcData, scenario.worldRepType, navMesh, area);

        // Get one of the guards planner
        if (m_Guards.Count > 0)
            m_gCtrl.SetGuardPlanner(m_Guards[0].GetNpcData().guardPlanner.Value);
    }

    // Reset NPCs at the end of the round
    public void Reset(List<MeshPolygon> navMesh, StealthArea area)
    {
        SetScore(100);
        
        GuardsOverlapTime = 0f;

        // Reset guards
        foreach (var guard in m_Guards)
        {
            guard.ResetLocation(navMesh, m_Guards, area.GetMap().GetWalls(), area.GetSessionInfo());
            guard.ResetNpc();
        }

        m_gCtrl.ResetBehavior();
    }

    #endregion


    // Update the variables for the guards
    public void UpdateWldStNpcs()
    {
        // WorldState.Set("guard_state", GetState().ToString());

        foreach (var guard in m_Guards)
            guard.UpdateWldStatV(m_Guards);
    }

    public void CoinPicked()
    {
        if (GetState() is Search)
        {
            IncrementScore(15);
        }
    }

    public void IncrementScore(float score)
    {
        m_score += score;
        m_score = Mathf.Max(0, m_score);
        SetScore(m_score);
        m_SA.AreaUiManager.ShakeScore(score);
    }


    public void SetScore(float score)
    {
        m_score = score;
        AreaUIManager.Score = Mathf.Round(m_score * 10f) / 10f;
        m_SA.AreaUiManager.UpdateScore(AreaUIManager.Score);
    }


    #region FSM functions

    // Get current state
    public IState GetState()
    {
        return m_gCtrl.GetState();
    }


    //
    // public void AddDialogLine(NPC speaker, string dialogId, bool isVerbose)
    // {
    //     if (DialogGroup.RulesPass(speaker, dialogId,isVerbose))
    //         m_Scriptor.AppendDialogLine(speaker, dialogId);
    // }
    //


    private NPC GetLastGuard()
    {
        NPC intruder = m_SA.intrdrManager.GetIntruders()[0];

        // The last time an opponent was seen
        float maxTime = Mathf.NegativeInfinity;
        Guard lastGuard = null;

        foreach (var guard in m_Guards)
        {
            string timeString = WorldState.Get("last_time_" + guard.name + "_saw_" + intruder.name);
            timeString = Equals(timeString, WorldState.EMPTY_VALUE) ? "0" : timeString;
            float lastTime = float.Parse(timeString);

            if (lastTime > maxTime)
            {
                maxTime = lastTime;
                lastGuard = guard;
            }
        }

        return lastGuard;
    }

    #endregion


    // Assign a role flag to the closest guard to the intruder's last known position.
    // Order that guard to navigate to that position. 
    public void AssignGuardRoles()
    {
        // Set the first intruder 
        Intruder firstIntruder = m_SA.intrdrManager.GetIntruders()[0];

        // Assign the guard closest to the intruder's last position to chase them
        Guard closestGuard = null;
        float minDistance = Mathf.Infinity;

        foreach (var guard in m_Guards)
        {
            float distance = PathFinding.GetShortestPathDistance(m_SA.worldRep.GetNavMesh(),
                guard.transform.position,
                firstIntruder.GetLastKnownLocation());

            if (distance < minDistance)
            {
                closestGuard = guard;
                minDistance = distance;
            }
        }

        // Set the closest guard to the position the intruder was last seen on to chase and the rest to intercept
        foreach (var guard in m_Guards)
            if (guard == closestGuard)
            {
                guard.role = GuardRole.Chase;
                guard.SetGoal(firstIntruder.GetLastKnownLocation(), true);
            }
            else
                guard.role = GuardRole.Intercept;
    }


    // Let NPCs cast their vision
    public void CastVision()
    {
        foreach (var guard in m_Guards)
            guard.CastVision();
    }

    // NPCs decide plans if idle
    public void MakeDecision()
    {
        // Update the state of the guards manager
        m_gCtrl.ExecuteState();
    }


    // Execute NPCs plans
    public void Move(float deltaTime)
    {
        foreach (var guard in m_Guards)
            guard.ExecutePlan(GetState(), guard.role, deltaTime);
    }


    // Update performance metrics
    public void UpdateMetrics(float deltaTime)
    {
        if (IsGuardsOverlapping())
            GuardsOverlapTime += deltaTime;
    }


    public bool IsGuardsOverlapping()
    {
        for (int i = 0; i < m_Guards.Count; i++)
        for (int j = i + 1; j < m_Guards.Count; j++)
        {
            if (Vector2.Distance(m_Guards[i].GetTransform().position, m_Guards[j].GetTransform().position) < 0.4f)
                return true;
        }

        return false;
    }

    // Update the guard manager for every 
    public void UpdateGuardManager(Session session)
    {
        // Restrict Guards Seen Area
        ResetGuardSeenArea(session.coveredRegionResetThreshold);

        // Update the guards observations
        // UpdateObservations();

        // UpdatePotentialFuturePosition();

        // Mark visited nodes
        // m_interceptor.MarkVisitedInterceptionPoints(m_Guards, m_state.GetState());
    }


    // Update the guards FoV
    public void UpdateGuardVision()
    {
        
        bool intruderSpotted = false;
        NPC spotter = null;
        foreach (var guard in m_Guards)
        {
            // Accumulate the Seen Area of the guard
            // guard.AccumulateSeenArea(); // Disabled since it is not needed

            // Check if any intruders are spotted
            bool seen = guard.SpotIntruders(m_SA.intrdrManager.GetIntruders());

            if (!intruderSpotted)
            {
                intruderSpotted = seen;
                spotter = guard;
            }
        }

        // Render guards if the intruder can see them
        foreach (var intruder in m_SA.intrdrManager.GetIntruders())
        {
            intruder.SpotGuards(m_Guards);

            if (m_SA.GetSessionInfo().gameType == GameType.CoinCollection)
                intruder.SpotCoins(m_SA.coinSpawner.GetCoins());
        }

        // Switch the state of the guards 
        if (intruderSpotted)
        {
            // Guards knows the intruders location
            m_gCtrl.StartChase(spotter);
        }
        else if (GetState() is Chase)
        {
            // if the intruder is not seen and the guards were chasing then start searching
            m_gCtrl.StartSearch();
        }
    }

    public void ResetGuardSeenArea(float resetThreshold)
    {
        foreach (var guard in m_Guards)
        {
            guard.RestrictSeenArea(resetThreshold);
        }
    }

    public List<Guard> GetGuards()
    {
        return m_Guards;
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