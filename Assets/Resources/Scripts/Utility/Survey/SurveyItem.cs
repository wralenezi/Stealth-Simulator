using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using TMPro;

[JsonObject(MemberSerialization.OptIn)]
public abstract class SurveyItem : MonoBehaviour
{
    // The survey this item belongs to
    protected Survey survey;
    
    protected Transform questionPanel;
    private TextMeshProUGUI m_question;
    
    protected Transform inputPanel;

    public bool isAnswered;
    
    // Item info
    // Item name
    [JsonProperty("name")] private string m_name;
    // Item Description
    [JsonProperty("desc")] private string m_desc;
    // Item answer
    [JsonProperty("answer")] protected string m_answer;
    
    public virtual void Initiate(string name, Survey _survey)
    {
        survey = _survey;
        gameObject.name = name;
        m_name = name;
        
        questionPanel = transform.Find("QuestionPanel");
        m_question = questionPanel.Find("Text").GetComponent<TextMeshProUGUI>();
        
        inputPanel = transform.Find("InputPanel");
        
        RectTransform rect = GetComponent<RectTransform>(); 
        rect.sizeDelta = new Vector2(Screen.width,Screen.height);
    }
    
    

    public void SetQuestion(string question)
    {
        m_question.text = question;
        m_desc = question;
    }


    public abstract void Answer(string answer);


}
