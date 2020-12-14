using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// A line segment that represents a possible area to search in for an intruder
public class SearchSegment
{
    // the direction the segment expand to
    private Vector2 m_movementDir;

    // Probability of the intruder is in this segment
    private float m_Probability;

    // The timestamp the segment was created on
    public int timestamp;

    // The first destination of the line segment 
    public WayPoint destination1;
    public Vector2 position1;

    // direction of movement the agent last showed
    public Vector2 direction;

    // The second destination of the line segment
    public WayPoint destination2;
    public Vector2 position2;


    public SearchSegment(WayPoint dst1, Vector2 startingPos1, WayPoint dst2, Vector2 startingPos2, Vector2 dir,
        float prob)
    {
        destination1 = dst1;
        position1 = startingPos1;
        destination2 = dst2;
        position2 = startingPos2;
        direction = dir;
        m_Probability = prob;

        if (dir.x <= 0f && dir.y >= 0f)
            m_movementDir = dir;
        else
            m_movementDir = -dir;

        SetTimestamp(StealthArea.episodeTime);
    }

    public void SetTimestamp(float tstamp)
    {
        timestamp = Mathf.RoundToInt(tstamp);
    }

    public float GetTimeStamp()
    {
        return timestamp;
    }

    public int GetAge()
    {
        int age = 2 * (Mathf.RoundToInt(StealthArea.episodeTime) - timestamp);

        return Math.Min(age, Properties.MaxAge);
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
        float distance1 = Vector2.Distance(position1, destination1.GetPosition());

        if (distance1 > 0.1f &&
            GeometryHelper.IsPointOnLine(destination1.GetPosition(), destination2.GetPosition(), position1))
        {
            position1 += m_movementDir * speed * Time.fixedDeltaTime * 1.2f;
        }
        else
            PropagateDestination(destination2, destination1);

        float distance2 = Vector2.Distance(position2, destination2.GetPosition());
        if (distance2 > 0.1f &&
            GeometryHelper.IsPointOnLine(destination1.GetPosition(), destination2.GetPosition(), position1))
        {
            position2 += -m_movementDir * speed * Time.fixedDeltaTime * 1.2f;
        }
        else
            PropagateDestination(destination1, destination2);
    }

    // Once the search reach an end node then propagate new search segments in the adjacent lines
    // param: source is the source of the movement, destination is the point the movement reached and to be propagated from 
    public void PropagateDestination(WayPoint source, WayPoint destination)
    {
        // Don't propagate if the probability is less than the probability of the road map node
        // if (m_Probability <= destination.GetProbability())
        if (destination.GetProbability() > 0f)
            return;

        // Count the connections except the one the movement came from
        int count = destination.GetLines().Count - 1;


        float newProb = Mathf.Round((m_Probability / count) * 100f) / 100f;

        // Assign probability to that road map point to mark it
        destination.SetProbability(newProb);

        // Create search segments in the other 
        foreach (var line in destination.GetLines())
        {
            if (line.IsPointPartOfLine(destination) && line.IsPointPartOfLine(source))
                continue;

            WayPoint newDest = line.wp1 == destination ? line.wp2 : line.wp1;
            Vector2 dir = (newDest.GetPosition() - destination.GetPosition()).normalized;

            line.AddSearchSegment(destination.GetPosition(), destination.GetPosition(), dir, newProb);
        }
    }


    // Draw the search segment
    public void Draw()
    {
        var thickness = 20;

        Vector2 expansion = m_movementDir * 0.035f;
        Handles.DrawBezier(position1 + expansion, position2 - expansion, position1 + expansion, position2 - expansion,
            Properties.GetSegmentColor(GetProbability()),
            null,
            thickness);

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