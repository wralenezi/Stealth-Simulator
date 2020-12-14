
using System;
using UnityEngine;

public static class Properties 
{
    // General Settings
    public static string logFilesPath = "c:/LogFiles/";
    
    
    // Winding order for outer polygons; inner polygon is opposite.
    public static WindingOrder outerPolygonWinding = WindingOrder.CounterClockwise;
    public static WindingOrder innerPolygonWinding = WindingOrder.Clockwise;
    
    // Polygon Smoothing parameters
    public static float MinAngle = 10f;
    public static float MaxAngle = 170f;
    
    // The walkable area offset from the actual map
    public static float InterPolygonOffset = 0.15f;
    
    // The number of episodes to record
    public static int EpisodesCount = 102;
    
    // Staleness Properties
    // Staleness range
    // Colors for view point state
    public const byte StalenessHigh = 255;
    public const byte StalenessLow = 0;

    public static Color32 GetStalenessColor(float staleness)
    {
        float cappedStaleness = Mathf.Min(staleness, StalenessHigh);
        byte colorLevel = (byte) (StalenessHigh - cappedStaleness);
        return new Color32(colorLevel, colorLevel, colorLevel, 255);
    }
    
    // Staleness rate per second
    public const float StalenessRate = 16f;
    
    // Time required to cover one distance unit in seconds
    public static float TimeRequiredToCoverOneUnit = 3f;
    
    // Hiding Spots
    // Number of static hiding spots
    public static readonly int HidingSpotsCount = 50;
    
    // NPC Properties
    
    // Field of View Properties
    public const float ViewRadius = 15f;
    public const float ViewAngle = 90f;


    // Grid parameters representation
    static readonly int GridMultiplier = 1;
    public static readonly int GridDefaultSizeX = 16 * GridMultiplier;
    public static readonly int GridDefaultSizeY = 10 * GridMultiplier;
    public static readonly float NodeRadius = 0.1f;
    
    
    // Search Parameters
    public static Color32 GetSegmentColor(float age)
    {
        //byte colorLevel = (byte) Math.Round((age/MaxAge) * 255);
        byte colorLevel = (byte) Math.Round(age * 255);
        // return new Color32(255, (byte) (255 - colorLevel), (byte) (255 - colorLevel), 255);
        return new Color32(255, 0, 0, colorLevel);
    }
    
    // The agent difference threshold
    public static readonly int AgeThreshold = 10;
    
    // Max age a search segment can have
    public static readonly int MaxAge = 60;
    // Max length an edge can have
    public static readonly int MaxEdgeLength = 3;
    
    // Rate of increase of the probability value of search segment
    public static readonly float ProbabilityIncreaseRate = 0.01f;
}
