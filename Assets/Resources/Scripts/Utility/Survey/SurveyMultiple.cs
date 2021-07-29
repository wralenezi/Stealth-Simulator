using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SurveyMultiple : SurveyItem
{
    private GameObject BtnPrefab;
    private string btnPath = "Prefabs/UIs/Buttons/Button";

    private List<string> choices;
    private List<Button> buttons;

    
    public override void Initiate(string name, Survey survey)
    {
        base.Initiate(name, survey);

        BtnPrefab = (GameObject) Resources.Load(btnPath);
        
        choices = new List<string>();
        buttons = new List<Button>();
    }
    
    public void AddOption(string option, ButtonType buttonType)
    {
        choices.Add(option);
        GameObject btnGo = Instantiate(BtnPrefab, inputPanel);
        btnGo.name = option + "_btn";
        btnGo.transform.Find("Text").GetComponent<TextMeshProUGUI>().text = option;
        Button btn = btnGo.GetComponent<Button>();
        ButtonController btnCon = btnGo.GetComponent<ButtonController>();
        btnCon.Initiate(this, buttonType);
        
        buttons.Add(btn);
    }


    public override void Answer(string _answer)
    {
        isAnswered = true;
        gameObject.SetActive(false);
        m_answer = _answer;
        survey.NextItem();
    }
}
