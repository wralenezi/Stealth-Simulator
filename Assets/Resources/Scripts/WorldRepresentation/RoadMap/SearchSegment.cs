﻿using System;
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

    // The midpoint of the segment when its length is maxed.
    private Vector2 m_segmentMidPoint;

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

    public SearchSegment(WayPoint dst1, Vector2 startingPos1, WayPoint dst2, Vector2 startingPos2)
    {
        m_destination1 = dst1;
        position1 = startingPos1;

        m_destination2 = dst2;
        position2 = startingPos2;

        m_segmentMidPoint = (m_destination1.GetPosition() + m_destination2.GetPosition()) / 2f;

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
        SetTimestamp(0f);
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

    public Vector2 GetMidPoint()
    {
        return m_segmentMidPoint;
    }

    // Get the age of the search segment
    public float GetAge()
    {
        float age = StealthArea.episodeTime - timestamp;

        return age;
    }

    public bool IsActive()
    {
        return timestamp != 0f;
    }

    // Get the fitness value of the search segment
    public float GetFitness()
    {
        return GetProbability();
    }

    // Reset the segment after it has been seen
    public void Seen()
    {
        SetTimestamp(StealthArea.episodeTime);
        SetProb(MinProbability);
        reached1 = false;
        reached2 = false;
    }

    // Set the probability of the search 
    public void SetProb(float prob)
    {
        m_Probability = prob;
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

    public void Expand(float speed, float timeDelta)
    {
        float slowSpeed = 0.1f / Mathf.Pow(10, Time.timeScale);

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
            position1 += positionDir1 * (expansionStep1);


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
            position2 += positionDir2 * (expansionStep2);
    }


    // Once the search reach an end node then propagate new search segments in the adjacent lines
    // param: source is the source of the movement, destination is the point the movement reached and to be propagated from 
    public void PropagateDestination(int index)
    {
        WayPoint wayPoint = index == 1 ? m_destination1 : m_destination2;

        // Don't propagate if the segment is zero
        if (GetProbability() == MinProbability)
            return;

        // Give a portion of the probability
        float newProb = GetProbability() * Properties.DiscountFactor; // * timeDelta;

        // Create search segments in the other points connected to this destination
        foreach (var line in wayPoint.GetLines())
        {
            // Make sure the new point is not propagated befpre
            if (line.GetSearchSegment() == this || GetProbability() < 0.2f || line.GetSearchSegment().isPropagated)
                continue;

            if (line.GetSearchSegment().GetProbability() > GetProbability())
                break;

            // Create the new search segment 
            line.ProbabgateToSegment(wayPoint.GetPosition(), newProb, StealthArea.episodeTime);
        }
    }


    // Draw the search segment
    public void Draw()
    {
        Gizmos.color = Properties.GetSegmentColor(GetProbability());
        Gizmos.DrawLine(position1, position2);


        // Handles.Label(position1, Vector2.Distance(position1,m_segmentMidPoint).ToString());
        if (GetProbability() > 0f)
            Handles.Label(m_segmentMidPoint, (Mathf.Round(GetProbability() * 100f) / 100f).ToString());
    }
}