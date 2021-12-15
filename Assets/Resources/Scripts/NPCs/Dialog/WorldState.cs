using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WorldState
{
    // List of headings; some rules might include several instances and they all are recorded in this list to validate them
    private static List<string> _headings;

    public const string EMPTY_VALUE = "NA";

    // The dictionary that saves the variables of the world state
    private static Dictionary<string, string> _worldState;
    
    public static void Initialize()
    {
        _worldState = new Dictionary<string, string>();
        _headings = new List<string>();
    }

    public static void Reset()
    {
        _worldState.Clear();
    }

    public static void Set(string name, string value)
    {
        _worldState[name] = value;
    }

    public static string Get(string name)
    {
        return _worldState.TryGetValue(name, out string value) ? value : EMPTY_VALUE;
    }

    // Get the time spent since a timeStamp
    public static int GetTimeSince(string timeStamp)
    {
        return Mathf.RoundToInt(Time.time - float.Parse(Get(timeStamp)));
    }

    // Get the heading from a position to another in words
    public static string GetDirectionName(Vector2 from, Vector2 to)
    {
        Vector2 dir = (to - from).normalized;

        string heading = "";

        if (Mathf.Abs(dir.y) > 0.5f)
        {
            if (dir.y > 0.5f)
                heading += "north";
            else if (dir.y < -0.5f)
                heading += "south";
        }

        if (Mathf.Abs(dir.x) > 0.5f)
        {
            // Add a separator
            heading += heading != "" ? "-" : "";

            if (dir.x > 0.5f)
                heading += "east";
            else if (dir.x < -0.5f)
                heading += "west";
        }

        if (heading == "")
            heading = "around";

        return heading;
    }

    // get the world state as a string
    public static string GetWorldState()
    {
        string result = "";

        foreach (var pair in _worldState)
        {
            result += pair.Key + " = " + pair.Value + "\n";
        }

        return result;
    }

    // Check if the rules of a dialog are valid
    public static bool RulesPass(NPC speaker, NPC listener, string dialogId, bool isVerbose)
    {
        if (Equals(speaker, null))
            return false;

        // Get the rules of a specific line
        Rules rules = LineLookUp.GetRuleSet(dialogId);

        if (rules.GetRuleCount() == 0) return true;

        // Loop through the rules
        foreach (var rule in rules.GetRules())
        {
            if (!ValidateRule(speaker, listener, rule, isVerbose))
            {
                if (isVerbose) Debug.Log(dialogId + " - " + rule + " - Failed");
                return false;
            }
        }

        // All rules are validated
        return true;
    }


    // Check a single rule and validate it with the world state
    private static bool ValidateRule(NPC speaker, NPC listener, string rule, bool isVerbose)
    {
        if (rule.Length == 0) return true;

        // If the rule string starts with the word "time" then it needs evaluation
        return rule.Substring(0, 4) == "time"
            ? ValidateTimeRule(rule, isVerbose)
            : ValidatePlainRule(rule, speaker, listener, isVerbose);
    }

    // Validate a rule by a simple check with the world state
    private static bool ValidatePlainRule(string rule, NPC speaker, NPC listener, bool isVerbose)
    {
        // Clear the headings
        _headings.Clear();

        try
        {
            // Split the rule
            char ruleSplitter = ' ';
            string header = rule.Split(ruleSplitter)[0];
            string op = rule.Split(ruleSplitter)[1];
            string value = rule.Split(ruleSplitter)[2];

            // Replace the variables of single values
            // swap the keywords that starts with the character `  with the actual value
            // speaker variable
            if (header.Contains("`speaker"))
                header = header.Replace("`speaker", speaker.name);
            // listener variable
            if (header.Contains("`listener"))
                header = header.Replace("`listener", listener.name);

            _headings.Add(header);

            // Replace variables with multiple values
            EvaluateVariables(speaker.name);

            return CheckRules(op, value, isVerbose);
        }
        catch (Exception e)
        {
            Debug.Log(rule);
            Console.WriteLine(e);
            throw;
        }
    }

    private static void EvaluateVariables(string speaker)
    {
        int i = 0;
        while (i < _headings.Count)
        {
            string header = _headings[i];

            // guards mean all the guards except the speaker
            if (header.Contains("*guards"))
            {
                List<Guard> guards = GameManager.Instance.GetActiveArea().guardsManager.GetGuards();

                foreach (var str in from guard in guards
                    where !Equals(guard.name, speaker)
                    select header.Replace("*guards", guard.name))
                {
                    _headings.Add(str);
                }

                _headings.RemoveAt(i);
                if (i > 0) i--;
            }

            i++;
        }
    }


    private static bool CheckRule(string header, string op, string value, bool isVerbose)
    {
        // Get the value from the world state
        string wrldStValue = Get(header);

        if (wrldStValue == "NA") return false;

        // Based on the operator check
        bool isSuccess = op switch
        {
            // Equality
            "=" => value == wrldStValue,
            ">=" => float.Parse(wrldStValue) >= float.Parse(value),
            ">" => float.Parse(wrldStValue) > float.Parse(value),
            "<=" => float.Parse(wrldStValue) <= float.Parse(value),
            "<" => float.Parse(wrldStValue) < float.Parse(value),
            _ => false
        };

        if (isVerbose)
        {
            if (wrldStValue == EMPTY_VALUE)
                Debug.Log(header + " is not found.");

            Debug.Log(header + " " + op + " " + " " + value + " -- " + isSuccess + " ( " + wrldStValue + " )");
        }

        return isSuccess;
    }

    private static bool CheckRules(string op, string value, bool isVerbose)
    {
        while (_headings.Count > 0)
        {
            if (!CheckRule(_headings[0], op, value, isVerbose))
                return false;

            if (_headings.Count > 0) _headings.RemoveAt(0);
        }

        return true;
    }


    // Validate time based rule 
    private static bool ValidateTimeRule(string rule, bool isVerbose)
    {
        char ruleSplitter = ' ';
        string header = rule.Split(ruleSplitter)[0];
        string op = rule.Split(ruleSplitter)[1];
        string value = rule.Split(ruleSplitter)[2];

        // Define the start and end labels or a phase
        string wrldStatHeader = header;

        switch (header)
        {
            // duration of the last patrol phase
            case "time_last_patrol":
                wrldStatHeader = "lastPatrolTime";
                break;

            // duration of the last chase phase
            case "time_last_chase":
                wrldStatHeader = "lastChaseTime";
                break;

            // duration of the last search phase
            case "time_last_search":
                wrldStatHeader = "lastSearchTime";
                break;
        }

        // Get the value from the world state
        if (Equals(Get(wrldStatHeader + "Start"), EMPTY_VALUE))
        {
            if (isVerbose)
                Debug.Log(wrldStatHeader + " : No start time");
            return false;
        }

        string wrldStStartValue = Get(wrldStatHeader + "Start");
        wrldStStartValue = Equals(wrldStStartValue, EMPTY_VALUE) ? "0" : wrldStStartValue;

        float weldStEndValue = Get(wrldStatHeader + "End") == EMPTY_VALUE
            ? Time.time
            : float.Parse(Get(wrldStatHeader + "End"));

        float timeInterval = weldStEndValue - float.Parse(wrldStStartValue);

        // Based on the operator check
        bool isSuccess = op switch
        {
            ">=" => timeInterval >= float.Parse(value),
            ">" => timeInterval > float.Parse(value),
            "<=" => timeInterval <= float.Parse(value),
            "<" => timeInterval < float.Parse(value),
            _ => false
        };

        if (isVerbose)
        {
            if (value == EMPTY_VALUE)
                Debug.Log(header + " is not found.");

            Debug.Log(header + " " + op + " " + " " + value + " -- " + isSuccess + " ( " + timeInterval + " )");
        }

        return isSuccess;
    }
}