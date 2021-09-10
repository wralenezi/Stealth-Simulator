using System.Collections;
using System.Collections.Generic;
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