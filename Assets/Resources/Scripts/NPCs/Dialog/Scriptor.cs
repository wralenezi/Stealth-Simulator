using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class Scriptor : MonoBehaviour
{
    // Display debug messages
    [SerializeField] private bool isVerbose;

    // Enable The Dialog system or not
    public bool enabled = false;

    private string m_LastDialogLine;
    private float m_LastTimeDialogPlayed;
    private const float SameLineCooldown = 5f;

    /// Chat bubble
    private const string ChatBubbleLoc = "Prefabs/ChatBubble";

    // chat bubble Object
    private ChatBubble m_ChtBble;

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

    // flag to show world state
    public bool showWorldState;

    // The UI text that shows the world state
    private TextMeshProUGUI m_wrldStatLabel;

    // List of NPCs that can speak
    private List<Guard> m_Guards;

    public void Initialize()
    {
        // Skip if this component is not enabled
        if (!enabled) return;

        // Load the defined dialogs 
        LineLookUp.Initiate();

        // Create the world state label
        CreateWrldStateLabel();

        // Load variables
        m_LinesToPlay = new Queue<DialogLine>();
        m_As = GetComponent<AudioSource>();
        m_As.volume = 0.1f;

        // Reference the guards
        m_Guards = GetComponent<GuardsManager>().GetGuards();

        // Initiate the chat bubble
        GameObject chtBblePrefab = Resources.Load(ChatBubbleLoc) as GameObject;
        GameObject chatBubbleObj = Instantiate(chtBblePrefab);
        m_ChtBble = chatBubbleObj.GetComponent<ChatBubble>();

        // 
        StartShoutout();

        isVerbose = true;
    }

    // Associate the dialogs into 
    private void CreateWrldStateLabel()
    {
        GameObject canvas = GameObject.Find("Canvas");

        GameObject label = new GameObject("World State Label");
        label.transform.parent = canvas.transform;

        m_wrldStatLabel = label.AddComponent<TextMeshProUGUI>();
        Vector2 canvasPos = canvas.GetComponent<RectTransform>().position;

        label.GetComponent<RectTransform>().position = new Vector2(-378f, 282f) + canvasPos;

        m_wrldStatLabel.fontSize = 7f;
    }

    private void Update()
    {
        if (enabled) m_wrldStatLabel.gameObject.SetActive(showWorldState);
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
            // time to wait before playing next 
            int wait = Random.Range(1, 3);
            yield return new WaitForSeconds(wait);

            // Update the world state
            UpdateWldSt();

            // Chose the dialog to play
            ChooseDialog(null, "Filler");
        }
    }

    // Find an adequate dialog to add to be played
    public void ChooseDialog(NPC speaker, string dialogType)
    {
        if (!enabled) return;

        DialogGroup dialogs = LineLookUp.GetDialogGroup(dialogType);

        // Update the world state label
        m_wrldStatLabel.text = WorldState.GetWorldState();

        string lineId = dialogs.ChooseDialog(ref speaker, isVerbose);

        // if a line is found append it 
        if (!Equals(speaker, null) && !Equals(lineId, ""))
            AppendDialogLine(speaker, lineId);
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

    private void AppendDialogLine(NPC speaker, string _dialogId)
    {
        if (Time.time - m_LastTimeDialogPlayed < SameLineCooldown && _dialogId == m_LastDialogLine)
            return;

        UpdateWldSt();

        // Create the dialog line
        DialogLine dialogLine = new DialogLine(_dialogId, speaker);

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
                return;


            // Check if this line is relevant
            bool isRspnsValid = false;
            string possibleRspnsId = "";
            NPC responder = null;

            int attempts = 6;
            while (!isRspnsValid && attempts-- > 0)
            {
                // Get a possible speaker
                responder = GetPossibleListener(dialog);

                // Get another possible response
                possibleRspnsId = LineLookUp.GetResponseForDialog(dialog.DialogId);

                // Check if the response is valid
                isRspnsValid = DialogGroup.RulesPass(responder, possibleRspnsId, isVerbose);
            }

            // If a response was found, add it and try to expand it 
            if (isRspnsValid)
            {
                DialogLine newDialog = new DialogLine(possibleRspnsId, responder);
                m_LinesToPlay.Enqueue(newDialog);
                dialog = newDialog;
                continue;
            }

            break;
        }
    }

    private NPC GetPossibleListener(DialogLine dialog)
    {
        NPC responder = null;
        // Choose the next speaker if this dialog line is general
        if (Equals(dialog.listener, null))
        {
            foreach (var guard in m_Guards.Where(guard => guard != dialog.speaker))
            {
                responder = guard;
                break;
            }
        }
        else
            responder = dialog.listener;


        return responder;
    }

    // Play a dialog line and return its length in seconds
    private float Play(DialogLine dialogLine)
    {
        // Determine the length 
        float secPerLetter = 0.3f;
        AudioClip audioClip = Resources.Load<AudioClip>(dialogLine.GetAudioPath());
        m_As.clip = audioClip;
        m_ChtBble.SetText(dialogLine.speaker, dialogLine.Line);
        m_As.Play();

        // In case there is no file for that, just return the time based on the number of letters in the line.
        return !Equals(audioClip, null) ? audioClip.length : secPerLetter * dialogLine.Line.Length;
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

                if (isVerbose)
                    Debug.Log(m_CrntDlgLn.speaker + " : " + m_CrntDlgLn.Line);

                m_CrntDlgLn.Status = DialogStatus.Said;
                m_LastDialogLine = m_CrntDlgLn.DialogId;
                m_LastTimeDialogPlayed = Time.time;

                yield return new WaitForSeconds(wait);
                m_ChtBble.Disable();
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
            m_ChtBble.Disable();
            return true;
        }

        return false;
    }
}