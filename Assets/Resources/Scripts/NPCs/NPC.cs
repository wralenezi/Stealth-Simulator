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

    // NPC Rigid body
    private Rigidbody2D m_NpcRb;

    // The world representation
    protected WorldRep World;

    // The area of the game
    protected StealthArea Area;


    // The plan to follow
    protected List<Vector2> PathToTake;
    protected Vector2? Goal;

    //************** Logging data ***************//
    // Variables for the distance travelled by a character
    // The last position the NPC was logged until
    private Vector2? m_LastPosition;

    // The total distance the guard travelled
    [SerializeField] private float m_TotalDistanceTravelled;

    public override void Initialize()
    {
        PathToTake = new List<Vector2>();
        World = transform.parent.parent.Find("Map").GetComponent<WorldRep>();
    }

    public void SetArea(StealthArea area)
    {
        Area = area;
    }

    public void SetPosition()
    {
        m_LastPosition = transform.position;
    }

    // The set up of the start of the episode
    public override void OnEpisodeBegin()
    {
        PathToTake.Clear();
        m_TotalDistanceTravelled = 0f;
        
        // Randomly place the NPC on the map
        int polygonIndex = Random.Range(0, Area.GetNavMesh().Count);
        transform.position = Area.GetNavMesh()[polygonIndex].GetRandomPosition();
        
        SetPosition();
    }

    // Set the NPC data
    public void SetNpcData(NpcData data)
    {
        Data = data;
        m_NpcRb = GetComponent<Rigidbody2D>();
    }

    public void AssignGoal()
    {
        if (Goal == null)
        {
            RequestDecision();
        }
    }

    public abstract LogSnapshot LogNpcProgress();

    public virtual void ExecutePlan()
    {
    }


    // Rotate to a specific target and then move towards it; return a boolean if the point is reached or not
    protected bool GoStraightTo(Vector3 target, IState state)
    {
        // Calculate how much rotation must be done
        Vector2 rotateDir = (target - transform.position).normalized;
        float m_goalAngle = Vector2.SignedAngle(Vector2.up, rotateDir);
        Quaternion toRotation = Quaternion.AngleAxis(m_goalAngle, Vector3.forward);
        float angleLeft = Mathf.Round(toRotation.eulerAngles.z - transform.rotation.eulerAngles.z);

        // Make sure no rotation is due before moving
        if (Mathf.Abs(angleLeft) > 0f)
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, toRotation,
                Properties.NpcRotationSpeed * Time.fixedDeltaTime);
            return false;
        }

        // Handle movement
        float distanceLeft = Vector2.Distance(transform.position, target);

        // The guard doesn't need to visit the goal on the path. Just see it
        if (PathToTake[PathToTake.Count - 1] == (Vector2) target)
        {
            if (!(state is Chase))
                distanceLeft -= Properties.ViewRadius - 0.5f;
        }

        if (distanceLeft > 0.1f)
        {
            // Vector2 dir = (target - transform.position).normalized;
            m_NpcRb.MovePosition(m_NpcRb.position +
                                 ((Vector2) transform.up * (Properties.NpcSpeed * Time.fixedDeltaTime)));

            // Update the total distance traveled
            if (m_LastPosition != null)
                UpdateDistance();


            return false;
        }

        // Since no changes were needed to the position then the agent reached the goal
        return true;
    }

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
                Properties.NpcRotationSpeed * Time.fixedDeltaTime * 10f);
        }

        m_NpcRb.MovePosition(m_NpcRb.position + (dir.normalized * (Properties.NpcSpeed * Time.fixedDeltaTime)));
    }


    // Update the total distance travelled by the NPC
    private void UpdateDistance()
    {
        var position = transform.position;
        var distanceTravelled = Vector2.Distance(position, m_LastPosition.Value);
        m_TotalDistanceTravelled += distanceTravelled;
        m_LastPosition = position;
    }

    public void SetLastPositionPosition()
    {
        m_LastPosition = transform.position;
    }

    public float GetTravelledDistance()
    {
        return m_TotalDistanceTravelled;
    }

    public NpcType GetNpcType()
    {
        return Data.npcType;
    }
}