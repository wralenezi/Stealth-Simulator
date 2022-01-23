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


    public override void Initiate(string name, ItemType _type, Survey survey, string code)
    {
        base.Initiate(name, _type, survey, code);

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

        // Set the color
        ColorBlock colorBlock = ColorBlock.defaultColorBlock;
        colorBlock.normalColor = choice.color - new Color(0.3f, 0.3f, 0.3f, 0f);;
        colorBlock.highlightedColor = colorBlock.normalColor - new Color(0.2f, 0.2f, 0.2f,0f);
        btn.colors = colorBlock;


        buttons.Add(btn);
    }

    public override void ProcessAnswer(string _answer)
    {
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
    }
}