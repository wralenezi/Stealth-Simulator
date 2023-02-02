using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;


// Abstract class of an NPC
public abstract class NPC : MonoBehaviour
{
    // NPC data
    protected NpcData Data;

    // NPC Rigid body for physics
    private Rigidbody2D m_NpcRb;

    private Vector2 _velocity;

    private Transform m_transform;

    // show the NPCs path to take
    public bool ShowPath;

    // Is Controlled by user
    public bool ControlledByUser;

    // The path the agent is meant to follow
    private List<Vector2> _pathToTake;
    private List<Vector2> _fullPath;

    private Vector2? FacingDirection;

    // if the agent has to reach the goal or simply see it.
    private bool m_hasToReach = false;

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
    private Vector2? _lastPosition;

    // The total distance the guard travelled
    [SerializeField] private float _totalDistanceTravelled;

    // Voice parameters for WebGL
    private VoiceParams m_voiceParam;

    // Called when the agent is first created
    public virtual void Initiate(NpcData data, VoiceParams _voice)
    {
        m_voiceParam = _voice;
        _pathToTake = new List<Vector2>();
        _fullPath = new List<Vector2>();

        LinesToPassThrough = new List<RoadMapLine>();

        m_transform = transform;

        Data = data;
        m_NpcRb = GetComponent<Rigidbody2D>();
        m_FovPolygon = new List<Polygon>() {new Polygon()};

        AddFoV(Properties.GetFovAngle(Data.npcType), Properties.GetFovRadius(Data.npcType),
            Properties.GetFovColor(Data.npcType));

        // ShowPath = true;
    }

    // The set up of the start of the episode
    public virtual void ResetNpc()
    {
        ClearGoal();
        _totalDistanceTravelled = 0f;

        // SetPosition();
        m_FovPolygon[0].Clear();

        Renderer = GetComponent<Renderer>();
        FovRenderer = Fov.GetComponent<Renderer>();
    }

    // Clear the designated goal and path to take
    public virtual void ClearGoal()
    {
        _pathToTake.Clear();
        _fullPath.Clear();
        ClearLines();
    }

    // Remove the counter the NPC left on the road map and clear them from the list.
    public abstract void ClearLines();


    // Update the agent's last position
    private void SetPosition()
    {
        _velocity = Equals(_lastPosition, null)
            ? Vector2.zero
            : (Vector2) GetTransform().position - _lastPosition.Value;

        _lastPosition = GetTransform().position;
    }

    // Add a field of view component to the NPC
    private void AddFoV(float angle, float radius, Color color)
    {
        // The game object that contains the field of view
        GameObject fovGameObject = new GameObject("FoV");

        // Assign it as a child to the guard
        var transform1 = transform;
        fovGameObject.transform.parent = transform1;
        fovGameObject.transform.position = transform1.position;

        m_FovRadius = Properties.GetFovRadius(Data.npcType);

        Fov = fovGameObject.AddComponent<FieldOfView>();
        Fov.Initiate(angle, radius, color);
    }

    public void SetFovColor(Color32 color)
    {
        Fov.ChangeColor(color);
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

    public float GetCurrentSpeed()
    {
        return _velocity.magnitude;
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

    public VoiceParams GetVoiceParam()
    {
        return m_voiceParam;
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
        return _pathToTake;
    }

    public List<Vector2> GetFullPath()
    {
        _fullPath.Clear();

        _fullPath.Add(GetTransform().position);

        foreach (var p in GetPath())
            _fullPath.Add(p);

        return _fullPath;
    }

    public List<Polygon> GetFov()
    {
        return m_FovPolygon;
    }

    public void ForceToReachGoal(bool _toReach)
    {
        m_hasToReach = _toReach;
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
        ).normalized;
    }

    // Place the NPC's start position
    public void ResetLocation(List<MeshPolygon> navMesh, List<Intruder> intruders, List<Guard> guards,
        Session sessionInfo)
    {
        // If there is no location specified for the agent to be set in; place them randomly.
        if (Data.location == null)
        {
            // if the npc is an intruder then place in front of one of the guards
            if (Data.npcType == NpcType.Intruder)
            {
                switch (sessionInfo.scenario)
                {
                    // If the scenario is a chase then place the intruder somewhere a guard can see
                    case Scenario.Chase:
                        GetTransform().position = GetPositionNearNpc(guards, MapManager.Instance.GetWalls());
                        return;

                    case Scenario.Stealth:
                        GetTransform().position = PathFinding.Instance.GetCornerFurthestFromPoint(
                            CollectablesManager.Instance.GetGoalPosition(GameType.CoinCollection).Value,
                            Properties.NpcRadius * 2f);

                        break;
                }
            }
            else if (Data.npcType == NpcType.Guard)
            {
                switch (sessionInfo.guardSpawnMethod)
                {
                    case GuardSpawnType.Random:
                        // Randomly place the NPC on the map
                        int polygonIndex = Random.Range(0, navMesh.Count);
                        GetTransform().position = navMesh[polygonIndex].GetRandomPosition();
                        break;

                    case GuardSpawnType.Separate:
                        // Randomly place the guards away from each other
                        GetTransform().position =
                            GetPositionFarFromGuards(GetNpcData().id - 1, intruders, guards,
                                MapManager.Instance.GetWalls()[0]);
                        break;

                    case GuardSpawnType.Goal:
                        //Place the guard on the goal's position
                        GetTransform().position =
                            CollectablesManager.Instance.GetGoalPosition(GameType.CoinCollection).Value;
                        break;
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

    private Vector2 GetPositionFarFromGuards(int index, List<Intruder> intruders, List<Guard> guards,
        List<MeshPolygon> navMesh)
    {
        float minDistanceFromIntruder = PathFinding.Instance.longestShortestPath * 0.3f;

        int attempts = 250;
        float maxSqrMag = Mathf.NegativeInfinity;
        Vector2? furthestPoint = null;

        while (attempts > 0)
        {
            attempts--;

            int polygonIndex = Random.Range(0, navMesh.Count);
            Vector2 pos = navMesh[polygonIndex].GetRandomPosition();

            bool inPoly = navMesh[polygonIndex].IsCircleContainedInPolygon(pos, Properties.NpcRadius);
            if (!inPoly) continue;

            if (intruders.Count > 0)
            {
                Vector2 intruderPosition = intruders[0].GetTransform().position;
                bool isFarEnough = minDistanceFromIntruder <
                                   PathFinding.Instance.GetShortestPathDistance(pos, intruderPosition);
                if (!isFarEnough) continue;
            }

            float sqrMag = 0f;
            for (int i = 0; i <= index; i++)
            {
                Vector2 offset = pos - (Vector2) guards[i].GetTransform().position;
                sqrMag += offset.sqrMagnitude;
            }

            if (maxSqrMag <= sqrMag)
            {
                maxSqrMag = sqrMag;
                furthestPoint = pos;
            }
        }


        if (!Equals(furthestPoint, null)) return furthestPoint.Value;


        int pIndex = Random.Range(0, navMesh.Count);
        return navMesh[pIndex].GetCentroidPosition();
    }

    private Vector2 GetPositionFarFromGuards(int index, List<Intruder> intruders, List<Guard> guards, Polygon outerWall)
    {
        float minDistanceFromIntruder = PathFinding.Instance.longestShortestPath * 0.3f;

        int attempts = 250;
        float maxMinSqrMag = Mathf.NegativeInfinity;
        Vector2? furthestPoint = null;

        while (attempts > 0)
        {
            attempts--;

            int vertexIndex = Random.Range(0, outerWall.GetVerticesCount());
            Vector2 pos = outerWall.GetCorner(vertexIndex);

            if (intruders.Count > 0)
            {
                Vector2 intruderPosition = intruders[0].GetTransform().position;
                bool isFarEnough = minDistanceFromIntruder <
                                   PathFinding.Instance.GetShortestPathDistance(pos, intruderPosition);
                if (!isFarEnough) continue;
            }

            float minSqrMag = Mathf.Infinity;
            for (int i = 0; i < index; i++)
            {
                Vector2 offset = pos - (Vector2) guards[i].GetTransform().position;

                if (minSqrMag > offset.sqrMagnitude)
                    minSqrMag = offset.sqrMagnitude;
            }

            if (maxMinSqrMag < minSqrMag)
            {
                maxMinSqrMag = minSqrMag;
                furthestPoint = pos;
            }
        }


        if (!Equals(furthestPoint, null)) return furthestPoint.Value;


        int pIndex = Random.Range(0, outerWall.GetVerticesCount());
        return outerWall.GetCorner(pIndex);
    }

    private Vector2 PlaceInGuardFoV(List<Guard> guards, List<MeshPolygon> navMesh)
    {
        foreach (var guard in guards)
        {
            Vector2 center = guard.GetFovPolygon().GetCentroidPosition();
            if (guard.GetFovPolygon().IsPointInPolygon(center, true))
                return center;
        }

        int polygonIndex = Random.Range(0, navMesh.Count);
        return navMesh[polygonIndex].GetCentroidPosition();
    }


    private Vector2 GetPositionNearNpc(List<Guard> guards, List<Polygon> mapWalls)
    {
        float distanceMultiplier = 1.5f;

        foreach (var guard in guards)
        {
            Vector2 place = guard.GetTransform().position + (Vector3) Vector2.up * distanceMultiplier;
            if (PolygonHelper.IsPointInPolygons(mapWalls, place))
            {
                guard.SetOrientationTowards(place);
                return place;
            }

            place = guard.GetTransform().position - (Vector3) Vector2.up * distanceMultiplier;
            if (PolygonHelper.IsPointInPolygons(mapWalls, place))
            {
                guard.SetOrientationTowards(place);
                return place;
            }

            place = guard.GetTransform().position + (Vector3) Vector2.right * distanceMultiplier;
            if (PolygonHelper.IsPointInPolygons(mapWalls, place))
            {
                guard.SetOrientationTowards(place);
                return place;
            }

            place = guard.GetTransform().position - (Vector3) Vector2.right * distanceMultiplier;
            if (PolygonHelper.IsPointInPolygons(mapWalls, place))
            {
                guard.SetOrientationTowards(place);
                return place;
            }
        }

        return transform.position;
    }

    private Vector2? GetSafePosition(List<Guard> guards, List<MeshPolygon> navMesh)
    {
        Vector2? position = null;

        int attempts = 100;

        float safeDistance = PathFinding.Instance.longestShortestPath * 0.4f;

        while (Equals(position, null) && attempts > 0)
        {
            int polygonIndex = Random.Range(0, navMesh.Count);
            Vector2 randomPosition = navMesh[polygonIndex].GetCentroidPosition();

            bool inMap = false;
            foreach (var p in navMesh)
            {
                inMap = p.IsCircleContainedInPolygon(randomPosition, Properties.NpcRadius);

                if (inMap)
                    break;
            }

            if (!inMap)
                break;


            bool isSeen = true;
            foreach (var g in guards)
            {
                isSeen = GeometryHelper.IsCirclesVisible(randomPosition, g.GetTransform().position,
                    Properties.NpcSpeed, "Wall");

                Vector2 offset = (Vector2) g.GetTransform().position - randomPosition;
                float sqrMag = offset.sqrMagnitude;

                if (isSeen || sqrMag < safeDistance * safeDistance)
                    break;
            }

            if (!isSeen)
                position = randomPosition;

            attempts--;
        }

        return position;
    }


    // Define a goal for the agent and set the path to navigate to it,
    // param@isForced forces the guard to forget its current path and make a new one.
    public void SetDestination(Vector2 _goal, bool hasToReach, bool isForced)
    {
        if (IsBusy() && !isForced) return;

        m_hasToReach = hasToReach;

        // Get the shortest path to the goal
        PathFinding.Instance.GetShortestPath(GetTransform().position, _goal, ref _pathToTake);

        _pathToTake.RemoveAt(0);
    }

    private void LookAt(Vector2 position)
    {
        Vector2 direction = position - (Vector2) GetTransform().position;
        SetDirection(direction.normalized);
    }

    private void SetOrientationTowards(Vector2 position)
    {
        Vector2 direction = position - (Vector2) GetTransform().position;
        float goalAngle = Vector2.SignedAngle(Vector2.up, direction.normalized);
        GetTransform().rotation = Quaternion.AngleAxis(goalAngle, Vector3.forward);
    }



    public void SetDirection(Vector2 direction)
    {
        FacingDirection = direction;
    }

    public Vector2? GetGoal()
    {
        Vector2? goal = null;
        if (_pathToTake.Count > 0) goal = _pathToTake[_pathToTake.Count - 1];
        return goal;
    }


    // If the NPC has a path to take then they are busy.
    public virtual bool IsBusy()
    {
        return _pathToTake.Count > 0;
    }

    // Get the current metrics of the agent's performance
    public abstract LogSnapshot LogNpcProgress();

    // Update NPC metrics
    public virtual void UpdateMetrics(float timeDelta)
    {
    }

    // Move the NPC through it's path
    public void ExecutePlan(State state, float deltaTime)
    {
        // Update the total distance traveled
        UpdateDistance();

        if (ControlledByUser)
            MoveByInput(deltaTime);
        else if (_pathToTake.Count > 0)
            if (GoStraightTo(_pathToTake[0], deltaTime))
            {
                _pathToTake.RemoveAt(0);

                // When the path is over clear the goal.
                if (_pathToTake.Count == 0) ClearGoal();
            }
    }

    // Rotate to a specific target and then move towards it; return a boolean if the point is reached or not
    private bool GoStraightTo(Vector3 target, float deltaTime)
    {
        Vector3 currentPosition = GetTransform().position;
        Quaternion currentRotation = GetTransform().rotation;

        // Handle movement
        float distanceLeft = Vector2.Distance(currentPosition, target);

        // Find the angle needed to rotate to face the desired direction
        Vector2 rotateDir;

        if (Equals(GetGoal().Value, (Vector2) target) && !Equals(FacingDirection, null) && distanceLeft <= 0.1f)
            rotateDir = FacingDirection.Value;
        else
            rotateDir = (target - currentPosition).normalized;


        float goalAngle = Vector2.SignedAngle(Vector2.up, rotateDir);

        Quaternion toRotation = Quaternion.AngleAxis(goalAngle, Vector3.forward);
        float angleLeft = Mathf.Round(toRotation.eulerAngles.z - currentRotation.eulerAngles.z);

        angleLeft *= angleLeft > 180 ? -1 : 1;
        float rotationStep = Mathf.Min(Mathf.Abs(angleLeft), NpcRotationSpeed * deltaTime);
        GetTransform().rotation = Quaternion.RotateTowards(currentRotation, toRotation,
            rotationStep);

        // Make sure no rotation is due before moving
        float angleDiffThreshold = 180f / Mathf.Abs(angleLeft);

        if (Mathf.Abs(angleLeft) > angleDiffThreshold)
            return false;

        // How to behavior when heading for the last way point (goal)
        // ReSharper disable once PossibleInvalidOperationException
        if (GetGoal().Value == (Vector2) target)
        {
            // If the guard is in patrol, it doesn't need to visit the goal on the path. Just see it
            if (!m_hasToReach) distanceLeft -= m_FovRadius * 0.7f;
        }

        if (distanceLeft > 0.1f)
        {
            float distanceToMove = Mathf.Min(NpcSpeed * deltaTime, distanceLeft);
            m_NpcRb.MovePosition((Vector2) currentPosition +
                                 ((Vector2) GetTransform().up * distanceToMove));

            return false;
        }

        // This is to set the default value to false.
        m_hasToReach = false;

        FacingDirection = null;

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

        if (_pathToTake.Count == 0) return totalDistance;

        totalDistance += Vector2.Distance(GetTransform().position, _pathToTake[0]);

        if (_pathToTake.Count < 2) return totalDistance;

        for (int i = 0; i < _pathToTake.Count - 1; i++)
        {
            totalDistance += Vector2.Distance(_pathToTake[i], _pathToTake[i + 1]);
        }

        return totalDistance;
    }

    // Update the total distance travelled by the NPC for logging purposes
    private void UpdateDistance()
    {
        var position = GetTransform().position;

        _velocity = Equals(_lastPosition, null)
            ? Vector2.zero
            : (Vector2) position - _lastPosition.Value;

        if (!Equals(_lastPosition, null))
        {
            var distanceTravelled = Vector2.Distance(position, _lastPosition.Value);
            _totalDistanceTravelled += distanceTravelled;
        }

        _lastPosition = position;
    }

    public float GetTravelledDistance()
    {
        return _totalDistanceTravelled;
    }

    public NpcData GetNpcData()
    {
        return Data;
    }

    public float GetNpcSpeed()
    {
        return NpcSpeed;
    }


    public virtual void OnDrawGizmos()
    {
        List<Vector2> path = GetFullPath();
        if (ShowPath && _pathToTake != null & _pathToTake.Count > 0)
        {
            for (int i = 0; i < path.Count - 1; i++)
            {
                Gizmos.DrawLine(path[i], path[i + 1]);
                Gizmos.DrawSphere(path[i], 0.025f);
            }

            Gizmos.DrawSphere(path[path.Count - 1], 0.025f);
        }
    }
}

public struct VoiceParams
{
    // The pitch of the voice
    public float Pitch;

    // The voice index on the speechSynthesis voices 
    public int VoiceIndex;

    public VoiceParams(int _voiceIndex, float _pitch)
    {
        VoiceIndex = _voiceIndex;
        Pitch = _pitch;
    }
}


public enum GuardSpawnType
{
    Random,

    Goal,

    Separate,

    Scripted
}