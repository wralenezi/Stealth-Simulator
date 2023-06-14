using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class MenuRadioButtonItem : MenuItem
{
    private GameObject BtnPrefab;
    private string btnPath = "Prefabs/UIs/Buttons/MenuButton";

    private List<MenuOption> choices;
    private List<Button> buttons;
    
    public override void Initiate(string name, Menu menu, string code)
    {
        base.Initiate(name, menu, code);

        BtnPrefab = (GameObject) Resources.Load(btnPath);

        choices = new List<MenuOption>();
        buttons = new List<Button>();
    }

    public void AddChoice(MenuOption choice)
    {
        choices.Add(choice);
        GameObject btnGo = Instantiate(BtnPrefab, inputPanel);
        btnGo.name = choice.value + "_btn";
        btnGo.transform.Find("Text").GetComponent<TextMeshProUGUI>().text = choice.label;
        Button btn = btnGo.GetComponent<Button>();
        MenuButton btnCon = btnGo.GetComponent<MenuButton>();
        btnCon.Initiate(this, choice);

        // Set the color
        ColorBlock colorBlock = ColorBlock.defaultColorBlock;
        float difference = 0.1f;
        colorBlock.normalColor = new Color(difference + colorBlock.normalColor.r,
            difference + colorBlock.normalColor.g, difference + colorBlock.normalColor.b, 1);
        difference = -0.3f;
        colorBlock.highlightedColor = new Color(difference + colorBlock.normalColor.r,
            difference + colorBlock.normalColor.g, difference + colorBlock.normalColor.b, 1);
        
        btn.colors = colorBlock;
        btn.transform.GetChild(0).GetComponent<TextMeshProUGUI>().color = new Color(1 - colorBlock.normalColor.r,
            1 - colorBlock.normalColor.g, 1 - colorBlock.normalColor.b, 1);


        buttons.Add(btn);
    }

    public override void ProcessAnswer(string _answer)
    {
        // Check the answer and process it based on the code.
        if (Equals(m_code, "color"))
        {
            // menu.UpdateName(name, _answer);
        }

        m_answer = _answer;
    }

    public override bool IsAnswerValid(string answer)
    {
        return true;
    }
}
