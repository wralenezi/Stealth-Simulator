using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class ChatBubble : MonoBehaviour
{
    private Vector2 padding;

    // The Bubble Components
    private SpriteRenderer m_SprtRndrr;
    private TextMeshPro m_TxtMshPr;

    // Reference to the speaker
    public NPC speaker = null;

    private void Awake()
    {
        m_SprtRndrr = transform.Find("BG").GetComponent<SpriteRenderer>();
        m_TxtMshPr = transform.Find("Text").GetComponent<TextMeshPro>();
        padding = new Vector2(1f, 0.5f);
        Disable();
    }

    private void Enable()
    {
        gameObject.SetActive(true);
    }

    public void Disable()
    {
        gameObject.SetActive(false);
        speaker = null;
    }

    public void SetText(NPC _speaker, string msg)
    {
        Enable();
        speaker = _speaker;
        m_TxtMshPr.SetText(msg);
        m_TxtMshPr.ForceMeshUpdate();
        Vector2 textSize = m_TxtMshPr.GetRenderedValues(false);
        m_SprtRndrr.size = textSize + padding;
    }

    private void Update()
    {
        FollowSpeaker();
    }

    // Follow the speaker; if there is one.
    private void FollowSpeaker()
    {
        if (Equals(speaker, null) || Equals(speaker.GetTransform(), null)) return;

        transform.position = (Vector2) speaker.GetTransform().position + m_SprtRndrr.size / 2f;
    }
}