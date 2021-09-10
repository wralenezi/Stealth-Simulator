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

    // Guards behavior managers
    // public Patroler patroler;
    // Time of the last timestamp a patrol state initiated 
    private float m_lstPatrolTime;


    // Interceptor controller for controlling how guards act when chasing an intruder in sight.
    public Interceptor interceptor;

    // Time of the last timestamp a chase state initiated 
    private float m_lstChaseTime;

    // Search controller for controlling how guards searched for an intruder.
    public Searcher searcher;

    // Time of the last timestamp a search state initiated 
    private float m_lstSearchTime;

    public GuardDecisionStyle decisionStyle;

    // List of Guards
    private List<Guard> m_Guards;

    // List of Intruders
    private List<Intruder> m_Intruders;

    // Score 
    private float m_score;

    // Guards state
    private StateMachine m_state;
    public string StateName;

    private float m_lastAlertTime;
    private readonly float AlarmCooldown = 2f;
    private AudioSource m_alarmAudio;

    // Script and dialog manager
    private Scriptor m_Scriptor;

    // timestamp since the search started
    private float m_searchStamp;

    // Guards planner
    private GuardBehavior m_guardPlanner;

    // Possible locations to search for the intruder in
    private List<InterceptionPoint> m_possiblePositions;

    // The npc layer to ignore collisions between Npcs
    private LayerMask m_npcLayer;

    // The number of updates done for the possible interception points
    private int m_UpdateCount = 0;

    // Total time of guards overlapping each other
    public static float GuardsOverlapTime;

    // The weights for deciding the heuristic
    public SearchWeights searchWeights;

    // Time it takes to make a decision for all guards
    public float Decision;
    public float Updated;

    // Start the NPC manager
    public void Initiate(StealthArea _stealthArea, Transform map)
    {
        m_StealthArea = _stealthArea;

        m_Guards = new List<Guard>();
        m_Intruders = new List<Intruder>();

        // Interception controller
        interceptor = map.GetComponent<Interceptor>();
        interceptor.Initiate(m_StealthArea);

        // Search Controller
        searcher = map.gameObject.AddComponent<Searcher>();
        searcher.Initiate(m_StealthArea);

        // Initiate the FSM to patrol for the guards
        m_state = new StateMachine();
        ChangeState(new Patrol(this));
        m_lstPatrolTime = Time.time;
        WorldState.Set("lastPatrolTime", m_lstPatrolTime.ToString());

        // m_StealthArea.AreaUiManager.UpdateGuardLabel(GetState());

        // Ignore collision between NPCs
        m_npcLayer = LayerMask.NameToLayer("NPC");
        Physics2D.IgnoreLayerCollision(m_npcLayer, m_npcLayer);

        // Initiate scriptor
        m_Scriptor = gameObject.AddComponent<Scriptor>();
        m_Scriptor.Initialize();
    }

    public void Reset()
    {
        SetScore(0);
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
        float guardsPresence = m_StealthArea.GetSessionInfo().guardsCount / Properties.MaxGuardCount;
        sensor.AddObservation(guardsPresence);

        // The normalized area of the map
        float mapsRelativeArea = m_StealthArea.mapDecomposer.GetNavMeshArea() / Properties.MaxWalkableArea;
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
                npcGameObject.name = "Intruder" + npcData.id.ToString().PadLeft(2, '0');
                npc = npcGameObject.AddComponent<Intruder>();
                spriteRenderer.color = Color.blue;
                m_Intruders.Add((Intruder) npc);
                break;

            case NpcType.Guard:
                npcGameObject.name = "Guard" + npcData.id.ToString().PadLeft(2, '0');
                if (world != WorldRepType.Grid)
                    npc = npcGameObject.AddComponent<VisMeshGuard>();
                else
                    npc = npcGameObject.AddComponent<GridGuard>();

                string colorName = "";
                switch (npcData.guardPlanner.Value.search)
                {
                    case GuardSearchPlanner.Cheating:
                        spriteRenderer.color = Color.red;
                        colorName = "Red";
                        break;

                    case GuardSearchPlanner.RmPropSimple:
                        spriteRenderer.color = Color.green;
                        colorName = "Green";
                        break;

                    case GuardSearchPlanner.RmPropOccupancyDiffusal:
                        spriteRenderer.color = Color.yellow;
                        colorName = "Yellow";
                        break;

                    case GuardSearchPlanner.Random:
                        spriteRenderer.color = Color.black;
                        colorName = "Tutorial";
                        break;
                }

                m_StealthArea.AreaUiManager.UpdateGuardLabel(colorName, spriteRenderer.color);

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
    public void CreateNpcs(Session scenario, List<MeshPolygon> navMesh, StealthArea area)
    {
        foreach (var npcData in scenario.GetGuardsData())
            CreateNpc(npcData, scenario.worldRepType, navMesh, area);

        foreach (var npcData in scenario.GetIntrudersData())
            CreateNpc(npcData, scenario.worldRepType, navMesh, area);

        // Get one of the guards planner
        if (m_Guards.Count > 0)
            m_guardPlanner = m_Guards[0].GetNpcData().guardPlanner.Value;
    }

    // Reset NPCs at the end of the round
    public void ResetNpcs(List<MeshPolygon> navMesh, StealthArea area)
    {
        GuardsOverlapTime = 0f;

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
        ChangeState(new Patrol(this));
        m_lstPatrolTime = Time.time;
        WorldState.Set("lastPatrolTime", m_lstPatrolTime.ToString());
    }

    #endregion

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
            bool seen = guard.SpotIntruders(m_Intruders);

            if (!intruderSpotted)
            {
                intruderSpotted = seen;
                spotter = guard;
            }
        }

        // Render guards if the intruder can see them
        foreach (var intruder in m_Intruders)
        {
            intruder.SpotGuards(m_Guards);

            if (GameManager.instance.gameType == GameType.CoinCollection)
                intruder.SpotCoins(m_StealthArea.coinSpawner.GetCoins());
        }

        // Switch the state of the guards 
        if (intruderSpotted)
        {
            // Guards knows the intruders location
            StartChase(spotter);
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
        if (m_Intruders.Count > 0)
        {
            Vector2 pos = m_Intruders[0].transform.position;
            GameManager.MainCamera.transform.position = new Vector3(pos.x, pos.y, -1f);
        }
    }

    // Update the search area in case the guards are searching for an intruder
    public void UpdateSearcher(float timeDelta)
    {
        // Move and propagate the possible intruder position (phantoms)
        if (GetState() is Search)
        {
            float timeBefore = Time.realtimeSinceStartup;
            searcher.UpdateSearcher(m_Intruders[0].GetNpcSpeed(), m_Guards, timeDelta);
            Updated = (Time.realtimeSinceStartup - timeBefore);
        }
    }

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
            IncrementScore(100);
        }
    }

    public void HideLabels()
    {
        foreach (var intruder in m_Intruders)
        {
            intruder.HideLabel();
        }
    }

    public void IncrementScore(float score)
    {
        m_score += score;
        m_score = Mathf.Max(0, m_score);
        SetScore(m_score);
        m_StealthArea.AreaUiManager.ShakeScore(score);
    }


    public void SetScore(float score)
    {
        m_score = score;
        m_StealthArea.AreaUiManager.UpdateScore(Mathf.Round(m_score * 10f) / 10f);
    }


    #region FSM functions

    // Order Guards to patrol
    public void Patrol()
    {
        foreach (var guard in m_Guards)
            guard.Patrol();
    }

    // Get current state
    public IState GetState()
    {
        return m_state.GetState();
    }

    private void ChangeState(IState state)
    {
        m_state.ChangeState(state);
        WorldState.Set("guard_state", GetState().ToString());
    }

    // In case of intruder is seen
    public void StartChase(NPC spotter)
    {
        // Switch to chase state
        if (m_state.GetState() is Chase) return;

        if (m_state.GetState() is Search)
            WorldState.Set("lastSearchTimeEnd", Time.time.ToString());
        else if (m_state.GetState() is Patrol)
            WorldState.Set("lastPatrolTimeEnd", Time.time.ToString());

        ChangeState(new Chase(this));

        // AddDialogLine(spotter, "st_spotted");
        //
        m_Scriptor.ChooseDialog(spotter, "Spot");


        m_lstChaseTime = Time.time;
        WorldState.Set("lastChaseTimeStart", m_lstChaseTime.ToString());
        WorldState.Set("lastChaseTimeEnd", WorldState.EMPTY_VALUE);

        interceptor.Clear();

        if (GameManager.instance.gameType == GameType.CoinCollection)
            m_StealthArea.coinSpawner.DisableCoins();

        // m_StealthArea.AreaUiManager.UpdateGuardLabel(GetState());

        foreach (var intruder in m_Intruders)
        {
            intruder.StartRunningAway();
        }
    }
    //
    // public void AddDialogLine(NPC speaker, string dialogId, bool isVerbose)
    // {
    //     if (DialogGroup.RulesPass(speaker, dialogId,isVerbose))
    //         m_Scriptor.AppendDialogLine(speaker, dialogId);
    // }
    //

    // Order guards to chase
    public void Chase()
    {
        m_lastAlertTime = StealthArea.GetElapsedTime();

        // Loop through the guards to order them
        foreach (var guard in m_Guards)
        {
            // Decide the guard behavior in chasing based on its parameter
            if (m_guardPlanner.chase == GuardChasePlanner.Intercepting)
            {
                if (guard.role == GuardRole.Chase || !guard.IsBusy())
                    guard.SetGoal(m_Intruders[0].GetLastKnownLocation(), true);
            }
            else if (m_guardPlanner.chase == GuardChasePlanner.Simple)
                guard.SetGoal(m_Intruders[0].GetLastKnownLocation(), true);


            // Update the score based on the game type if the intruder is seen
            if (m_Intruders.Count > 0)
            {
                if (GameManager.instance.gameType == GameType.Stealth)
                {
                    float percentage = Properties.EpisodeLength - m_Intruders[0].GetAlertTime();
                    percentage = Mathf.Round((percentage * 1000f) / Properties.EpisodeLength) / 10f;
                    GameManager.instance.GetActiveArea().AreaUiManager.UpdateScore(percentage);
                }
                else if (GameManager.instance.gameType == GameType.CoinCollection)
                {
                    float decreaseRate = 20f;
                    IncrementScore(-decreaseRate * Time.deltaTime);
                }
            }
        }
    }

    // Check if the intercepting guard can switch to chasing; this is not done every frame since it requires path finding and is expensive. 
    public void StopChase()
    {
        if (!(m_state.GetState() is Chase)) return;

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

    // In case the intruder is not seen and the guards were on alert, start the search or keep doing it.
    public void StartSearch()
    {
        // if we were chasing then switch to search
        if (!(m_state.GetState() is Chase)) return;

        // Change the guard state
        ChangeState(new Search(this));

        WorldState.Set("lastChaseTimeEnd", m_lstSearchTime.ToString());

        m_lstSearchTime = Time.time;
        WorldState.Set("lastSearchTimeStart", m_lstSearchTime.ToString());
        WorldState.Set("lastSearchTimeEnd", WorldState.EMPTY_VALUE);
        m_searchStamp = StealthArea.GetElapsedTime();

        // m_StealthArea.AreaUiManager.UpdateGuardLabel(GetState());

        // Spawn the coins
        if (GameManager.instance.gameType == GameType.CoinCollection)
            m_StealthArea.coinSpawner.SpawnCoins();

        // Intruders will start their hiding behavior.
        foreach (var intruders in m_Intruders)
            intruders.StartHiding();

        // Flow the probability for intruder positions
        searcher.PlaceSsForSearch(m_Intruders[0].GetLastKnownLocation(),
            m_Intruders[0].GetDirection());

        // Assign the guard roles
        // AssignGuardRoles();

        // Order the guards to go the intruder's last known position
        foreach (var guard in m_Guards)
            guard.SetGoal(m_Intruders[0].GetLastKnownLocation(), true);
    }


    private NPC GetLastGuard()
    {
        NPC intruder = m_Intruders[0];

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

    // Keep searching for the intruder
    public void Search()
    {
        // Benchmark purposes 
        float timeBefore = Time.realtimeSinceStartup;

        foreach (var guard in m_Guards)
        {
            // In case of cheating behavior, Don't wait till the guard is free and just guide them to intruder's actual position.
            if (m_guardPlanner.search == GuardSearchPlanner.Cheating)
            {
                guard.SetGoal(m_Intruders[0].GetTransform().position, true);
                continue;
            }

            // Two method for choosing the guard plans

            // Choose the segment
            SearchIndividualSegments(guard);

            // Define a path the guard should follow
            // SearchPath(guard);
        }

        // Logging purposes
        if (Time.realtimeSinceStartup - timeBefore > Decision)
            Decision = (Time.realtimeSinceStartup - timeBefore);
    }

    // Individual segment search
    public void SearchIndividualSegments(Guard guard)
    {
        // Once the chaser is idle that means that the intruder is still not seen
        // Now Guards should start visiting the nodes with distance more than zero
        if (!guard.IsBusy())
        {
            // Search behavior based on the planner type 
            if (m_guardPlanner.search == GuardSearchPlanner.RmPropSimple ||
                m_guardPlanner.search == GuardSearchPlanner.RmPropOccupancyDiffusal)
            {
                // Get a new goal and swap it with the closest guard to that goal and take their goals instead.
                SwapGoal(guard, searcher.GetSearchSegment(guard, m_Guards, m_Intruders[0],
                    m_StealthArea.worldRep.GetNavMesh(),
                    searchWeights));
            }
            else if (m_guardPlanner.search == GuardSearchPlanner.Random)
            {
                Vector2 randomRoadmap = m_StealthArea.roadMap.GetRandomRoadMapNode();
                guard.SetGoal(randomRoadmap, true);
            }
        }
    }

    public void SearchPath(Guard guard)
    {
        if (!guard.IsBusy())
        {
            if (m_guardPlanner.search == GuardSearchPlanner.RmPropSimple ||
                m_guardPlanner.search == GuardSearchPlanner.RmPropOccupancyDiffusal)
            {
                float timeDiff = StealthArea.GetElapsedTime() - m_searchStamp;
                float distancePortion = timeDiff / 15f;

                float length = Properties.MaxPathDistance * distancePortion;


                searcher.GetPath(guard, length);

                // searcher.FindLineAndPath(guard);
            }
        }
    }

    public void EndSearch()
    {
        searcher.Clear();
    }

    #endregion


    // Assign a role flag to the closest guard to the intruder's last known position.
    // Order that guard to navigate to that position. 
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
    }

    // Assign the interception points to the guards
    public void AssignGuardToInterceptionPoint()
    {
        // Calculate the distance to each future possible position of the intruder and choose the closest to the guard's current position to intercept. 
        m_possiblePositions = interceptor.GetPossiblePositions();

        foreach (var guard in m_Guards)
        {
            // 
            if (guard.role == GuardRole.Chase && guard.IsBusy())
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
                        if (guard1 != guard && guard1.IsBusy() && guard1.GetGoal() == node.position)
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

    // Assign goal to closest guard and swap goals if needed if the guard was busy.
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
            if (closestGuard.IsBusy())
            {
                Vector2 tempGoal = closestGuard.GetGoal().Value;
                assignedGuard.SetGoal(tempGoal, true);
                
                // guard announce to go instead 
                m_Scriptor.ChooseDialog(assignedGuard,"Plan");
            }

            // Assign the new goal to the guard
            closestGuard.SetGoal(newGoal, true);
            
            m_Scriptor.ChooseDialog(closestGuard,"Plan");
        }
        else
        {
            assignedGuard.SetGoal(newGoal, false);
            m_Scriptor.ChooseDialog(assignedGuard,"Plan");
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
        if (IsGuardsOverlapping())
            GuardsOverlapTime += deltaTime;

        foreach (var intruder in m_Intruders)
            intruder.UpdateMetrics(deltaTime);
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

    // private void UpdatePotentialFuturePosition()
    // {
    //     if (m_UpdateCount++ == 4)
    //     {
    //         if (m_state.GetState() is Chase)
    //         {
    //             // Calculate the distances to possible future positions of the intruder
    //             interceptor.CreatePossiblePositions(m_Intruders[0].GetLastKnownLocation(),
    //                 m_Intruders[0].GetDirection());
    //             AssignGuardRoles();
    //         }
    //
    //         m_UpdateCount = 0;
    //     }
    // }


    public void ClearGoals()
    {
        foreach (var guard in m_Guards)
            guard.ClearGoal();
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