using System;
using UnityEngine;

public static class Properties
{
    // General game set up
    //----------------------------------------------------------
    // The number of episodes to record
    public const int EpisodesCount = 60;

    // The duration of an episode.
    public const float EpisodeLength = 105f;


    //-------------------------------------------------------------------------//
    // Geometry Parameters
    // Winding order for outer polygons; inner polygon is opposite.
    public static WindingOrder outerPolygonWinding = WindingOrder.CounterClockwise;
    public static WindingOrder innerPolygonWinding = WindingOrder.Clockwise;

    // Polygon Smoothing parameters
    public static float MinAngle = 10f;
    public static float MaxAngle = 170f;

    // The walkable area offset from the actual map
    public static float InterPolygonOffset = 0.3f;

    //------------------------------------------------------------------------------------------------------
    // Grid parameters representation
    static readonly int GridMultiplier = 1;
    public static readonly int GridDefaultSizeX = 16 * GridMultiplier;
    public static readonly int GridDefaultSizeY = 10 * GridMultiplier;
    public static readonly float NodeRadius = 0.15f;


    //---------------------------------------------------//
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

    //------------------------------------------------------------------------------------------------------
    // NPC Properties
    public const float SpeedMultiplyer = 0.6f;
    public const float NpcSpeed = 4f * SpeedMultiplyer;
    public const float NpcRotationSpeed = 300f * SpeedMultiplyer;

    // The multiplier of the intruder's speed
    public const float IntruderSpeedMulti = 1.5f;
    public const float IntruderRotationSpeedMulti = 2f;

    public static Color32 GetFovColor(NpcType npcType)
    {
        switch (npcType)
        {
            case NpcType.Guard:
                return new Color32(0, 100, 100, 100);

            case NpcType.Intruder:
                return new Color32(255, 255, 255, 150);

            default:
                return new Color32(100, 100, 100, 100);
        }
    }

    private static float maxWidth;

    public static void SetMapMaxWidth(float _maxWidth)
    {
        maxWidth = _maxWidth;
    }


    public static float GuardsFovRadiusPercentage = 0.1f;

    // Get the default value for view radius for the Npcs as a portion of a value
    public static float GetFovRadius(NpcType npcType)
    {
        // Field of View Properties
        float viewRadiusFractionOfMap;

        switch (npcType)
        {
            case NpcType.Guard:
                // viewRadiusFractionOfMap = GuardsFovRadiusPercentage;
                viewRadiusFractionOfMap = GetGuardFovForMap(MapManager.Instance.mapData.name);
                break;

            case NpcType.Intruder:
                viewRadiusFractionOfMap = 0f;
                break;

            default:
                viewRadiusFractionOfMap = 0.1f;
                break;
        }

        return maxWidth * viewRadiusFractionOfMap;
    }

    private static float GetGuardFovForMap(string mapName)
    {
        switch (mapName)
        {
            case "Hall":
                return 0.3f;
            
            default:
                return 0.1f;
        }
    }

    // Get the FoV angle based on the type of the npc.
    public static float GetFovAngle(NpcType npcType)
    {
        // Field of View Properties
        float fovAngle;

        switch (npcType)
        {
            case NpcType.Guard:
                fovAngle = 90f;
                break;

            case NpcType.Intruder:
                fovAngle = 0f;
                break;

            default:
                fovAngle = 90f;
                break;
        }

        return fovAngle;
    }


    //-----------------------------------------------------------------------------
    // Search Parameters
    // Rate of increase of the probability value of search segment
    public static readonly float ProbabilityIncreaseRate = 0.001f * NpcSpeed;
    public static readonly float PropagationMultiplier = 0.5f * NpcSpeed;

    // Parameters of Damian Isla implementation
    // Probability Diffuse factor; it tunes how fast the probability is propagated.
    public static readonly float ProbDiffFac = 0.1f;

    // Ratio of max length of a segment to map width. 
    private static readonly float maxEdgeRatio = 0.2f;

    public static float GetMaxEdgeLength()
    {
        // return maxWidth * maxEdgeRatio;
        return 2f;
    }

    // Max age a search segment can have
    public static float MaxAge = 255f;

    // The maximum walkable area; This acts as a denominator for normalizing the map size.
    public static float MaxWalkableArea = 5000f;

    // The Maximum number of guards available.
    public static int MaxGuardCount = 10;

    // Get a color opacity based a on a value from 0 to 1
    public static Color32 GetSegmentColor(float feature)
    {
        if (feature < 0f) feature = 0f;

        // In case of using the likelihood feature
        byte opacity = (byte) Mathf.Round(feature * 255);

        return new Color32(255, 0, 0, opacity);
    }
}