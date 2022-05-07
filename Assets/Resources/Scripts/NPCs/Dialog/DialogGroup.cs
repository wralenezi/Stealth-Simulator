using System;
using System.Collections;
using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

public class DialogGroup
{
    // The set of dialog lines to choose from; the will be sorted from dialogs with highest number of rules to lowest
    private List<string> m_DialogIds;

    public DialogGroup()
    {
        m_DialogIds = new List<string>();
    }

    public void AddLineId(string dialogId)
    {
        m_DialogIds.InsertIntoSortedList(dialogId,
            delegate(string x, string y) { return LineLookUp.GetRuleCount(x).CompareTo(LineLookUp.GetRuleCount(y)); },
            Order.Dsc);
    }

    public DialogLine ChooseDialogAndSpeaker<T>(List<T> npcs, ref NPC listener, SpeechType speechClass, bool isVerbose) where T : NPC
    {
        NPC speaker = null;
        string dialog = "";
        bool isFound = false;

        // loop through the list of filler dialog and take the first the applies
        for (int index = 0; index < m_DialogIds.Count; index++)
        {
            dialog = m_DialogIds[index];

            int tries = 5;
            while (tries > 0)
            {
                // Get a random npc
                T npc = npcs[Random.Range(0, npcs.Count)];

                if (WorldState.RulesPass( npc, listener, dialog, speechClass, isVerbose))
                {
                    speaker = npc;
                    isFound = true;
                    break;
                }

                tries--;
            }

            if (isFound) break;
        }

        if (!isFound) return null;

        // Create the dialog line
        DialogLine dialogLine = new DialogLine(dialog, speaker, listener);
        return dialogLine;
    }


    public DialogLine ChooseDialog(ref NPC speaker, ref NPC listener, SpeechType speechClass, bool isVerbose)
    {
        // current dialog to be assessed
        string dialog = "";

        bool isFound = false;
        // loop through the list of filler dialog and take the first the applies
        for (int index = 0; index < m_DialogIds.Count; index++)
        {
            dialog = m_DialogIds[index];
            //
            // if (Equals(speaker, null))
            // {
            //     int tries = 5;
            //     while (tries > 0)
            //     {
            //         // Get a random guard
            //         List<Guard> guards = GameManager.Instance.GetActiveArea().guardsManager.GetGuards();
            //         Guard guard = guards[Random.Range(0, guards.Count)];
            //
            //         if (WorldState.RulesPass(guard, listener, dialog, speechClass, isVerbose))
            //         {
            //             speaker = guard;
            //             isFound = true;
            //             break;
            //         }
            //
            //         tries--;
            //     }
            // }
            // else
            if (WorldState.RulesPass(speaker, listener, dialog, speechClass, isVerbose))
            {
                isFound = true;
                break;
            }


            if (isFound) break;
        }

        if (!isFound) return null;

        // Create the dialog line
        DialogLine dialogLine = new DialogLine(dialog, speaker, listener);
        return dialogLine;
    }


    public void PrintDialogList()
    {
        string output = "";
        foreach (var dialog in m_DialogIds)
        {
            output += dialog + " - ";
        }

        if (output.Length > 0) output.Substring(0, output.Length - 2);

        Debug.Log(output);
    }
}