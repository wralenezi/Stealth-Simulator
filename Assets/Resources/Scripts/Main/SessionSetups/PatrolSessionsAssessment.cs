using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PatrolSessionsAssessment
{
    private static int _episodeLength = 120;
    private static int _episodeCount = 10;


    public static List<Session> GetSessions()
    {
        List<Session> sessions = new List<Session>();

        List<int> guardTeams = new List<int>();
        guardTeams.Add(2);

        List<MapData> maps = new List<MapData>();
        // maps.Add(new MapData("amongUs"));
        maps.Add(new MapData("MgsDock"));
        // maps.Add(new MapData("dragonAgeBrc202d"));
        // maps.Add(new MapData("Boxes"));
        // maps.Add(new MapData("bloodstainedAngle"));
        

        AddVisMeshSession("", ref sessions, maps, "black", guardTeams, _episodeLength);
        AddRoadMapSession("", ref sessions, maps, "black", guardTeams, _episodeLength);
        AddGridSession("", ref sessions, maps, "black", guardTeams, _episodeLength);
        return sessions;
    }

    private static void AddVisMeshSession(string gameCode, ref List<Session> sessions, List<MapData> maps,
        string teamColor, List<int> guardTeams, float episodeLength)
    {
        // Guard Patrol Behavior
        List<GuardSpawnType> guardSpawnTypes = new List<GuardSpawnType>()
        {
            // GuardSpawnType.Random,
            GuardSpawnType.Separate,
            // GuardSpawnType.Goal
        };

        List<float> maxSeenRegionPortions = new List<float>()
        {
            0.5f,
            // 0.7f,
            0.9f
        };

        float max = 10f;
        float min = 0f;
        float increment = 5f;

        List<float> areaWeights = new List<float>();
        for (float i = min; i <= max; i += increment)
        {
            areaWeights.Add(i / max);
        }

        List<float> stalenessWeights = new List<float>();
        for (float i = min; i <= max; i += increment)
        {
            stalenessWeights.Add(i / max);
        }

        List<float> distanceWeights = new List<float>();
        for (float i = min; i <= max; i += increment)
        {
            distanceWeights.Add(i / max);
        }

        List<float> separationWeights = new List<float>();
        for (float i = min; i <= max; i += increment)
        {
            separationWeights.Add(i / max);
        }

        List<VMDecision> decisionTypes = new List<VMDecision>()
        {
            VMDecision.Weighted
        };

        foreach (var guardSpawnType in guardSpawnTypes)
        foreach (var areaWeight in areaWeights)
        foreach (var stalenessWeight in stalenessWeights)
        foreach (var distanceWeight in distanceWeights)
        foreach (var separationWeight in separationWeights)
        foreach (var decisionType in decisionTypes)
        foreach (var maxSeenRegionPortion in maxSeenRegionPortions)
        foreach (var guardTeam in guardTeams)
        foreach (var mapData in maps)
        {
            // Set the Hyper-parameters for the behavior
            PatrolerParams patrolParams = new VisMeshPatrolerParams(maxSeenRegionPortion, areaWeight, stalenessWeight,
                distanceWeight, separationWeight, decisionType);

            GuardBehaviorParams guardBehaviorParams = new GuardBehaviorParams(patrolParams,
                null, null);

            Session session = new Session(episodeLength, "", GameType.CoinCollection, Scenario.Stealth, teamColor,
                guardSpawnType, guardTeam, guardBehaviorParams, 0,
                null,
                mapData, SpeechType.Simple, SurveyType.EndEpisode);

            session.sessionVariable = "VisMesh";
            session.coinCount = 0;

            // Add guards
            for (int i = 0; i < session.guardsCount; i++)
                session.AddNpc(i + 1, NpcType.Guard, null);

            session.MaxEpisodes = _episodeCount;

            sessions.Add(session);
        }
    }

    private static void AddRoadMapSession(string gameCode, ref List<Session> sessions, List<MapData> maps,
        string teamColor, List<int> guardTeams, float episodeLength)
    {
        List<GuardSpawnType> guardSpawnTypes = new List<GuardSpawnType>()
        {
            // GuardSpawnType.Random,
            GuardSpawnType.Separate,
            // GuardSpawnType.Goal
        };

        float max = 10f;
        float min = 0f;
        float increment = 5f;

        List<float> maxNormalizedPathLengths = new List<float>()
        {
            0.25f,
            1f
        };

        List<float> guardPassingWeights = new List<float>();
        for (float i = min; i <= max; i += increment)
        {
            guardPassingWeights.Add(i / max);
        }

        List<float> stalenessWeights = new List<float>();
        for (float i = min; i <= max; i += increment)
        {
            stalenessWeights.Add(i / max);
        }

        List<float> connectivityWeights = new List<float>();
        for (float i = min; i <= max; i += increment)
        {
            connectivityWeights.Add(i / max);
        }

        List<float> separationWeights = new List<float>();
        for (float i = min; i <= max; i += increment)
        {
            separationWeights.Add(i / max);
        }


        List<RMPassingGuardsSenstivity> passingGuardsSenstivities = new List<RMPassingGuardsSenstivity>()
        {
            RMPassingGuardsSenstivity.Actual,
            RMPassingGuardsSenstivity.Max
        };

        foreach (var guardSpawnType in guardSpawnTypes)
        foreach (var maxNormalizedPathLength in maxNormalizedPathLengths)
        foreach (var guardPassingWeight in guardPassingWeights)
        foreach (var stalenessWeight in stalenessWeights)
        foreach (var connectivityWeight in connectivityWeights)
        foreach (var passingGuardsSenstivity in passingGuardsSenstivities)
        foreach (var guardTeam in guardTeams)
        foreach (var mapData in maps)
        {
            PatrolerParams patrolParams = new RoadMapPatrolerParams(maxNormalizedPathLength, stalenessWeight,
                guardPassingWeight, connectivityWeight, RMDecision.DijkstraPath, passingGuardsSenstivity, 0f, 0f, 0f);

            GuardBehaviorParams guardBehaviorParams = new GuardBehaviorParams(patrolParams, null, null);

            Session session = new Session(episodeLength, "", GameType.CoinCollection, Scenario.Stealth, teamColor,
                guardSpawnType, guardTeam, guardBehaviorParams, 0,
                null,
                mapData, SpeechType.Simple, SurveyType.EndEpisode);

            session.sessionVariable = "RoadMap";
            session.coinCount = 0;

            // Add guards
            for (int i = 0; i < session.guardsCount; i++)
            {
                session.AddNpc(i + 1, NpcType.Guard, null);
            }


            session.MaxEpisodes = _episodeCount;

            sessions.Add(session);
        }

        List<float> ageWeights = new List<float>();
        for (float i = min; i <= max; i += increment)
        {
            ageWeights.Add(i / max);
        }

        List<float> dtsToGuardWeights = new List<float>();
        for (float i = min; i <= max; i += increment)
        {
            dtsToGuardWeights.Add(i / max);
        }

        List<float> dtsFromOwnWeights = new List<float>();
        for (float i = min; i <= max; i += increment)
        {
            dtsFromOwnWeights.Add(i / max);
        }

        foreach (var dtsFromOwnWeight in dtsFromOwnWeights)
        foreach (var dtsToGuardWeight in dtsToGuardWeights)
        foreach (var ageWeight in ageWeights)
        foreach (var stalenessWeight in stalenessWeights)
        foreach (var guardSpawnType in guardSpawnTypes)
        foreach (var guardTeam in guardTeams)
        foreach (var mapData in maps)
        {
            PatrolerParams patrolParams = new RoadMapPatrolerParams(0f, stalenessWeight,
                0f, 0f, RMDecision.EndPoint, 0f, ageWeight, dtsToGuardWeight, dtsFromOwnWeight);

            GuardBehaviorParams guardBehaviorParams = new GuardBehaviorParams(patrolParams, null, null);

            Session session = new Session(episodeLength, "", GameType.CoinCollection, Scenario.Stealth, teamColor,
                guardSpawnType, guardTeam, guardBehaviorParams, 0,
                null,
                mapData, SpeechType.Simple, SurveyType.EndEpisode);

            session.sessionVariable = "RoadMap";
            session.coinCount = 0;

            // Add guards
            for (int i = 0; i < session.guardsCount; i++)
            {
                session.AddNpc(i + 1, NpcType.Guard, null);
            }


            session.MaxEpisodes = _episodeCount;

            sessions.Add(session);
        }
    }

    private static void AddGridSession(string gameCode, ref List<Session> sessions, List<MapData> maps,
        string guardColor,
        List<int> guardTeams, float episodeLength)
    {
        List<GuardSpawnType> guardSpawnTypes = new List<GuardSpawnType>()
        {
            // GuardSpawnType.Random,
            GuardSpawnType.Separate,
            // GuardSpawnType.Goal
        };

        float max = 10f;
        float min = 0f;
        float increment = 5f;

        List<float> stalenessWeights = new List<float>();
        for (float i = min; i <= max; i += increment)
        {
            stalenessWeights.Add(i / max);
        }

        List<float> distanceWeights = new List<float>();
        for (float i = min; i <= max; i += increment)
        {
            distanceWeights.Add(i / max);
        }

        List<float> separationWeights = new List<float>();
        for (float i = min; i <= max; i += increment)
        {
            separationWeights.Add(i / max);
        }

        List<float> cellSizes = new List<float>()
        {
            0.75f,
            1f
        };

        foreach (var guardSpawnType in guardSpawnTypes)
        foreach (var guardTeam in guardTeams)
        foreach (var cellSize in cellSizes)
        foreach (var stalenessWeight in stalenessWeights)
        foreach (var distanceWeight in distanceWeights)
        foreach (var separationWeight in separationWeights)
        foreach (var mapData in maps)
        {
            // Set the Hyper-parameters for the behavior
            PatrolerParams patrolParams =
                new GridPatrolerParams(cellSize, stalenessWeight, distanceWeight, separationWeight);

            GuardBehaviorParams guardBehaviorParams = new GuardBehaviorParams(patrolParams,
                null, null);

            Session session = new Session(episodeLength, gameCode, GameType.CoinCollection, Scenario.Stealth,
                guardColor,
                guardSpawnType, guardTeam, guardBehaviorParams, 0,
                null,
                mapData, SpeechType.Simple, SurveyType.EndEpisode);

            session.sessionVariable = "Grid";
            session.coinCount = 0;

            // Add guards
            for (int i = 0; i < session.guardsCount; i++)
            {
                session.AddNpc(i + 1, NpcType.Guard, null);
            }

            session.MaxEpisodes = _episodeCount;

            sessions.Add(session);
        }
    }
}