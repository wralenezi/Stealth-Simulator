using System;
using UnityEditor;
using UnityEngine;

// A line segment that represents a possible area to search in for an intruder
public class SearchSegment
{
    // the direction the segment expand to
    private Vector2 m_movementDir;

    // Probability of the intruder is in this segment
    private float m_Probability;

    // Maximum length of the search segment divided by two
    private float m_halfMaxLength;

    // The midpoint of the segment when its length is maxed.
    private Vector2 m_segmentMidPoint;

    // The timestamp the segment was created on
    public int timestamp;

    // The first destination of the line segment 
    private WayPoint m_destination1;
    public Vector2 position1;

    // direction of movement the agent last showed
    public Vector2 direction;

    // The second destination of the line segment
    private WayPoint m_destination2;
    public Vector2 position2;


    public SearchSegment(WayPoint dst1, Vector2 startingPos1, WayPoint dst2, Vector2 startingPos2, Vector2 dir,
        float prob, float maxLength, Vector2 segmentMidPoint)
    {
        m_destination1 = dst1;
        position1 = startingPos1;

        m_destination2 = dst2;
        position2 = startingPos2;
        direction = dir;
        m_Probability = prob;

        m_halfMaxLength = maxLength / 2f;
        m_segmentMidPoint = segmentMidPoint;

        m_movementDir = (m_destination1.GetPosition() - m_segmentMidPoint).normalized;

        SetTimestamp(StealthArea.episodeTime);
    }

    // Set the timestamp the search segment was create on
    public void SetTimestamp(float tstamp)
    {
        timestamp = Mathf.RoundToInt(tstamp);
    }

    public float GetTimeStamp()
    {
        return timestamp;
    }

    // Get the age of the search segment
    public int GetAge()
    {
        int age = 2 * (Mathf.RoundToInt(StealthArea.episodeTime) - timestamp);

        return Math.Min(age, Properties.MaxAge);
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
        m_Probability = -0.02f;
    }

    public void IncreaseProbability()
    {
        m_Probability += Properties.ProbabilityIncreaseRate * Time.deltaTime;


        if (m_Probability > 1f)
            m_Probability = 1f;
    }

    public float GetProbability()
    {
        return Mathf.Round(m_Probability * 100f) / 100f;
    }

    // Expand the line segment and propagate them
    public void Expand(float speed)
    {
        float probagationMultiplier = 4f;

        // Expand from one side
        float distance1 = Vector2.Distance(m_segmentMidPoint, position1);

        Vector2 positionDir1 = (position1 - m_segmentMidPoint).normalized;

        // Expand the search segment to the right.
        position1 += m_movementDir * (speed * Time.deltaTime * probagationMultiplier);

        if (distance1 >= m_halfMaxLength && m_movementDir == positionDir1)
        {
            position1 = m_destination1.GetPosition();

            // Place a new search segment
            PropagateDestination(m_destination2, m_destination1);
        }

        // Expand to the other
        float distance2 = Vector2.Distance(m_segmentMidPoint, position2);

        Vector2 positionDir2 = (position2 - m_segmentMidPoint).normalized;

        // Expand the search segment to the left.
        position2 += -m_movementDir * (speed * Time.deltaTime * probagationMultiplier);

        if (distance2 >= m_halfMaxLength && -m_movementDir == positionDir2)
        {
            position2 = m_destination2.GetPosition();

            // Place a new search segment
            PropagateDestination(m_destination1, m_destination2);
        }
    }

    // Once the search reach an end node then propagate new search segments in the adjacent lines
    // param: source is the source of the movement, destination is the point the movement reached and to be propagated from 
    public void PropagateDestination(WayPoint source, WayPoint destination)
    {
        // Don't propagate if the way point already propagated search segments
        if (destination.GetProbability() > 0f || m_Probability <= 0f)
            return;

        // Count the connections except the one the movement came from
        int count = destination.GetLines().Count - 1;

        // if there are several conjunctions distribute the portability among them, if there is only one then decrease it by a fixed value. 
        float newProb = count == 1 ? m_Probability - 0.01f : Mathf.Round((m_Probability / count) * 100f) / 100f;

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
            line.AddSearchSegment(destination.GetPosition(), destination.GetPosition(), dir, newProb);
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
        Handles.Label(m_segmentMidPoint, (Mathf.Round(GetProbability() * 100) / 100f).ToString());


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