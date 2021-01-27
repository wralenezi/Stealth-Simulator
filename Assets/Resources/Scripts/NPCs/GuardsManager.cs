using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class GuardsManager : MonoBehaviour
{
    // World Representation
    private WorldRep m_WorldRep;

    // List of Guards
    private List<Guard> m_Guards;

    // List of Intruders
    private List<Intruder> m_Intruders;

    // Logging manager
    private PerformanceMonitor m_performanceMonitor;

    // Guards state
    private StateMachine m_state;
    public string StateName;

    // Guards planner
    private GuardBehavior m_guardPlanner;

    // For calculating interception points
    private Interceptor m_interceptor;

    // Possible locations to search for the intruder in
    private List<InterceptionPoint> m_possiblePositions;

    // The npc layer to ignore collisions between npcs
    private LayerMask m_npcLayer;

    // The number of updates done for the possible interception points
    private int m_UpdateCount = 0;

    // Text label to display important messages to human players
    private Text m_Text;


    // Start the NPC manager
    public void Initiate(Text text)
    {
        m_Guards = new List<Guard>();
        m_Intruders = new List<Intruder>();

        m_WorldRep = transform.parent.Find("Map").GetComponent<WorldRep>();
        m_interceptor = transform.parent.Find("Map").GetComponent<Interceptor>();

        m_Text = text;

        m_performanceMonitor = transform.parent.Find("Map").GetComponent<PerformanceMonitor>();
        m_performanceMonitor.SetArea();
        m_performanceMonitor.ResetResults();

        // Destroy the arena if the logging is over
        if (m_performanceMonitor.IsDone())
        {
            transform.parent.transform.GetComponent<StealthArea>().EndArea();
        }

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
            guard.EndEpisode();
        }

        // Reset Intruders
        foreach (var intruder in m_Intruders)
        {
            intruder.ResetLocation(navMesh, m_Guards, area.GetMap().GetWalls(), area.GetSessionInfo());
            intruder.EndEpisode();
        }

        m_state.ChangeState(new Patrol(this));
    }


    // Update the guards FoV
    public void UpdateGuardVision()
    {
        // In the case of searching for an intruder
        UpdateSearchArea();


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

    // 
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


    // Update the search area in case the guards are searching for an intruder
    public void UpdateSearchArea()
    {
        // Move and propagate the possible intruder position (phantoms)
        if (GetState() is Search)
            m_interceptor.ExpandSearch(m_Intruders[0].GetNpcSpeed(), m_Guards);
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

    // Order Guards to patrol
    public void Patrol()
    {
        foreach (var guard in m_Guards)
            guard.Patrol();
    }


    // Assign interception points
    public void AssignGuardRoles()
    {
        // Assign the guard closest to the intruder's last position to chase them
        Guard closestGuard = null;
        float minDistance = Mathf.Infinity;

        foreach (var guard in m_Guards)
        {
            float distance = PathFinding.GetShortestPathDistance(m_WorldRep.GetNavMesh(), guard.transform.position,
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
        m_possiblePositions = m_interceptor.GetPossiblePositions();

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
                float distanceToNode = PathFinding.GetShortestPathDistance(m_WorldRep.GetNavMesh(),
                    guard.transform.position, node.position);

                // Distance from the intruder's last seen position
                float distanceFromIntruder = PathFinding.GetShortestPathDistance(m_WorldRep.GetNavMesh(),
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
            m_interceptor.Clear();

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

    // In case of an intruder is located order the guard to chase to that assigned @param: target
    public void UpdateChasingTarget(Vector2 target)
    {
    }

    // In case the intruder is not seen and the guards were on alert, start the search or keep doing it.
    public void StartSearch()
    {
        // if we were chasing then switch to search
        if (m_state.GetState() is Chase)
        {
            m_state.ChangeState(new Search(this));


            foreach (var intruders in m_Intruders)
            {
                intruders.StartHiding();
            }

            // Flow the probability 
            m_interceptor.PlaceInterceptionForSearch(m_Intruders[0].GetLastKnownLocation(),
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
                m_interceptor.CreatePossiblePositions(m_Intruders[0].GetLastKnownLocation(),
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
            // Once the chaser is idle that means that the intruder is still not seen
            // Now Guards should start visiting the nodes with distance more than zero
            if (guard.IsIdle())
            {
                // 
                if (m_guardPlanner.search == GuardSearchPlanner.Interception)
                {
                    // AssignGuardToInterceptionPoint();
                    guard.SetGoal(
                        m_interceptor.GetSearchSegment(guard, m_Guards, m_Intruders[0], m_WorldRep.GetNavMesh()),
                        false);
                }
                else if (m_guardPlanner.search == GuardSearchPlanner.Random)
                {
                    Vector2 randomRoadmap = m_interceptor.GetRandomRoadMapNode();

                    guard.SetGoal(randomRoadmap, false);
                }
            }
        }
    }


    public void EndSearch()
    {
        m_interceptor.Clear();
    }

    #endregion


    // NPCs decide plans if idle
    public void MakeDecision()
    {
        m_state.Update();

        foreach (var intruder in m_Intruders)
            intruder.ExecuteState();
    }


    // Execute NPCs plans
    public void MoveNpcs()
    {
        foreach (var guard in m_Guards)
            guard.ExecutePlan(m_state.GetState(), guard.role);

        foreach (var intruder in m_Intruders)
            intruder.ExecutePlan(intruder.GetState(), null);
    }


    // Update performance metrics
    public void UpdateMetrics()
    {
        foreach (var intruder in m_Intruders)
            intruder.UpdateMetrics();
    }

    public void LogPerformance()
    {
        if (m_Guards != null)
            foreach (var guard in m_Guards)
                m_performanceMonitor.UpdateProgress(guard.LogNpcProgress());

        if (m_Intruders != null)
            foreach (var intruder in m_Intruders)
                m_performanceMonitor.UpdateProgress(intruder.LogNpcProgress());
    }

    // Log the episode's performance and check if required number of episodes is recorded
    // Upload 
    public bool FinalizeLogging(bool isUpload)
    {
        LogPerformance();

        if (!isUpload)
            m_performanceMonitor.LogEpisodeFinish();
        else
            m_performanceMonitor.UploadEpisodeData();

        return IsDone();
    }

    public bool IsDone()
    {
        return m_performanceMonitor.IsDone();
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