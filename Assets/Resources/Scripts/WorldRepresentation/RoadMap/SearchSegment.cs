using System;
using UnityEditor;
using UnityEngine;

// A line segment that represents a possible area to search in for an intruder
public class SearchSegment
{
    // Probability of the intruder is in this segment
    private float m_Probability;

    // The midpoint of the segment when its length is maxed.
    private Vector2 m_segmentMidPoint;

    // The timestamp the segment was created on
    public int timestamp;

    // The first destination of the line segment 
    private WayPoint m_destination1;
    public Vector2 position1;
    // If the segment reached it's first destination
    private bool reached1;

    // The second destination of the line segment
    private WayPoint m_destination2;
    public Vector2 position2;
    // If the segment reached it's second destination
    private bool reached2;

    public SearchSegment(WayPoint dst1, Vector2 startingPos1, WayPoint dst2, Vector2 startingPos2,
        float prob, Vector2 segmentMidPoint)
    {
        m_destination1 = dst1;
        position1 = startingPos1;

        m_destination2 = dst2;
        position2 = startingPos2;
        m_Probability = prob;

        m_segmentMidPoint = segmentMidPoint;
        
        SetTimestamp(StealthArea.episodeTime);
    }

    // Set the timestamp the search segment was create on
    public void SetTimestamp(float tstamp)
    {
        timestamp = Mathf.RoundToInt(tstamp);
    }

    public Vector2 GetMidPoint()
    {
        return m_segmentMidPoint;
    }

    public float GetTimeStamp()
    {
        return timestamp;
    }

    // Get the age of the search segment
    public float GetAge()
    {
        int age = (Mathf.RoundToInt(StealthArea.episodeTime) - timestamp);

        return Mathf.Min(age, Properties.MaxAge);
    }

    // Get the fitness value of the search segment
    public float GetFitness()
    {
        return GetProbability();
    }

    public Vector2 GetDestination1()
    {
        return m_destination1.GetPosition();
    }

    // Reset the segment after it has been seen
    public void Reset()
    {
        SetTimestamp(StealthArea.episodeTime);
        m_Probability = -0.1f;
    }

    public void IncreaseProbability(float deltaTime)
    {
        if (m_Probability < 1f)
            m_Probability += Properties.ProbabilityIncreaseRate * deltaTime;
        else
            m_Probability = 1f;
    }

    public float GetProbability()
    {
        return Mathf.Round(m_Probability * 100f) / 100f;
    }

    public void Expand(float speed, float timeDelta)
    {
        float slowSpeed = 0.01f;
        
        // Expand from one side
        float distance1 = Vector2.Distance(m_destination1.GetPosition(), position1);

        Vector2 positionDir1 = (m_destination1.GetPosition() - position1).normalized;

        float expansionSpeed1 = reached1 ? slowSpeed : speed;
        
        // Expand the search segment to the right.
        float expansionStep1 = Mathf.Min(distance1, expansionSpeed1 * timeDelta * Properties.ProbagationMultiplier);

        position1 += positionDir1 * (expansionStep1);

        if (distance1 < 0.1f)
        {
            reached1 = true;
            position1 = m_destination1.GetPosition();

            // Place a new search segment
            PropagateDestination(m_destination2, m_destination1, speed);
        }

        // Expand to the other
        float distance2 = Vector2.Distance(m_destination2.GetPosition(), position2);

        Vector2 positionDir2 = (m_destination2.GetPosition() - position2).normalized;

        float expansionSpeed2 = reached2 ? slowSpeed : speed;

        // Expand the search segment to the left.
        float expansionStep2 = Mathf.Min(distance2, expansionSpeed2 * timeDelta * Properties.ProbagationMultiplier);

        position2 += positionDir2 * (expansionStep2);

        if (distance2 < 0.1f)
        {
            reached2 = true;
            position2 = m_destination2.GetPosition();

            // Place a new search segment
            PropagateDestination(m_destination1, m_destination2, speed);
        }
    }


    // Once the search reach an end node then propagate new search segments in the adjacent lines
    // param: source is the source of the movement, destination is the point the movement reached and to be propagated from 
    public void PropagateDestination(WayPoint source, WayPoint destination, float speed)
    {
        // Don't propagate if the way point already propagated search segments
        if (m_Probability < 0f)
            return;

        // Count the connections except the one the movement came from
        int count = destination.GetLines().Count - 1;

        // if there are several conjunctions distribute the portability among them, if there is only one then decrease it by a fixed value. 
        // float newProb =
        //     m_Probability - Properties.ProbabilityIncreaseRate / (0.5f * Properties.SpeedMultiplyer); //0.2f / speed;

        // float newProb = count == 1 ? m_Probability : Mathf.Round((m_Probability / count) * 100f) / 100f;
        
        float newProb = count == 1 ? m_Probability : m_Probability - 0.1f;

        // Assign probability to that road map point to mark it
        destination.SetProbability(newProb);

        // Create search segments in the other points connected to this destination
        foreach (var line in destination.GetLines())
        {
            // Make sure the new point is not visited
            if (line.IsPointPartOfLine(destination) && line.IsPointPartOfLine(source))
                continue;

            // Assign the new destination 
            WayPoint newDest = line.wp1 == destination ? line.wp2 : line.wp1;
            Vector2 dir = (newDest.GetPosition() - destination.GetPosition()).normalized;

            // Make sure there are no more than 1 search segments in the line
            if (line.GetSearchSegments().Count > 0)
                continue;

            // Create the new search segment 
            line.AddSearchSegment(destination.GetPosition(), destination.GetPosition(), newProb);
        }
    }


    // Draw the search segment
    public void Draw()
    {
        // var thickness = 5;
        //
        // Vector2 expansion = m_movementDir * 0.035f;
        // Handles.DrawBezier(position1 + expansion, position2 - expansion, position1 + expansion, position2 - expansion,
        //     //Properties.GetSegmentColor(GetProbability()),
        //     Color.red,
        //     null,
        //     thickness);

        Gizmos.color = Properties.GetSegmentColor(GetProbability());
        Gizmos.DrawLine(position1, position2);
        //

        // Handles.Label(position1, Vector2.Distance(position1,m_segmentMidPoint).ToString());
        // if (GetProbability() > 0f)
        //     Handles.Label(m_segmentMidPoint, (Mathf.Round(GetProbability() * 100) / 100f).ToString());


        // Handles.Label(position2, Vector2.Distance(position2,m_segmentMidPoint).ToString());
        // Handles.Label(m_destination2.GetPosition()+ Vector2.down, "d2");

        // Gizmos.color = Color.green;
        // float sphereR = 0.05f;
        // Gizmos.DrawSphere(position1, sphereR);
        // Gizmos.DrawSphere(destination1.GetPosition(), sphereR);

        // Gizmos.color = Color.red;
        // Gizmos.DrawSphere(position2, sphereR);
        // Gizmos.DrawSphere(destination2.GetPosition(), sphereR);

        //Vector2 mid = (position1 + position2) / 2f;
        //Handles.Label(mid, GetProbability().ToString());
    }
}