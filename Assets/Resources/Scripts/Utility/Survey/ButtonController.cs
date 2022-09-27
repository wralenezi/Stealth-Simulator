using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ButtonController : MonoBehaviour
{
    private SurveyItem m_surveyItem;
    private Choice _choice;

    public void Initiate(SurveyItem surveyItem, Choice choice)
    {
        m_surveyItem = surveyItem;
        _choice = choice;
    }

    public void OnClick()
    {
        m_surveyItem.Answer(_choice.value);
    }
}


public enum ButtonType
{
    Survey,
    
    GameStealth,

    GameCoin
}