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


    public override void Initiate(string name, Survey survey, string code)
    {
        base.Initiate(name, survey, code);

        BtnPrefab = (GameObject) Resources.Load(btnPath);

        choices = new List<string>();
        buttons = new List<Button>();
    }

    public void AddChoice(Choice choice)
    {
        choices.Add(choice.value);
        GameObject btnGo = Instantiate(BtnPrefab, inputPanel);
        btnGo.name = choice.value + "_btn";
        btnGo.transform.Find("Text").GetComponent<TextMeshProUGUI>().text = choice.label;
        Button btn = btnGo.GetComponent<Button>();
        ButtonController btnCon = btnGo.GetComponent<ButtonController>();
        btnCon.Initiate(this, choice.type);

        buttons.Add(btn);
    }


    public override void Answer(string _answer)
    {
        // Mark as answered and hide the game object
        isAnswered = true;
        gameObject.SetActive(false);

        // Check the answer and process it based on the code.
        if (Equals(m_code, "color"))
        {
            int valueIndex = buttons.FindIndex(x =>
                x.transform.Find("Text").GetComponent<TextMeshProUGUI>().text.Equals(_answer));

            string value = buttons[valueIndex].name;
            survey.UpdateName(name, value);

            m_answer = SessionsSetup.GetCategoryFromColor(choices[valueIndex]);
        }
        else
            m_answer = _answer;

        survey.NextItem();
    }
}