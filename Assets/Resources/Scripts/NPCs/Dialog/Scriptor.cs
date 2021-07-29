using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;
using Random = UnityEngine.Random;

public class Scriptor : MonoBehaviour
{
    // Verbose flag
    [SerializeField] private bool IsVerbose;

    private string m_LastDialogLine;
    private float m_LastTimeDialogPlayed;
    private float m_sameLineCooldown = 5f;

    // Dialog lines queue; the lines scheduled to be played
    private Queue<DialogLine> m_LinesToPlay;

    // The Current Dialog Line being played 
    private DialogLine m_CrntDlgLn;

    // Audio source to play
    private AudioSource m_As;

    // Coroutine for playing dialog lines
    private IEnumerator playingDialogs;

    // Coroutine regularly playing the dialog coroutine
    private IEnumerator regularShoutout;

    // Filler lines to play when there is no special actions 
    private int fillerIndex;
    private CyclicalList<string> m_fillerLineIds;

    // List of NPCs that can speak
    private List<Guard> m_Guards;

    public void Initialize()
    {
        // Load the defined dialogs 
        LineLookUp.Initiate();

        // Load variables
        m_LinesToPlay = new Queue<DialogLine>();
        m_As = GetComponent<AudioSource>();

        // Set the lines
        fillerIndex = 0;
        m_fillerLineIds = new CyclicalList<string>() {"st_search-continue"};

        m_Guards = GetComponent<GuardsManager>().GetGuards();

        StartShoutout();

        IsVerbose = true;
    }

    // Start the routine to repeatedly play filler dialogs
    private void StartShoutout()
    {
        // If the variable is not assign it 
        regularShoutout ??= RegularShouts();

        StartCoroutine(regularShoutout);
    }

    public void StopShoutout()
    {
        if (regularShoutout == null) return;

        StopCoroutine(regularShoutout);
        regularShoutout = null;
    }

    // Routine for playing filler dialogs 
    private IEnumerator RegularShouts()
    {
        while (true)
        {
            int wait = Random.Range(5, 10);
            yield return new WaitForSeconds(wait);
            UpdateWldSt();
            ChooseDialog();
        }
    }

    // Find an adequate filler dialog to add to be played
    private void ChooseDialog()
    {
        // we need to find the line and the speaker who will say it
        string speaker = "";
        string lineId = "";

        // loop through the list of filler dialog and take the first the applies
        bool foundLine = false;
        for (int i = fillerIndex; i < m_fillerLineIds.Count; i++)
        {
            // loop through guards to check if they can speak a line
            foreach (var guard in GameManager.instance.GetActiveArea().guardsManager.GetGuards())
            {
                // if a line has been found, then stop looping
                if (foundLine)
                    break;

                // Check if the preconditions of this line and speaker are valid
                if (RulesPass(guard.name, m_fillerLineIds[fillerIndex]))
                {
                    speaker = guard.name;
                    lineId = m_fillerLineIds[fillerIndex];
                    foundLine = true;
                    break;
                }
            }
        }

        // Restrict the index to be in range
        fillerIndex %= m_fillerLineIds.Count;

        // if a line is found append it 
        if (speaker != "")
        {
            AppendDialogLine(speaker, lineId);
        }
    }

    // Check if the rules of a line are valid
    public bool RulesPass(string speaker, string lineId)
    {
        // Get the rules of a specific line
        Rules rules = LineLookUp.GetRuleSet(lineId);

        if (IsVerbose) Debug.Log(WorldState.GetWorldState());

        string conditions = "";

        // Loop through the rules
        foreach (var rule in rules.GetRules())
        {
            // If a rule is empty then it is valid
            if (rule == "") continue;

            if (!ValidateRule(speaker, rule))
            {
                if (IsVerbose) Debug.Log(lineId + " - " + rule + " - Failed");
                return false;
            }
        }

        // All rules are validated
        return true;
    }


    // Check a single rule and validate it with the world state
    private bool ValidateRule(string speaker, string rule)
    {
        // If the rule string starts with a dot, then it is a rule that needs calculation
        return rule.Substring(0, 4) == "time" ? ValidateTimeRule(rule) : ValidatePlainRule(rule, speaker);
    }

    // Validate a rule by a simple check with the world state
    private bool ValidatePlainRule(string rule, string speaker)
    {
        // Split the rule
        char ruleSplitter = ' ';
        string header = rule.Split(ruleSplitter)[0];
        string op = rule.Split(ruleSplitter)[1];
        string value = rule.Split(ruleSplitter)[2];

        // if the rule header start with underscore then it is a variable 
        if (header[0] == '_')
        {
            // Remove the variable identifier
            header = header.Remove(0, 1);

            // Assign the header actual value
            switch (header)
            {
                case "speaker":
                    header = speaker;
                    break;
            }
        }

        // Get the value from the world state
        string wrldStValue = WorldState.Get(header);

        // Based on the operator check
        bool isSuccess = op switch
        {
            // Equality
            "=" => value == wrldStValue,
            ">=" => float.Parse(wrldStValue) >= float.Parse(value),
            "<=" => float.Parse(wrldStValue) <= float.Parse(value),
            _ => false
        };

        if (IsVerbose)
        {
            if (wrldStValue == "")
                Debug.Log(header + " is not found.");
            else
                Debug.Log(rule + ": " + isSuccess);
        }

        return isSuccess;
    }


    // Validate time based rule 
    private bool ValidateTimeRule(string rule)
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
        if (WorldState.Get(wrldStatHeader + "Start") == "")
        {
            if (IsVerbose)
                Debug.Log(wrldStatHeader + " : No start time");
            return false;
        }

        string wrldStStartValue = WorldState.Get(wrldStatHeader + "Start");

        float weldStEndValue = WorldState.Get(wrldStatHeader + "End") == ""
            ? Time.time
            : float.Parse(WorldState.Get(wrldStatHeader + "End"));

        float timeInterval = weldStEndValue - float.Parse(wrldStStartValue);

        // Based on the operator check
        bool isSuccess = op switch
        {
            ">=" => timeInterval >= float.Parse(value),
            "<=" => timeInterval <= float.Parse(value),
            _ => false
        };

        if (IsVerbose)
        {
            if (value == "")
                Debug.Log(header + " is not found.");
            else
                Debug.Log(rule + ": " + isSuccess);
        }

        return isSuccess;
    }


    // Play the queue dialog lines
    private void PlayScript(bool isForced)
    {
        // If it is force then stop any dialogs and 
        if (isForced && playingDialogs != null)
        {
            StopCoroutine(playingDialogs);
            playingDialogs = null;
            m_CrntDlgLn = null;
            m_As.Pause();
        }

        if (playingDialogs != null) return;

        playingDialogs = PlayDialogs();
        StartCoroutine(playingDialogs);
    }

    // Update the World State Variables
    private void UpdateWldSt()
    {
        // Update the guards variables
        GameManager.instance.GetActiveArea().guardsManager.UpdateWldStNpcs();
    }

    public void AppendDialogLine(string speakerId, string _name)
    {
        if (Time.time - m_LastTimeDialogPlayed < m_sameLineCooldown && _name == m_LastDialogLine)
            return;

        UpdateWldSt();

        // Create the dialog line
        DialogLine dialogLine = new DialogLine(_name, speakerId);

        // Clear the Queue and stop less important dialogs
        if (IsHigherPriority(dialogLine))
        {
            // Insert new dialog
            m_LinesToPlay.Enqueue(dialogLine);

            // Check if there are follow up lines and add them
            BranchDialog(dialogLine);

            // play the script 
            PlayScript(true);
        }
    }


    // Branch a dialog
    private void BranchDialog(DialogLine dialog)
    {
        int count = 5;
        while (count-- > 0)
        {
            // If there are no possible response then end
            if (!LineLookUp.IsDlgHasRspns(dialog.DialogId))
            {
                return;
            }

            // Check if this line is relevant
            bool isRspnsValid = false;
            string possibleRspnsId = "";
            string responder = "";

            int attempts = 6;
            while (!isRspnsValid && attempts-- > 0)
            {
                // Get a possible speaker
                responder = GetPossibleListener(dialog);

                // Get another possible response
                possibleRspnsId = LineLookUp.GetResponseForDialog(dialog.DialogId);

                // Check if the response is valid
                isRspnsValid = RulesPass(responder, possibleRspnsId);
            }

            // If a response was found, add it and try to expand it 
            if (possibleRspnsId != "")
            {
                DialogLine newDialog = new DialogLine(possibleRspnsId, responder);
                m_LinesToPlay.Enqueue(newDialog);
                dialog = newDialog;
                continue;
            }

            break;
        }
    }

    private string GetPossibleListener(DialogLine dialog)
    {
        string responder = "";
        // Choose the next speaker if this dialog line is general
        if (string.IsNullOrEmpty(dialog.ListenerId))
        {
            foreach (var guard in m_Guards.Where(guard => guard.name != dialog.SpeakerId))
            {
                responder = guard.name;
                break;
            }
        }
        else
            responder = dialog.ListenerId;


        return responder;
    }

    // Play a dialog line and return its length in seconds
    private float Play(DialogLine dialogLine)
    {
        AudioClip audioClip = Resources.Load<AudioClip>(dialogLine.GetAudioPath());
        m_As.clip = audioClip;
        m_As.Play();

        return audioClip.length;
    }

    private IEnumerator PlayDialogs()
    {
        while (m_LinesToPlay.Count > 0f)
        {
            m_CrntDlgLn = m_LinesToPlay.Dequeue();
            float waitTime = Random.Range(0.3f, 1f);

            if (m_CrntDlgLn.Status == DialogStatus.Queued)
            {
                float wait = Play(m_CrntDlgLn) + waitTime;

                if (IsVerbose)
                    Debug.Log(m_CrntDlgLn.SpeakerId + " : " + m_CrntDlgLn.Line);

                m_CrntDlgLn.Status = DialogStatus.Said;
                m_LastDialogLine = m_CrntDlgLn.DialogId;
                m_LastTimeDialogPlayed = Time.time;

                yield return new WaitForSeconds(wait);
            }
        }
        
        m_CrntDlgLn = null;
        playingDialogs = null;
    }

    // Stop the current audio and clear the queue if the new dialog priority is higher
    private bool IsHigherPriority(DialogLine newLine)
    {
        if (m_CrntDlgLn == null) return true;

        if (m_CrntDlgLn != null && newLine.Priority > m_CrntDlgLn.Priority)
        {
            // m_As.Stop();
            m_LinesToPlay.Clear();
            return true;
        }

        return false;
    }
}