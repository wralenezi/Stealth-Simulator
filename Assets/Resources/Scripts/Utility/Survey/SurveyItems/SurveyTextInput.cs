using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SurveyTextInput : SurveyItem
{
    private GameObject textInputObject;
    private TMP_InputField _inputField;
    private string inputFieldPath = "Prefabs/UIs/InputField";

    private string btnPath = "Prefabs/UIs/Buttons/Button";
    private GameObject _btnGo;
    private Button _btn;

    public void Initiate(string name, ItemType _type, Survey survey, string code, bool isNickname)
    {
        base.Initiate(name, _type, survey, code);

        GameObject prefab = (GameObject) Resources.Load(inputFieldPath);
        textInputObject = Instantiate(prefab, inputPanel);
        _inputField = textInputObject.GetComponent<TMP_InputField>();

        if (isNickname)
        {
            _inputField.characterLimit = 3;
            _inputField.pointSize = 72f;
        }

        AddChoice(new Choice("ok", "Next", ButtonType.Survey, ColorBlock.defaultColorBlock.normalColor));
    }

    public void AddChoice(Choice choice)
    {
        GameObject BtnPrefab = (GameObject) Resources.Load(btnPath);
        _btnGo = Instantiate(BtnPrefab, inputPanel);
        _btnGo.name = choice.value + "_btn";
        _btnGo.transform.Find("Text").GetComponent<TextMeshProUGUI>().text = choice.label;
        _btn = _btnGo.GetComponent<Button>();
        ButtonController btnCon = _btnGo.GetComponent<ButtonController>();
        btnCon.Initiate(this, choice.type);

        // Set the color
        ColorBlock colorBlock = ColorBlock.defaultColorBlock;
        colorBlock.normalColor = choice.color - new Color(0.3f, 0.3f, 0.3f, 0f);
        ;
        colorBlock.highlightedColor = colorBlock.normalColor - new Color(0.2f, 0.2f, 0.2f, 0f);
        _btn.colors = colorBlock;
    }


    public override void ProcessAnswer(string answer)
    {
        m_answer = _inputField.text;
    }
}