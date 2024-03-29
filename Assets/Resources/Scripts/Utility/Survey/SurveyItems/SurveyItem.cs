﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using TMPro;

[JsonObject(MemberSerialization.OptIn)]
public abstract class SurveyItem : MonoBehaviour
{
    protected ItemType type;
    
    // The survey this item belongs to
    protected Survey survey;

    protected Transform questionPanel;
    protected TextMeshProUGUI m_question;

    protected Transform inputPanel;

    public bool isAnswered;
    
    // Item info
    // Item name
    [JsonProperty("name")] private string m_name;

    // Item Description
    [JsonProperty("desc")] private string m_desc;

    // Item answer
    [JsonProperty("answer")] protected string m_answer;

    // code to determine if the answer is a color and should be handled
    protected string m_code;

    public virtual void Initiate(string _name, ItemType _type, Survey _survey, string _code)
    {
        survey = _survey;
        gameObject.name = _name;
        m_name = name;
        m_code = _code;
        type = _type;

        questionPanel = transform.Find("QuestionPanel");
        m_question = questionPanel.Find("Text").GetComponent<TextMeshProUGUI>();

        inputPanel = transform.Find("InputPanel");

        RectTransform rect = GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(Screen.width, Screen.height);
    }

    public void DeactivateInput(string choice)
    {
        for (int i = 0; i < inputPanel.childCount; i++)
        {
            Transform child = inputPanel.GetChild(i);

            if (child.gameObject.name.Equals(choice))
            {
                child.gameObject.SetActive(false);
            }
        }
    }


    public void SetQuestion(string question)
    {
        m_question.text = question;
        m_desc = question;
    }

    public void Answer(string answer)
    {
        // Mark as answered and hide the game object
        if (IsAnswerValid(answer))
        {
            isAnswered = true;
            gameObject.SetActive(false);

            ProcessAnswer(answer);

            survey.NextItem();
        }
    }

    public abstract void ProcessAnswer(string answer);

    public abstract bool IsAnswerValid(string answer);
}


public enum ItemType
{
    Survey,
    
    RepeatEpisode,
    
    SkipEpisode
}