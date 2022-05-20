using System.Collections.Generic;

public static class PatrolSessions
{
    public static List<Session> GetSessions()
    {
        List<Session> sessions = new List<Session>();

        MapData mapData = new MapData("amongUs", 0.5f);
        AddDynamicSession(ref sessions, mapData);

        mapData = new MapData("Boxes", 1f);
        AddDynamicSession(ref sessions, mapData);

        // mapData = new MapData("MgsDock", 2f);
        // AddDynamicSession(ref sessions, mapData);

        // mapData = new MapData("Alien_isolation_mod", 0.75f);
        // AddDynamicSession(ref sessions, mapData);

        // mapData = new MapData("CoD_relative", 0.15f);
        // AddDynamicSession(ref sessions, mapData);
        //
        // mapData = new MapData("valorant_ascent", 1.5f);
        // AddDynamicSession(ref sessions, mapData);
        //
        // // Add Scripted scenarios
        // NpcLocation? intruderLocation = new NpcLocation(new Vector2(-13.25f, 4.4f), 0f);
        // List<NpcLocation> guardLocations = new List<NpcLocation>
        // {
        //     new NpcLocation(new Vector2(0.48f, 4.8f), 0f),
        //     new NpcLocation(new Vector2(-5.4f, -3.63f), 0f)
        // };
        //
        // mapData = new MapData("MgsDock", 2f);
        // AddScriptedSession(ref sessions, mapData, intruderLocation, guardLocations);


        return sessions;
    }

    private static void AddDynamicSession(ref List<Session> sessions, MapData mapData)
    {
        List<PatrolPlanner> guardMethods = new List<PatrolPlanner>()
        {
            // PatrolPlanner.gRoadMap,
            PatrolPlanner.gRandom
        };

        List<GuardSpawnType> guardSpawnTypes = new List<GuardSpawnType>()
        {
            // GuardSpawnType.Random,
            GuardSpawnType.Separate
        };

        List<int> guardTeams = new List<int>()
        {
            6, 5, 4, 3
        };

        foreach (var guardMethod in guardMethods)
        foreach (var guardTeam in guardTeams)
        foreach (var guardSpawnType in guardSpawnTypes)
        {
            IntruderBehavior intruderBehavior = new IntruderBehavior
            {
                pathCancel = PathCanceller.None,
                thresholdType = RiskThresholdType.None,
                trajectoryType = TrajectoryType.None
            };

            Session session = new Session("", GameType.CoinCollection, Scenario.Stealth, "blue", guardSpawnType,
                guardTeam, 0,
                intruderBehavior,
                mapData, SpeechType.Simple, SurveyType.EndEpisode);

            // Add guards
            for (int i = 0; i < session.guardsCount; i++)
            {
                Behavior behavior = new Behavior(guardMethod, AlertPlanner.Simple,
                    SearchPlanner.Cheating, PlanOutput.DijkstraPath);

                session.AddNpc(i + 1, NpcType.Guard, behavior, PathFindingHeursitic.EuclideanDst,
                    PathFollowing.SimpleFunnel, null);
            }


            sessions.Add(session);
        }
    }

    private static void AddScriptedSession(ref List<Session> sessions, MapData mapData,
        List<NpcLocation> guardLocations)
    {
        {
            IntruderBehavior intruderBehavior = new IntruderBehavior
            {
                pathCancel = PathCanceller.None,
                thresholdType = RiskThresholdType.None,
                trajectoryType = TrajectoryType.None
            };

            Session session = new Session("", GameType.CoinCollection, Scenario.Stealth, "blue",
                GuardSpawnType.Scripted, guardLocations.Count, 0,
                intruderBehavior,
                mapData, SpeechType.Simple, SurveyType.EndEpisode);

            // Add guards
            for (int i = 0; i < session.guardsCount; i++)
            {
                Behavior behavior = new Behavior(PatrolPlanner.gScripted, AlertPlanner.Simple,
                    SearchPlanner.Cheating, PlanOutput.DijkstraPath);

                session.AddNpc(i + 1, NpcType.Guard, behavior, PathFindingHeursitic.EuclideanDst,
                    PathFollowing.SimpleFunnel, guardLocations[i]);
            }

            sessions.Add(session);
        }
    }
}