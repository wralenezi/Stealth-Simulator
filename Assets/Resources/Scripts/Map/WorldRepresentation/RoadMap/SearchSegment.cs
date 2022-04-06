using System;
using UnityEditor;
using UnityEngine;

// A line segment that represents a possible area to search in for an intruder
public class SearchSegment
{
    // Check if the segment si propagated to once already.
    public bool isPropagated;

    // Probability of the intruder is in this segment
    private float m_Probability;
    public const float MinProbability = 0f;

    // The timestamp the segment was created on
    public float timestamp;

    // The first destination of the line segment 
    private WayPoint m_destination1;

    public Vector2 position1;

    // If the segment reached it's first destination
    public bool reached1;

    // The second destination of the line segment
    private WayPoint m_destination2;

    public Vector2 position2;

    // If the segment reached it's second destination
    public bool reached2;

    public bool IsObserved;

    public SearchSegment(WayPoint dst1, Vector2 startingPos1, WayPoint dst2, Vector2 startingPos2)
    {
        m_destination1 = dst1;
        position1 = startingPos1;

        m_destination2 = dst2;
        position2 = startingPos2;

        Reset();
    }

    // Set the default probability when the road map is not used
    public void SetDefaultProb()
    {
        m_Probability = MinProbability;
    }

    public void Reset()
    {
        SetDefaultProb();
        SetTimestamp(StealthArea.GetElapsedTime());
        isPropagated = false;
    }

    // Set the search segment with new values
    public void SetSsData(Vector2 startingPos1, Vector2 startingPos2, float prob)
    {
        position1 = startingPos1;
        position2 = startingPos2;
        SetProb(prob);
        reached1 = false;
        reached2 = false;
    }

    // Set the timestamp the search segment was create on
    public void SetTimestamp(float tstamp)
    {
        timestamp = tstamp;
    }

    public float GetLastSeenTimeStamp()
    {
        return timestamp;
    }

    public Vector2 GetMidPoint()
    {
        return (position1 + position2) / 2f;
    }

    // Get the age of the search segment
    public float GetAge()
    {
        float age = StealthArea.GetElapsedTime() - timestamp;
        return age;
    }

    // Get the fitness value of the search segment
    public float GetFitness()
    {
        return GetProbability();
    }

    // Reset the segment after it has been seen
    public void Seen()
    {
        IsObserved = true;
        SetTimestamp(StealthArea.GetElapsedTime());
        SetProb(MinProbability);
        reached1 = false;
        reached2 = false;
    }

    public void notSeen()
    {
        if (IsObserved) SetTimestamp(StealthArea.GetElapsedTime());
        IsObserved = false;
    }

    // Set the probability of the search 
    public void SetProb(float prob)
    {
        m_Probability = prob;
    }

    public bool IsReached()
    {
        return reached1 && reached2;
    }

    public void AddProbability(float prob)
    {
        m_Probability += prob;
        SetProb(Mathf.Clamp(m_Probability, MinProbability, 1f));
    }

    public float GetProbability()
    {
        return m_Probability;
    }

    // Expand the search segment
    public void Expand(float speed, float timeDelta)
    {
        // The slowed expansion speed; to give a chance to slow down the expansion of seen segments.
        float slowSpeed = 1f / Mathf.Pow(10, Time.timeScale);

        // Expand from one side
        float distance1 = Vector2.Distance(m_destination1.GetPosition(), position1);

        Vector2 positionDir1 = (m_destination1.GetPosition() - position1).normalized;

        float expansionSpeed1 = reached1 ? slowSpeed : speed;

        // Expand the search segment to the right.
        float expansionStep1 = Mathf.Min(distance1,
            expansionSpeed1 * timeDelta * Properties.PropagationMultiplier);

        if (distance1 <= expansionStep1)
        {
            reached1 = true;
            position1 = m_destination1.GetPosition();
        }
        else
            position1 += positionDir1 * expansionStep1;


        // Expand to the other
        float distance2 = Vector2.Distance(m_destination2.GetPosition(), position2);

        Vector2 positionDir2 = (m_destination2.GetPosition() - position2).normalized;

        float expansionSpeed2 = reached2 ? slowSpeed : speed;

        // Expand the search segment to the left.
        float expansionStep2 = Mathf.Min(distance2,
            expansionSpeed2 * timeDelta * Properties.PropagationMultiplier);

        if (distance2 <= expansionStep2)
        {
            reached2 = true;
            position2 = m_destination2.GetPosition();
        }
        else
            position2 += positionDir2 * expansionStep2;
    }


    // Once the search reach an end node then propagate new search segments in the adjacent lines
    // param: source is the source of the movement, destination is the point the movement reached and to be propagated from 
    public void PropagateDestination(int index)
    {
        WayPoint wayPoint = index == 1 ? m_destination1 : m_destination2;

        // Don't propagate if the segment is zero
        if (Math.Abs(GetProbability() - MinProbability) < 0.00001f) return;

        // Give a portion of the probability
        float newProb = GetProbability() * (PathFinding.Instance.longestShortestPath - Properties.GetMaxEdgeLength() * 1.5f) /
                        PathFinding.Instance.longestShortestPath;
        
        if(newProb < 0.001f) return;

        // Create search segments in the other points connected to this destination
        foreach (var line in wayPoint.GetLines(false))
        {
            // Make sure the new point is not propagated before
            if (line.GetSearchSegment() == this || line.GetSearchSegment().isPropagated ||
                line.GetSearchSegment().IsObserved) continue;

            // Don't propagate the probability if the destination has a higher value
            if (line.GetSearchSegment().GetProbability() > GetProbability()) break;

            // Create the new search segment 
            line.PropagateToSegment(wayPoint.GetPosition(), newProb, StealthArea.GetElapsedTime());
        }
    }


    // Draw the search segment
    public void Draw(string label)
    {
        Gizmos.color = Properties.GetSegmentColor(GetProbability());
        Gizmos.DrawLine(position1, position2);
#if UNITY_EDITOR
        Handles.Label(GetMidPoint(), label);
#endif
    }
}