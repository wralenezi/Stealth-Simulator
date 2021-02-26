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
    private StealthArea m_StealthArea;

    // List of Guards
    private List<Guard> m_Guards;

    // List of Intruders
    private List<Intruder> m_Intruders;

    // Guards state
    private StateMachine m_state;
    public string StateName;

    // Guards planner
    private GuardBehavior m_guardPlanner;

    // Possible locations to search for the intruder in
    private List<InterceptionPoint> m_possiblePositions;

    // The npc layer to ignore collisions between npcs
    private LayerMask m_npcLayer;

    // The number of updates done for the possible interception points
    private int m_UpdateCount = 0;

    // Text label to display important messages to human players
    private Text m_Text;

    // The weights for deciding the heuristic
    public SearchWeights searchWeights;

    // Start the NPC manager
    public void Initiate(StealthArea _stealthArea, Text text)
    {
        m_StealthArea = _stealthArea;

        m_Guards = new List<Guard>();
        m_Intruders = new List<Intruder>();

        m_Text = text;

        // Initiate the FSM to patrol for the guards
        m_state = new StateMachine();
        m_state.ChangeState(new Patrol(this));

        // Ignore collision between NPCs
        m_npcLayer = LayerMask.NameToLayer("NPC");
        Physics2D.IgnoreLayerCollision(m_npcLayer, m_npcLayer);
    }

    private void Update()
    {
        StateName = m_state.GetState().ToString();
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
        sensor.AddObservation(m_StealthArea.GetSessionInfo().guardsCount / Properties.MaxGuardCount);

        // The normalized area of the map 
        // sensor.AddObservation(m_StealthArea.mapDecomposer.GetNavMeshArea() / Properties.MaxWalkableArea);
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
        continuousActionsOut[1] = 0.3f;
        // Default weight for the distance to other guards' closest goal of the segment
        continuousActionsOut[2] = 1f;
        // Default weight for the distance of the segment
        continuousActionsOut[3] = -0.8f;
    }

    // End the episode.
    public void Done()
    {
        float reward = m_Intruders[0].GetPercentAlertTime();
        reward += m_Intruders[0].GetNumberOfTimesSpotted() * 0.01f;

        SetReward(reward);
        EndEpisode();
    }

    #endregion


    #region NPC creation

    // Create an NPC
    private void CreateNpc(NpcData npcData, WorldRepType world, List<MeshPolygon> navMesh, StealthArea area)
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
            case NpcType.Intruder:
                npc = npcGameObject.AddComponent<Intruder>();
                spriteRenderer.color = Color.blue;
                m_Intruders.Add((Intruder) npc);
                break;

            case NpcType.Guard:
                if (world != WorldRepType.Grid)
                    npc = npcGameObject.AddComponent<VisMeshGuard>();
                else
                    npc = npcGameObject.AddComponent<GridGuard>();

                spriteRenderer.color = Color.red;
                m_Guards.Add((Guard) npc);
                break;

            default:
                npc = npcGameObject.GetComponent<Intruder>();
                break;
        }

        // 
        npc.Initiate();

        // Allocate the NPC based on the specified scenario
        npc.ResetLocation(navMesh, m_Guards, area.GetMap().GetWalls(), area.GetSessionInfo());

        npcGameObject.layer = m_npcLayer;

        // Set the data 
        npc.SetNpcData(npcData);
        npc.SetArea(area);
    }

    // Create the NPCs of the scenario
    // Param: npcsData - List of the NPCdata
    // Param: navMesh - List of polygons the NPCs will spawn on
    // Param: Area -  a reference to the main script of the instance
    public void CreateNpcs(Session scenario, List<MeshPolygon> navMesh, StealthArea area)
    {
        foreach (var npcData in scenario.GetNpcsData())
            CreateNpc(npcData, scenario.worldRepType, navMesh, area);


        // Get one of the guards planner
        if (m_Guards.Count > 0)
            m_guardPlanner = m_Guards[0].GetNpcData().guardPlanner.Value;
    }


    // Reset NPCs at the end of the round
    public void ResetNpcs(List<MeshPolygon> navMesh, StealthArea area)
    {
        // Reset guards
        foreach (var guard in m_Guards)
        {
            guard.ResetLocation(navMesh, m_Guards, area.GetMap().GetWalls(), area.GetSessionInfo());
            guard.ResetNpc();
        }

        // Reset Intruders
        foreach (var intruder in m_Intruders)
        {
            intruder.ResetLocation(navMesh, m_Guards, area.GetMap().GetWalls(), area.GetSessionInfo());
            intruder.ResetNpc();
        }


        // Set the guards to the default mode (patrol)
        m_state.ChangeState(new Patrol(this));
    }

    #endregion


    // Update the guards FoV
    public void UpdateGuardVision()
    {
        bool intruderSpotted = false;
        foreach (var guard in m_Guards)
        {
            // Accumulate the Seen Area of the guard
            guard.AccumulateSeenArea();

            // Check if any intruders are spotted
            if (!intruderSpotted)
                intruderSpotted = guard.SpotIntruders(m_Intruders);
        }

        // Render guards if the intruder can see them
        foreach (var intruder in m_Intruders)
        {
            intruder.SpotGuards(m_Guards);
        }

        // Switch the state of the guards 
        if (intruderSpotted)
        {
            // Guards knows the intruders location
            StartChase();
        }
        else if (m_state.GetState() is Chase)
        {
            // if the intruder is not seen and the guards were chasing then start searching
            StartSearch();
        }
    }

    // Set the Camera to follow the intruder
    public void FollowIntruder()
    {
        Vector2 pos = m_Intruders[0].transform.position;
        GameManager.MainCamera.transform.position = new Vector3(pos.x, pos.y, -1f);
    }

    // Update the search area in case the guards are searching for an intruder
    public void UpdateSearcher(float timeDelta)
    {
        // Move and propagate the possible intruder position (phantoms)
        if (GetState() is Search)
        {
            m_StealthArea.searcher.UpdateSearcher(m_Intruders[0].GetNpcSpeed(), m_Guards, timeDelta);
        }
    }

    // Update the guards observations to react properly to changes
    public void UpdateObservations()
    {
        StopIntercepting();
    }

    #region FSM functions

    // Get current state
    public IState GetState()
    {
        return m_state.GetState();
    }

    // Update the label of the status of the game.
    public void UpdateGuiLabel()
    {
        if (GetState() is Chase)
        {
            m_Text.text = "Alert";
            m_Text.color = Color.red;
        }
        else if (GetState() is Search)
        {
            m_Text.text = "Searching";
            m_Text.color = Color.yellow;
        }
        else if (GetState() is Patrol)
        {
            m_Text.text = "Normal";
            m_Text.color = Color.green;
        }
    }

    // Order Guards to patrol
    public void Patrol()
    {
        foreach (var guard in m_Guards)
            guard.Patrol();
    }

    // Once a intruder is seen one guard will be chasing and the others will be intercepting by being assigned "interception" locations.
    public void AssignGuardRoles()
    {
        // Assign the guard closest to the intruder's last position to chase them
        Guard closestGuard = null;
        float minDistance = Mathf.Infinity;

        foreach (var guard in m_Guards)
        {
            float distance = PathFinding.GetShortestPathDistance(m_StealthArea.worldRep.GetNavMesh(),
                guard.transform.position,
                m_Intruders[0].GetLastKnownLocation());

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
                guard.SetGoal(m_Intruders[0].GetLastKnownLocation(), true);
            }
            else
                guard.role = GuardRole.Intercept;


        // AssignGuardToInterceptionPoint();
    }

    // Assign the interception points to the guards
    public void AssignGuardToInterceptionPoint()
    {
        // Calculate the distance to each future possible position of the intruder and choose the closest to the guard's current position to intercept. 
        m_possiblePositions = m_StealthArea.interceptor.GetPossiblePositions();

        foreach (var guard in m_Guards)
        {
            // 
            if (guard.role == GuardRole.Chase && !guard.IsIdle())
                continue;


            float highestScore = Mathf.NegativeInfinity;
            InterceptionPoint designatedInterceptionPoint = null;


            foreach (var node in m_possiblePositions)
            {
                // Avoid going to the same goal of another guard if there are more interception points available.
                if (m_possiblePositions.Count > 1)
                {
                    bool isTargeted = false;
                    foreach (var guard1 in m_Guards)
                        if (guard1 != guard && guard1.GetGoal() != null && guard1.GetGoal() == node.position)
                        {
                            isTargeted = true;
                            break;
                        }

                    if (isTargeted)
                        continue;
                }

                // The distance from the guard
                float distanceToNode = PathFinding.GetShortestPathDistance(m_StealthArea.worldRep.GetNavMesh(),
                    guard.transform.position, node.position);

                // Distance from the intruder's last seen position
                float distanceFromIntruder = PathFinding.GetShortestPathDistance(m_StealthArea.worldRep.GetNavMesh(),
                    m_Intruders[0].GetLastKnownLocation(), node.position);

                float score = (1f / (node.generationIndex + 1f)) * 0.6f +
                              (1f / (node.distanceToEndNode + 1f)) * 0.1f +
                              (1f / (distanceToNode + 1f)) * 0.3f +
                              (1f / (distanceFromIntruder + 1f)) * 0.001f;

                if (score > highestScore)
                {
                    highestScore = score;
                    designatedInterceptionPoint = node;
                }
            }

            if (designatedInterceptionPoint != null)
            {
                guard.SetGoal(designatedInterceptionPoint.position, true);
            }
        }
    }


    // In case of intruder is seen
    public void StartChase()
    {
        // Switch to chase state
        if (!(m_state.GetState() is Chase))
        {
            m_state.ChangeState(new Chase(this));
            m_StealthArea.interceptor.Clear();

            foreach (var intruder in m_Intruders)
            {
                intruder.StartRunningAway();
            }
        }
    }

    // Order guards to chase
    public void Chase()
    {
        foreach (var guard in m_Guards)
        {
            // Decide the guard behavior in chasing based on its parameter
            if (m_guardPlanner.chase == GuardChasePlanner.Intercepting)
            {
                if (guard.role == GuardRole.Chase || guard.IsIdle())
                    guard.SetGoal(m_Intruders[0].GetLastKnownLocation(), true);
            }
            else if (m_guardPlanner.chase == GuardChasePlanner.Simple)
                guard.SetGoal(m_Intruders[0].GetLastKnownLocation(), true);
        }
    }

    // Check if the intercepting guard can switch to chasing; this is not done every frame since it requires path finding and is expensive. 
    public void StopIntercepting()
    {
        if (!(m_state.GetState() is Chase))
            return;

        // When the interceptor is closer to its target the the tolerance distance it can go straight for the intruder as long as the intruder is being chased
        float toleranceDistance = 1f;

        foreach (var guard in m_Guards)
            if (guard.role == GuardRole.Intercept)
            {
                float distanceToGoal = guard.GetRemainingDistanceToGoal();

                if (distanceToGoal < toleranceDistance)
                    guard.role = GuardRole.Chase;
            }
    }

    public void ClearGoals()
    {
        foreach (var guard in m_Guards)
            guard.ClearGoal();
    }

    // In case the intruder is not seen and the guards were on alert, start the search or keep doing it.
    public void StartSearch()
    {
        // if we were chasing then switch to search
        if (m_state.GetState() is Chase)
        {
            m_state.ChangeState(new Search(this));

            // Intruders will start their hiding behavior.
            foreach (var intruders in m_Intruders)
                intruders.StartHiding();

            // Flow the probability 
            m_StealthArea.searcher.PlaceSsForSearch(m_Intruders[0].GetLastKnownLocation(),
                m_Intruders[0].GetDirection());

            AssignGuardRoles();
        }
    }

    private void UpdatePotentialFuturePosition()
    {
        if (m_UpdateCount++ == 4)
        {
            if (m_state.GetState() is Chase)
            {
                // Calculate the distances to possible future positions of the intruder
                m_StealthArea.interceptor.CreatePossiblePositions(m_Intruders[0].GetLastKnownLocation(),
                    m_Intruders[0].GetDirection());
                AssignGuardRoles();
            }

            m_UpdateCount = 0;
        }
    }

    // Keep searching for the intruder
    public void Search()
    {
        foreach (var guard in m_Guards)
        {
            // Don't wait till the guard is free and just guide them to intruder's actual position.
            if (m_guardPlanner.search == GuardSearchPlanner.Cheating)
            {
                guard.SetGoal(m_Intruders[0].transform.position, true);
                continue;
            }

            // Once the chaser is idle that means that the intruder is still not seen
            // Now Guards should start visiting the nodes with distance more than zero
            if (guard.IsIdle())
            {
                // Search behavior based on the planner type 
                if (m_guardPlanner.search == GuardSearchPlanner.RmPropSimple ||
                    m_guardPlanner.search == GuardSearchPlanner.RmPropOccupancyDiffusal)
                {
                    // Get the search segment the guard should see
                    // guard.SetGoal(
                    //     m_StealthArea.interceptor.GetSearchSegment(guard, m_Guards, m_Intruders[0],
                    //         m_StealthArea.worldRep.GetNavMesh(),
                    //         searchWeights),
                    //     false);

                    SwapGoal(guard, m_StealthArea.searcher.GetSearchSegment(guard, m_Guards, m_Intruders[0],
                        m_StealthArea.worldRep.GetNavMesh(),
                        searchWeights));
                }
                else if (m_guardPlanner.search == GuardSearchPlanner.Random)
                {
                    Vector2 randomRoadmap = m_StealthArea.interceptor.GetRandomRoadMapNode();
                    guard.SetGoal(randomRoadmap, false);
                }
            }
        }
    }

    public void EndSearch()
    {
        m_StealthArea.interceptor.Clear();
    }

    #endregion


    // Assign goal to closest guard and swap goals.
    public void SwapGoal(Guard assignedGuard, Vector2 newGoal)
    {
        float minDistance = Vector2.Distance(assignedGuard.transform.position, newGoal);
        Guard closestGuard = null;
        for (int i = 0; i < m_Guards.Count; i++)
        {
            Guard curGuard = m_Guards[i];
            if (curGuard != assignedGuard)
            {
                float dstToOldGuard = Vector2.Distance(curGuard.transform.position, newGoal);

                // Check if the other guard is closer
                if (minDistance > dstToOldGuard)
                {
                    minDistance = dstToOldGuard;
                    closestGuard = curGuard;
                }
            }
        }

        if (closestGuard != null)
        {
            if (closestGuard.GetGoal() != null)
            {
                Vector2 tempGoal = closestGuard.GetGoal().Value;
                assignedGuard.SetGoal(tempGoal, true);
            }

            closestGuard.SetGoal(newGoal, true);
        }
        else
        {
            assignedGuard.SetGoal(newGoal, false);
        }
    }


    // Let NPCs cast their vision
    public void CastVision()
    {
        foreach (var guard in m_Guards)
            guard.CastVision();

        foreach (var intruder in m_Intruders)
            intruder.CastVision();
    }

    // NPCs decide plans if idle
    public void MakeDecision()
    {
        // Update the state of the guards manager
        m_state.UpdateState();

        foreach (var intruder in m_Intruders)
            intruder.ExecuteState();
    }


    // Execute NPCs plans
    public void MoveNpcs(float deltaTime)
    {
        foreach (var guard in m_Guards)
            guard.ExecutePlan(m_state.GetState(), guard.role, deltaTime);

        foreach (var intruder in m_Intruders)
            intruder.ExecutePlan(intruder.GetState(), null, deltaTime);
    }


    // Update performance metrics
    public void UpdateMetrics(float deltaTime)
    {
        foreach (var intruder in m_Intruders)
            intruder.UpdateMetrics(deltaTime);
    }

    // Update the guard manager for every 
    public void UpdateGuardManager(Session session)
    {
        // Restrict Guards Seen Area
        ResetGuardSeenArea(session.coveredRegionResetThreshold);

        // Update the guards observations
        UpdateObservations();

        UpdatePotentialFuturePosition();

        // Mark visited nodes
        // m_interceptor.MarkVisitedInterceptionPoints(m_Guards, m_state.GetState());
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

    public List<Intruder> GetIntruders()
    {
        return m_Intruders;
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