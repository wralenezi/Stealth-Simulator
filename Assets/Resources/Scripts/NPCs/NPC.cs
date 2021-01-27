using System;
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;
using Random = UnityEngine.Random;


// Abstract class of an NPC
public abstract class NPC : Agent
{
    // NPC data
    protected NpcData Data;

    // NPC Rigid body for physics
    private Rigidbody2D m_NpcRb;

    // The world representation
    protected WorldRep World;

    // The area of the game
    protected StealthArea Area;

    // The path the agent is meant to follow
    protected List<Vector2> PathToTake;
    protected Vector2? Goal;

    // NPC movement variables
    protected float NpcSpeed = 2f;
    protected float NpcRotationSpeed = 200f;

    // The game perspective
    protected GameView GameView;

    // Renderers for hiding the NPC when not needed
    protected Renderer Renderer;
    protected Renderer FovRenderer;

    // Field of View object
    protected FieldOfView Fov;

    //************** Logging data ***************//
    // Variables for the distance travelled by a character
    // The last position the NPC was logged until
    private Vector2? m_LastPosition;

    // The total distance the guard travelled
    [SerializeField] private float m_TotalDistanceTravelled;

    // show the NPCs path to take
    public bool ShowPath;


    // Called when the agent is first created
    public override void Initialize()
    {
        PathToTake = new List<Vector2>();
        World = transform.parent.parent.Find("Map").GetComponent<WorldRep>();
    }

    // The set up of the start of the episode
    public override void OnEpisodeBegin()
    {
        PathToTake.Clear();
        m_TotalDistanceTravelled = 0f;

        SetPosition();

        Renderer = GetComponent<Renderer>();
        FovRenderer = Fov.GetComponent<Renderer>();
    }


    // Set the area the agent will be using
    public void SetArea(StealthArea area)
    {
        Area = area;
    }

    // Update the agent's last position
    public void SetPosition()
    {
        m_LastPosition = transform.position;
    }

    // Get a vector that represents the agents orientation around the z-axis
    public Vector2 GetDirection()
    {
        Vector2 dir = -Vector2.right;
        float rad = transform.eulerAngles.z * Mathf.Deg2Rad;
        float s = Mathf.Sin(rad);
        float c = Mathf.Cos(rad);
        return new Vector2(
            dir.y * c + dir.x * s, dir.y * s - dir.x * c
        );
    }

    // Set the NPC data
    public void SetNpcData(NpcData data)
    {
        Data = data;
        m_NpcRb = GetComponent<Rigidbody2D>();
    }

    // Place the NPC's start position
    public void ResetLocation(List<MeshPolygon> navMesh, List<Guard> guards, List<Polygon> mapWalls, Session sessionInfo)
    {
        if (Data.location == null)
        {
            // if the npc is a guard then place them at the center of a navmesh polygon
            if (Data.npcType == NpcType.Guard)
            {
                // Randomly place the NPC on the map
                int polygonIndex = Random.Range(0, navMesh.Count);
                transform.position = navMesh[polygonIndex].GetCentroidPosition();
            }
            // if the npc is an intruder then place in front of one of the guards
            else if (Data.npcType == NpcType.Intruder)
            {
                // If the scenario is a chase then place the intruder somewhere a guard can see
                if (sessionInfo.scenario == Scenario.Chase)
                {
                    int guardIndex = Random.Range(0, guards.Count);
                    Guard guard = guards[guardIndex];
                    transform.position = GetPositionNearNpc(guard.transform, mapWalls);

                    // Find the angle needed to rotate to face the desired direction
                    Vector2 rotateDir = (transform.position - guard.transform.position).normalized;
                    float m_goalAngle = Vector2.SignedAngle(Vector2.up, rotateDir);

                    Quaternion toRotation = Quaternion.AngleAxis(m_goalAngle, Vector3.forward);
                    guard.transform.rotation = toRotation;
                }
            }
        }
        else
        {
            // Set the agent to the specified location
            transform.position = (Vector2) Data.location.Value.position;
            transform.rotation = Quaternion.AngleAxis(Data.location.Value.rotation, Vector3.forward);
        }
    }

    private Vector2 GetPositionNearNpc(Transform transform, List<Polygon> mapWalls)
    {
        if (PolygonHelper.IsPointInPolygons(mapWalls, transform.position + transform.up))
            return transform.position + transform.up;

        if (PolygonHelper.IsPointInPolygons(mapWalls, transform.position - transform.up))
            return transform.position - transform.up;

        if (PolygonHelper.IsPointInPolygons(mapWalls, transform.position + transform.right))
            return transform.position + transform.right;
        
        if (PolygonHelper.IsPointInPolygons(mapWalls, transform.position - transform.right))
            return transform.position - transform.right;

        return transform.position;
    }


    public void AssignGoal()
    {
        if (Goal == null)
        {
            RequestDecision();
        }
    }

    // Define a goal for the agent and set the path to navigate to it,
    public void SetGoal(Vector2 _goal, bool isForced)
    {
        if (Goal == null || isForced)
            Goal = _goal;

        SetPathToGoal();
    }

    // Set the path to the goal using the NavMesh
    public void SetPathToGoal()
    {
        if (Goal != null)
            PathToTake = PathFinding.GetShortestPath(World.GetNavMesh(),
                transform.position, Goal.Value);
    }

    // Get the agents goal
    public Vector2? GetGoal()
    {
        return Goal;
    }

    // Get the current metrics of the agent's performance
    public abstract LogSnapshot LogNpcProgress();

    // Update NPC metrics
    public virtual void UpdateMetrics()
    {
    }

    // Move the NPC through it's path
    public void ExecutePlan(IState state, GuardRole? guardRole)
    {
        if (PathToTake.Count > 0)
            if (GoStraightTo(PathToTake[0], state, guardRole))
            {
                PathToTake.RemoveAt(0);

                if (PathToTake.Count == 0)
                    Goal = null;
            }
    }

    // Rotate to a specific target and then move towards it; return a boolean if the point is reached or not
    protected bool GoStraightTo(Vector3 target, IState state, GuardRole? guardRole)
    {
        // Find the angle needed to rotate to face the desired direction
        Vector2 rotateDir = (target - transform.position).normalized;
        float m_goalAngle = Vector2.SignedAngle(Vector2.up, rotateDir);

        Quaternion toRotation = Quaternion.AngleAxis(m_goalAngle, Vector3.forward);
        float angleLeft = Mathf.Round(toRotation.eulerAngles.z - transform.rotation.eulerAngles.z);

        // Make sure no rotation is due before moving
        if (Mathf.Abs(angleLeft) > 0f)
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, toRotation,
                NpcRotationSpeed * Time.fixedDeltaTime);
            return false;
        }

        // Handle movement
        float distanceLeft = Vector2.Distance(transform.position, target);

        // How to behavior when heading for the last way point (goal)
        if (PathToTake[PathToTake.Count - 1] == (Vector2) target)
        {
            // If the guard is in patrol, it doesn't need to visit the goal on the path. Just see it
            if (state is Patrol || state is Search)
                distanceLeft -= Properties.ViewRadius - 0.5f;
        }

        if (distanceLeft > 0.1f)
        {
            // Vector2 dir = (target - transform.position).normalized;
            m_NpcRb.MovePosition(m_NpcRb.position +
                                 ((Vector2) transform.up * (NpcSpeed * Time.fixedDeltaTime)));

            // Update the total distance traveled
            if (m_LastPosition != null)
                UpdateDistance();


            return false;
        }

        // Since no changes were needed to the position then the agent reached the goal
        return true;
    }

    // Move the NPC by user input.
    public void MoveByInput()
    {
        Vector2 dir = new Vector2(0f, 0f);

        if (Input.GetKey(KeyCode.W))
            dir += Vector2.up;
        if (Input.GetKey(KeyCode.D))
            dir += Vector2.right;
        if (Input.GetKey(KeyCode.A))
            dir += Vector2.left;
        if (Input.GetKey(KeyCode.S))
            dir += Vector2.down;

        if (dir != Vector2.zero)
        {
            float m_goalAngle = Vector2.SignedAngle(Vector2.up, dir);
            Quaternion toRotation = Quaternion.AngleAxis(m_goalAngle, Vector3.forward);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, toRotation,
                NpcRotationSpeed * Time.fixedDeltaTime * 10f);
        }

        m_NpcRb.MovePosition(m_NpcRb.position + dir.normalized * (NpcSpeed * Time.fixedDeltaTime));
    }

    // Clear the designated goal and path to take
    public void ClearGoal()
    {
        Goal = null;
        PathToTake.Clear();
    }


    // Get the remaining distance to goal
    public float GetRemainingDistanceToGoal()
    {
        float totalDistance = 0;
        for (int i = 0; i < PathToTake.Count - 1; i++)
        {
            totalDistance += Vector2.Distance(PathToTake[i], PathToTake[i + 1]);
        }

        return totalDistance;
    }


    // Update the total distance travelled by the NPC for logging purposes
    private void UpdateDistance()
    {
        var position = transform.position;
        var distanceTravelled = Vector2.Distance(position, m_LastPosition.Value);
        m_TotalDistanceTravelled += distanceTravelled;
        m_LastPosition = position;
    }

    public bool IsIdle()
    {
        return Goal == null;
    }

    public float GetTravelledDistance()
    {
        return m_TotalDistanceTravelled;
    }

    public NpcData GetNpcData()
    {
        return Data;
    }

    public float GetNpcSpeed()
    {
        return NpcSpeed;
    }


    public void OnDrawGizmos()
    {
        if (ShowPath && PathToTake != null & PathToTake.Count > 0)
        {
            for (int i = 0; i < PathToTake.Count - 1; i++)
            {
                Gizmos.DrawLine(PathToTake[i], PathToTake[i + 1]);
                Gizmos.DrawSphere(PathToTake[i], 0.1f);
            }

            Gizmos.DrawSphere(PathToTake[PathToTake.Count - 1], 0.1f);
        }
    }
}