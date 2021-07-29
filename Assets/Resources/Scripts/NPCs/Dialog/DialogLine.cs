using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogLine
{
    // Dialog line identifier
    public string DialogId;

    // Speaker ID
    public string SpeakerId;

    // Status of the line
    public DialogStatus Status;

    // Listener ID; if -1 then its for a group
    public string ListenerId;

    // how urgent is it
    public int Priority;

    // the spoken line
    public string Line;

    public DialogLine(string _dialogId, string _speakerId, string _listenerId = "")
    {
        DialogId = _dialogId;
        SpeakerId = _speakerId;
        ListenerId = _listenerId;
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
        string path = "Sounds/Voices/guard_1/"+Line.Replace(" ","_");
        return path;
    }

    public string GetFollowUpLines()
    {
        return ""; 
    }
}

// Type of the dialog line
public enum DialogType
{
    Question,

    Command,

    Response,

    Statement
}

public enum DialogStatus
{
    Queued,

    Said,

    // The line was responded by another line
    Responded,
}