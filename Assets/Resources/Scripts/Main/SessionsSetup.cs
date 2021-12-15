using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class SessionsSetup : MonoBehaviour
{
    // A Dictionary for each color and what behavior they represent
    private static Dictionary<string, string> _colorLookUp;

    // Available color list
    private static List<string> colorList;

    // List of the colors for the 
    public static List<string> behaviorColors;

    // List of the colors for the speech colors
    public static List<string> speechColors;

    // Available game types
    private static string[] gameTypes = {"CoinCollection"}; //, "Stealth"};

    // Available maps
    private static string[] m_Maps = {"Boxes"}; //{"valorant_ascent", "Boxes", "AlienIsolation"};

    private static int[] m_MapScales = {1};
    private static int[] m_GuardCounts = {4};

    private static int chosenIndex;

    public static List<Dictionary<string, string>> PrepareStudySessions()
    {
        _colorLookUp = new Dictionary<string, string>();

        behaviorColors = new List<string>();
        speechColors = new List<string>();

        colorList = new List<string>() {"blue", "yellow", "grey", "green", "cyan"};

        // Randomly choose 
        int gameIndex = Random.Range(0, gameTypes.Length);
        string gameType = gameTypes[gameIndex];

        // Randomly choose a map and its setting
        chosenIndex = Random.Range(0, m_Maps.Length);
        string map = m_Maps[chosenIndex];
        int mapScale = m_MapScales[chosenIndex];
        int guardCount = m_GuardCounts[chosenIndex];

        // Methods that will be considered 
        List<string> originalMethods = new List<string>() {"RmPropSimple", "Cheating", "RmPropOccupancyDiffusal"};

        // Randomize the methods
        List<string> methods = new List<string>();

        while (originalMethods.Count > 0)
        {
            int randomIndex = Random.Range(0, originalMethods.Count);
            methods.Add(originalMethods[randomIndex]);
            originalMethods.RemoveAt(randomIndex);
        }

        List<Dictionary<string, string>> sessions = new List<Dictionary<string, string>>();
        Dictionary<string, string> session;

        int code = 0;

        // Add the tutorial session

        // Dictionary for the session 
        session = new Dictionary<string, string>
        {
            {"GameCode", "tutorial"},
            {"GameType", gameType},
            {"Scenario", "Chase"},
            {"CoverageResetThreshold", "100"},
            {"WorldRep", "Continuous"},
            {"Map", "MgsDock"},
            {"MapScale", "2"},
            {"GuardColor", "magenta"},
            {"GuardPatrolPlanner", "Stalest"},
            {"GuardChasePlanner", "Simple"},
            {"GuardSearchPlanner", "Random"},
            {"GuardsCount", "2"},
            {"dialogEnabled", "false"},
            {"PathFindingHeursitic", "EuclideanDst"},
            {"PathFollowing", "SimpleFunnel"},
            {"IntudersCount", "1"},
            {"IntruderPlanner", "UserInput"},
            {"SurveyType", "EndTutorial"}
        };

        // Add the session twice
        sessions.Add(session);


        // Add the behavior sessions

        foreach (var guardMethod in methods)
        {
            string color = GetUniqueColor();

            _colorLookUp[color] = guardMethod;

            behaviorColors.Add(color);

            // Dictionary for the session 
            session = new Dictionary<string, string>
            {
                {"GameCode", "methodComparison"},
                {"GameType", gameType},
                {"Scenario", "Chase"},
                {"CoverageResetThreshold", "100"},
                {"WorldRep", "Continuous"},
                {"Map", map},
                {"MapScale", mapScale.ToString()},
                {"GuardColor", color},
                {"GuardPatrolPlanner", "Stalest"},
                {"GuardChasePlanner", "Simple"},
                {"GuardSearchPlanner", guardMethod},
                {"GuardsCount", guardCount.ToString()},
                {"dialogEnabled", "false"},
                {"PathFindingHeursitic", "EuclideanDst"},
                {"PathFollowing", "SimpleFunnel"},
                {"IntudersCount", "1"},
                {"IntruderPlanner", "UserInput"},
                {"SurveyType", "EndEpisode"}
            };

            // Add the session twice
            sessions.Add(session);
        }

        // Modify the last session to evaluate the guards behavior
        sessions[sessions.Count - 1]["SurveyType"] = "BehaviorEval";

        // Do the experiments for the speech impact


        for (int i = 0; i < 2; i++)
        {
            bool dialogEnabled = (i > 0);

            string color = GetUniqueColor();

            if (dialogEnabled)
                _colorLookUp[color] = "with speech";
            else
                _colorLookUp[color] = "no speech";


            speechColors.Add(color);

            // Dictionary for the session 
            session = new Dictionary<string, string>
            {
                {"GameCode", "speechComparison"},
                {"GameType", gameType},
                {"Scenario", "Chase"},
                {"CoverageResetThreshold", "100"},
                {"WorldRep", "Continuous"},
                {"Map", map},
                {"MapScale", mapScale.ToString()},
                {"GuardColor", color},
                {"GuardPatrolPlanner", "Stalest"},
                {"GuardChasePlanner", "Simple"},
                {"GuardSearchPlanner", "RmPropSimple"},
                {"GuardsCount", guardCount.ToString()},
                {"dialogEnabled", dialogEnabled.ToString()},
                {"PathFindingHeursitic", "EuclideanDst"},
                {"PathFollowing", "SimpleFunnel"},
                {"IntudersCount", "1"},
                {"IntruderPlanner", "UserInput"},
                {"SurveyType", "EndEpisode"}
            };

            // Add the session twice
            sessions.Add(session);
        }

        // Modify the last session to evaluate the guards speech implementation
        sessions[sessions.Count - 1]["SurveyType"] = "SpeechEval";


        return sessions;
    }


    public static List<Dictionary<string, string>> StealthyPathingStudy()
    {
        List<Dictionary<string, string>> sessions = new List<Dictionary<string, string>>();


        // Dictionary for the session 
        var session = new Dictionary<string, string>
        {
            {"GameCode", ""},
            {"GameType", "Stealth"},
            {"Scenario", "Stealth"},
            {"CoverageResetThreshold", "100"},
            {"WorldRep", "Continuous"},
            {"Map", "Boxes"},
            {"MapScale", "1"},
            {"GuardColor", "magenta"},
            {"GuardPatrolPlanner", "Stalest"},
            {"GuardChasePlanner", "Simple"},
            {"GuardSearchPlanner", "RmPropOccupancyDiffusal"}, //"RmPropSimple"},
            {"GuardsCount", "4"},
            {"dialogEnabled", "true"},
            {"PathFindingHeursitic", "EuclideanDst"},
            {"PathFollowing", "SimpleFunnel"},
            {"IntudersCount", "0"},
            {"IntruderPlanner", "UserInput"},
            {"SurveyType", "EndEpisode"}
        };

        for (int i = 0; i < 20; i++)
            sessions.Add(session);

        return sessions;
    }


    public static List<Dictionary<string, string>> PrepareTempSessions()
    {
        List<Dictionary<string, string>> sessions = new List<Dictionary<string, string>>();
        Dictionary<string, string> session;


        // Add the tutorial session

        // Dictionary for the session 
        session = new Dictionary<string, string>
        {
            {"GameCode", ""},
            {"GameType", "Stealth"},
            {"Scenario", "Chase"},
            {"CoverageResetThreshold", "100"},
            {"WorldRep", "Continuous"},
            {"Map", "Boxes"},  //"Alien_isolation_mod"}, //"Boxes"}, // 
            {"MapScale", "1"},
            {"GuardColor", "magenta"},
            {"GuardPatrolPlanner", "Stalest"},
            {"GuardChasePlanner", "Simple"},
            {"GuardSearchPlanner", "RmPropSimple"}, //"RmPropSimple"}, //"RmPropOccupancyDiffusal"}, 
            {"GuardsCount", "3"},
            {"dialogEnabled", "true"},
            {"PathFindingHeursitic", "EuclideanDst"},
            {"PathFollowing", "SimpleFunnel"},
            {"IntudersCount", "1"},
            {"IntruderPlanner", "UserInput"},
            {"SurveyType", "EndEpisode"}
        };

        for (int i = 0; i < 20; i++)
            // Add the session twice
            sessions.Add(session);

        return sessions;
    }


    /// <summary>
    /// Get a string for a unique color from the list of available colors
    /// </summary>
    /// <returns>A string for a unique color</returns>
    private static string GetUniqueColor()
    {
        if (colorList.Count == 0) return "White";

        int index = Random.Range(0, colorList.Count);

        string color = colorList[index];

        colorList.RemoveAt(index);

        return color;
    }

    public static string GetCategoryFromColor(string color)
    {
        try
        {
            return _colorLookUp[color];
        }
        catch (Exception e)
        {
            Debug.Log(color);
            Console.WriteLine(e);
            throw;
        }
    }
}