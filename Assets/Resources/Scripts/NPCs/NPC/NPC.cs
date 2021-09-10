using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;


// Abstract class of an NPC
public abstract class NPC : MonoBehaviour
{
    // NPC data
    protected NpcData Data;

    // NPC Rigid body for physics
    private Rigidbody2D m_NpcRb;

    private Transform m_transform;

    // The world representation
    protected WorldRep World;

    // The area of the game
    protected StealthArea Area;

    // The path the agent is meant to follow
    private List<Vector2> PathToTake;
    private Vector2? Goal;

    // The sequence of Roadmap lines an NPC marked on their way;
    protected List<RoadMapLine> LinesToPassThrough;

    // NPC movement variables
    protected float NpcSpeed = Properties.NpcSpeed;
    protected float NpcRotationSpeed = Properties.NpcRotationSpeed;

    // The game perspective
    protected GameView GameView;

    // Renderers for hiding the NPC when not needed
    protected Renderer Renderer;
    protected Renderer FovRenderer;

    // Field of View object
    protected FieldOfView Fov;

    private float m_FovRadius;

    // The Current FoV
    private List<Polygon> m_FovPolygon;


    //************** Logging data ***************//
    // Variables for the distance travelled by a character
    // The last position the NPC was logged until
    private Vector2? m_LastPosition;

    // The total distance the guard travelled
    [SerializeField] private float m_TotalDistanceTravelled;

    // show the NPCs path to take
    public bool ShowPath;

    // Called when the agent is first created
    public virtual void Initiate(StealthArea area, NpcData data)
    {
        PathToTake = new List<Vector2>();
        LinesToPassThrough = new List<RoadMapLine>();

        Area = area;
        m_transform = transform;
        World = GetTransform().parent.parent.Find("Map").GetComponent<WorldRep>();
        Data = data;
        m_NpcRb = GetComponent<Rigidbody2D>();
        m_FovPolygon = new List<Polygon>() {new Polygon()};


        AddFoV();
    }


    // The set up of the start of the episode
    public virtual void ResetNpc()
    {
        ClearGoal();
        m_TotalDistanceTravelled = 0f;

        SetPosition();
        m_FovPolygon[0].Clear();


        Renderer = GetComponent<Renderer>();
        FovRenderer = Fov.GetComponent<Renderer>();
    }

    // Clear the designated goal and path to take
    public void ClearGoal()
    {
        Goal = null;
        PathToTake.Clear();
        ClearLines();
    }

    // Remove the counter the NPC left on the road map and clear them from the list.
    public abstract void ClearLines();


    // Update the agent's last position
    public void SetPosition()
    {
        m_LastPosition = GetTransform().position;
    }

    // Add a field of view component to the NPC
    public void AddFoV()
    {
        // The game object that contains the field of view
        GameObject fovGameObject = new GameObject("FoV");

        // Assign it as a child to the guard
        var transform1 = transform;
        fovGameObject.transform.parent = transform1;
        fovGameObject.transform.position = transform1.position;

        m_FovRadius = Properties.GetFovRadius(Data.npcType);

        Fov = fovGameObject.AddComponent<FieldOfView>();
        Fov.Initiate(Properties.GetFovAngle(Data.npcType), m_FovRadius, Properties.GetFovColor(Data.npcType));
    }

    private void LoadFovPolygon()
    {
        GetFovPolygon().Clear();
        foreach (var vertex in Fov.GetFovVertices())
            GetFovPolygon().AddPoint(vertex);
    }

    public float GetFovRadius()
    {
        return m_FovRadius;
    }


    // Cast the guard field of view
    public void CastVision()
    {
        Fov.CastFieldOfView();
        LoadFovPolygon();
    }

    public Transform GetTransform()
    {
        return m_transform;
    }

    // Get field of vision
    public Polygon GetFovPolygon()
    {
        return GetFov()[0];
    }

    public List<RoadMapLine> GetLinesToPass()
    {
        return LinesToPassThrough;
    }

    public List<Vector2> GetPath()
    {
        return PathToTake;
    }

    public List<Polygon> GetFov()
    {
        return m_FovPolygon;
    }

    // Get a vector that represents the agents facing direction around the z-axis
    public Vector2 GetDirection()
    {
        Vector2 dir = -Vector2.right;
        float rad = GetTransform().eulerAngles.z * Mathf.Deg2Rad;
        float s = Mathf.Sin(rad);
        float c = Mathf.Cos(rad);
        return new Vector2(
            dir.y * c + dir.x * s, dir.y * s - dir.x * c
        );
    }

    // Place the NPC's start position
    public void ResetLocation(List<MeshPolygon> navMesh, List<Guard> guards, List<Polygon> mapWalls,
        Session sessionInfo)
    {
        // If there is no location specified for the agent to be set in; place them randomly.
        if (Data.location == null)
        {
            // if the npc is a guard then place them at the center of a NavMesh polygon
            if (Data.npcType == NpcType.Guard)
            {
                // Randomly place the NPC on the map
                int polygonIndex = Random.Range(0, navMesh.Count);
                GetTransform().position = navMesh[polygonIndex].GetCentroidPosition();
            }
            // if the npc is an intruder then place in front of one of the guards
            else if (Data.npcType == NpcType.Intruder)
            {
                // If the scenario is a chase then place the intruder somewhere a guard can see
                if (sessionInfo.scenario == Scenario.Chase)
                {
                    int guardIndex = Random.Range(0, guards.Count);
                    Guard guard = guards[guardIndex];
                    GetTransform().position = GetPositionNearNpc(guard.GetTransform(), mapWalls);

                    // Find the angle needed to rotate to face the desired direction
                    Vector2 rotateDir = (GetTransform().position - guard.GetTransform().position).normalized;
                    float m_goalAngle = Vector2.SignedAngle(Vector2.up, rotateDir);

                    Quaternion toRotation = Quaternion.AngleAxis(m_goalAngle, Vector3.forward);
                    guard.GetTransform().rotation = toRotation;
                }
            }
        }
        else
        {
            // Set the agent to the specified location
            GetTransform().position = (Vector2) Data.location.Value.position;
            GetTransform().rotation = Quaternion.AngleAxis(Data.location.Value.rotation, Vector3.forward);
        }
    }


    private Vector2 GetPositionNearNpc(Transform transform, List<Polygon> mapWalls)
    {
        float distanceMultiplier = 0.2f;

        Vector2 place = transform.position + transform.up * distanceMultiplier;
        if (PolygonHelper.IsPointInPolygons(mapWalls, place))
            return place;

        place = transform.position - transform.up * distanceMultiplier;
        if (PolygonHelper.IsPointInPolygons(mapWalls, place))
            return place;

        place = transform.position + transform.right * distanceMultiplier;
        if (PolygonHelper.IsPointInPolygons(mapWalls, place))
            return place;

        place = transform.position - transform.right * distanceMultiplier;
        if (PolygonHelper.IsPointInPolygons(mapWalls, place))
            return place;

        return transform.position;
    }


    // Define a goal for the agent and set the path to navigate to it,
    // param@isForced forces the guard to forget its current path and make a new one.
    public void SetGoal(Vector2 _goal, bool isForced)
    {
        if (!IsBusy() || isForced)
        {
            Goal = _goal;

            // Get the shortest path to the goal
            PathFinding.GetShortestPath(World.GetNavMesh(),
                GetTransform().position, Goal.Value, PathToTake);
        }
    }

    // Update the word state variables
    public virtual void UpdateWldStatV(List<Guard> npcs)
    {
        // The orientation of the NPC's goal
        string heading = "NA";


        if (GetGoal() != null)
        {
            heading = WorldState.GetHeading(GetTransform().position, GetGoal().Value);

            foreach (var npc in npcs)
            {
                UpdtOtherGuardWrldStat(npc);
            }
        }


        // Set the heading value
        WorldState.Set(name + "_goal", heading);

        // NpcSpeed

        // Debug.Log(GetRemainingDistanceToGoal() / NpcSpeed);
    }


    // 
    private void UpdtOtherGuardWrldStat(NPC npc)
    {
        const int decimalSpots = 3;
        float multiplier = Mathf.Pow(10f, decimalSpots);

        // Update the path distance between these two NPCs
        float distance = PathFinding.GetShortestPathDistance(World.GetNavMesh(), npc.GetTransform().position,
            GetTransform().position);

        // Normalize the distance
        distance /= Properties.MaxPathDistance;
        distance = Mathf.Round(distance * multiplier) / multiplier;

        if (!Equals(this, npc))
            WorldState.Set("dst_" + name + "_to_" + npc.name, distance.ToString());


        // Set the distance from the other NPC to the goal of this
        if (!Equals(GetGoal(), null))
        {
            distance = PathFinding.GetShortestPathDistance(World.GetNavMesh(), npc.GetTransform().position,
                GetGoal().Value);

            // Normalize the distance
            distance /= Properties.MaxPathDistance;
            distance = Mathf.Round(distance * multiplier) / multiplier;

            WorldState.Set("dst_goal_" + name + "_to_" + npc.name, distance.ToString());
        }
        else
            WorldState.Set("dst_goal_" + name + "_to_" + npc.name, "NA");


        // Set the distance from the goal of the other NPC and the goal of this NPC
        if (!Equals(this, npc) && !Equals(GetGoal(), null) && !Equals(npc.GetGoal(), null))
        {
            distance = PathFinding.GetShortestPathDistance(World.GetNavMesh(), npc.GetGoal().Value,
                GetGoal().Value);
            distance /= Properties.MaxPathDistance;
            distance = Mathf.Round(distance * multiplier) / multiplier;

            WorldState.Set("dst_goal_" + name + "_to_goal_" + npc.name, distance.ToString());
        }
        else
            WorldState.Set("dst_goal_" + name + "_to_goal_" + npc.name, "NA");
    }


    // If the NPC has a path to take then they are busy.
    public bool IsBusy()
    {
        return PathToTake.Count > 0;
    }

    // Get the agents goal
    public Vector2? GetGoal()
    {
        return Goal;
    }

    // Get the current metrics of the agent's performance
    public abstract LogSnapshot LogNpcProgress();

    // Update NPC metrics
    public virtual void UpdateMetrics(float timeDelta)
    {
    }

    // Move the NPC through it's path
    public void ExecutePlan(IState state, GuardRole? guardRole, float deltaTime)
    {
        if (GetNpcData().intruderPlanner == IntruderPlanner.UserInput)
            MoveByInput(deltaTime);
        else if (PathToTake.Count > 0)
            if (GoStraightTo(PathToTake[0], state, guardRole, deltaTime))
            {
                PathToTake.RemoveAt(0);

                // When the path is over clear the goal.
                if (PathToTake.Count == 0)
                    ClearGoal();
            }
    }

    // Rotate to a specific target and then move towards it; return a boolean if the point is reached or not
    private bool GoStraightTo(Vector3 target, IState state, GuardRole? guardRole, float deltaTime)
    {
        Vector3 currentPosition = GetTransform().position;
        Quaternion currentRotation = GetTransform().rotation;

        // Find the angle needed to rotate to face the desired direction
        Vector2 rotateDir = (target - currentPosition).normalized;
        float m_goalAngle = Vector2.SignedAngle(Vector2.up, rotateDir);

        Quaternion toRotation = Quaternion.AngleAxis(m_goalAngle, Vector3.forward);
        float angleLeft = Mathf.Round(toRotation.eulerAngles.z - currentRotation.eulerAngles.z);


        angleLeft *= angleLeft > 180 ? -1 : 1;
        float rotationStep = Mathf.Min(Mathf.Abs(angleLeft), NpcRotationSpeed * deltaTime);
        GetTransform().rotation = Quaternion.RotateTowards(currentRotation, toRotation,
            rotationStep);

        // Make sure no rotation is due before moving
        float angleDiffThreshold = state is Chase ? 180f : 0f;
        if (Mathf.Abs(angleLeft) > angleDiffThreshold)
            return false;


        // Handle movement
        float distanceLeft = Vector2.Distance(currentPosition, target);

        // How to behavior when heading for the last way point (goal)
        if (PathToTake[PathToTake.Count - 1] == (Vector2) target)
        {
            // If the guard is in patrol, it doesn't need to visit the goal on the path. Just see it
            if (state is Patrol || state is Search)
                distanceLeft -= m_FovRadius - 1f;
        }

        if (distanceLeft > 0.1f)
        {
            float distanceToMove = Mathf.Min(NpcSpeed * deltaTime, distanceLeft);
            m_NpcRb.MovePosition((Vector2) currentPosition +
                                 ((Vector2) GetTransform().up * distanceToMove));

            // Update the total distance traveled
            if (m_LastPosition != null)
                UpdateDistance();

            return false;
        }

        // Since no changes were needed to the position then the agent reached the goal
        return true;
    }

    // Move the NPC by user input.
    public void MoveByInput(float deltaTime)
    {
        Vector2 dir = new Vector2(0f, 0f);

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            dir += Vector2.up;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            dir += Vector2.right;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            dir += Vector2.left;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            dir += Vector2.down;

        if (dir != Vector2.zero)
        {
            float m_goalAngle = Vector2.SignedAngle(Vector2.up, dir);
            Quaternion toRotation = Quaternion.AngleAxis(m_goalAngle, Vector3.forward);
            GetTransform().rotation = Quaternion.RotateTowards(GetTransform().rotation, toRotation,
                NpcRotationSpeed * deltaTime * 10f);
        }

        m_NpcRb.MovePosition((Vector2) GetTransform().position + dir.normalized * (NpcSpeed * deltaTime));
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
        var position = GetTransform().position;
        var distanceTravelled = Vector2.Distance(position, m_LastPosition.Value);
        m_TotalDistanceTravelled += distanceTravelled;
        m_LastPosition = position;
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