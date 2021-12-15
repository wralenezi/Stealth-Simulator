using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ButtonController : MonoBehaviour
{
    private SurveyItem m_surveyItem;
    private ButtonType m_Type;

    public void Initiate(SurveyItem surveyItem, ButtonType type)
    {
        m_surveyItem = surveyItem;
        m_Type = type;
    }

    public void OnClick()
    {
        m_surveyItem.Answer(transform.Find("Text").GetComponent<TextMeshProUGUI>().text);
    }
}


public enum ButtonType
{
    Survey,
    
    GameStealth,

    GameCoin
}