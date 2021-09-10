using System.Collections;
using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using TMPro;
using UnityEngine;

public class DialogGroup
{
    // The set of dialog lines to choose from
    private Queue<string> m_DialogIds;

    public DialogGroup()
    {
        m_DialogIds = new Queue<string>();
    }

    public void AddLineId(string dialogId)
    {
        m_DialogIds.Enqueue(dialogId);
    }

    public string ChooseDialog(ref NPC speaker, bool isVerbose)
    {
        // current dialog to be assessed
        string dialog = "";

        // flag if the dialog is found
        bool isFound = false;

        int index = 0;
        // loop through the list of filler dialog and take the first the applies
        while (index < m_DialogIds.Count)
        {
            dialog = m_DialogIds.Dequeue();

            if (Equals(speaker, null))
            {
                int tries = 5;
                while (tries > 0)
                {
                    // Get a random guard
                    List<Guard> guards = GameManager.instance.GetActiveArea().guardsManager.GetGuards();
                    Guard guard = guards[Random.Range(0,guards.Count)];

                    if (RulesPass(guard, dialog, isVerbose))
                    {
                        speaker = guard;
                        isFound = true;
                        break;
                    }

                    tries--;
                }
                // // loop through guards to check if they can speak a line
                // foreach (var guard in GameManager.instance.GetActiveArea().guardsManager.GetGuards()
                //     .Where(guard => RulesPass(guard, dialog, isVerbose)))
                // {
                //     speaker = guard;
                //     isFound = true;
                // }
            }
            else
            {
                if (!RulesPass(speaker, dialog, isVerbose)) continue;

                isFound = true;
            }


            m_DialogIds.Enqueue(dialog);
            index++;

            if (isFound) break;
        }

        return dialog;
    }


    // Check if the rules of a dialog are valid
    public static bool RulesPass(NPC speaker, string dialogId, bool isVerbose)
    {
        if (Equals(speaker, null))
            return false;

        // Get the rules of a specific line
        Rules rules = LineLookUp.GetRuleSet(dialogId);

        string conditions = "";

        // Loop through the rules
        foreach (var rule in rules.GetRules())
        {
            // If a rule is empty then it is valid
            if (rule == "") continue;

            if (!ValidateRule(speaker, rule, isVerbose))
            {
                if (isVerbose) Debug.Log(dialogId + " - " + rule + " - Failed");
                return false;
            }
        }

        // All rules are validated
        return true;
    }


    // Check a single rule and validate it with the world state
    private static bool ValidateRule(NPC speaker, string rule, bool isVerbose)
    {
        // If the rule string starts with a dot, then it is a rule that needs calculation
        return rule.Substring(0, 4) == "time"
            ? ValidateTimeRule(rule, isVerbose)
            : ValidatePlainRule(rule, speaker, isVerbose);
    }

    // Validate a rule by a simple check with the world state
    private static bool ValidatePlainRule(string rule, NPC speaker, bool isVerbose)
    {
        // Split the rule
        char ruleSplitter = ' ';
        string header = rule.Split(ruleSplitter)[0];
        string op = rule.Split(ruleSplitter)[1];
        string value = rule.Split(ruleSplitter)[2];

        // swap the keywords that starts with the character `  with the actual value
        // speaker variable
        if (header.Contains("`speaker"))
            header = header.Replace("`speaker", speaker.name);


        // Get the value from the world state
        string wrldStValue = WorldState.Get(header);

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
            if (wrldStValue == WorldState.EMPTY_VALUE)
                Debug.Log(header + " is not found.");
        }

        return isSuccess;
    }


    // Validate time based rule 
    private static bool ValidateTimeRule(string rule, bool isVerbose)
    {
        char ruleSplitter = ' ';
        string header = rule.Split(ruleSplitter)[0];
        string op = rule.Split(ruleSplitter)[1];
        string value = rule.Split(ruleSplitter)[2];

        // Remove the variable identifier
        // header = header.Remove(0, 1);

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
        if (Equals(WorldState.Get(wrldStatHeader + "Start"), WorldState.EMPTY_VALUE))
        {
            if (isVerbose)
                Debug.Log(wrldStatHeader + " : No start time");
            return false;
        }

        string wrldStStartValue = WorldState.Get(wrldStatHeader + "Start");
        wrldStStartValue = Equals(wrldStStartValue, WorldState.EMPTY_VALUE) ? "0" : wrldStStartValue;

        float weldStEndValue = WorldState.Get(wrldStatHeader + "End") == WorldState.EMPTY_VALUE
            ? Time.time
            : float.Parse(WorldState.Get(wrldStatHeader + "End"));

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
            if (value == WorldState.EMPTY_VALUE)
                Debug.Log(header + " is not found.");
        }

        return isSuccess;
    }
}