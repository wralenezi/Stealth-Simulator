using System.Collections;
using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;
using UnityEngine;

public class DialogLine
{
    // Dialog line identifier
    public string DialogId;

    // Speaker ID
    public NPC speaker;

    // Status of the line
    public DialogStatus Status;

    // Listener ID; if -1 then its for a group
    public NPC listener;

    // how urgent is it
    public int Priority;

    // the spoken line
    public string Line;

    public DialogLine(string _dialogId, NPC _speaker, NPC _listener = null)
    {
        DialogId = _dialogId;
        speaker = _speaker;
        listener = _listener;
        SetLine();
        SetPriority();
        Status = DialogStatus.Queued;
    }

    private void SetLine()
    {
        Line = LineLookUp.GetLineForDialog(DialogId);
        EvaluateLine();
    }

    // Replace the variable name with the values in the line
    private void EvaluateLine()
    {
        // Evaluate the variables
        if (Line.Contains("{speaker_goal_region}"))
        {
            string value = WorldState.Get(speaker.name + "_goal_region");
            value = Equals(value, WorldState.EMPTY_VALUE) ? "around" : value;
            Line = Line.Replace("{speaker_goal_region}", value);
        }

        if (Line.Contains("{speaker_middle_region}"))
        {
            string value = WorldState.Get(speaker.name + "_middle_region");
            Line = Line.Replace("{speaker_middle_region}", value);
        }

        if (Line.Contains("{speaker_start_region}"))
        {
            string value = WorldState.Get(speaker.name + "_start_region");
            Line = Line.Replace("{speaker_start_region}", value);
        }

        if (Line.Contains("{intruder_last_seen_region}"))
        {
            string value = WorldState.Get("intruder_last_seen_region");
            Line = Line.Replace("{intruder_last_seen_region}", value);
        }
    }

    private void SetPriority()
    {
        Priority = LineLookUp.GetPriority(DialogId);
    }

    public string GetAudioPath()
    {
        string path = "Sounds/Voices/guard_1/" + Line.Replace(" ", "_");
        return path;
    }

    public string GetFollowUpLines()
    {
        return "";
    }
}

public enum DialogStatus
{
    Queued,

    Said,

    // The line was responded by another line
    Responded,
}