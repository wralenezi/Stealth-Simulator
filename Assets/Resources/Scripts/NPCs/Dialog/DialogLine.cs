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
        // `SPEAKER_HEADING is the heading of the speaker. Duh.
        Line = Line.Replace("`SPEAKER_GOAL", WorldState.Get(speaker.name+"_goal"));
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