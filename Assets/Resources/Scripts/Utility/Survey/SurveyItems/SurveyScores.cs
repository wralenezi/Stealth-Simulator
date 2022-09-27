using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SurveyScores : SurveyItem
{
    private Dictionary<string, ScoresTable> _scoresTables;

    private string surveyScorePath = "Prefabs/UIs/ScoresTable";
    private GameObject surveyScorePrefab;

    private GameObject BtnPrefab;
    private string btnPath = "Prefabs/UIs/Buttons/Button";

    private List<Choice> choices;
    private List<Button> buttons;

    public override void Initiate(string _name, ItemType _type, Survey _survey, string _code)
    {
        base.Initiate(_name, _type, _survey, _code);

        _scoresTables = new Dictionary<string, ScoresTable>();

        surveyScorePrefab = (GameObject) Resources.Load(surveyScorePath);

        GameObject surveyItemGo;
        foreach (var session in GameManager.Instance.GetClosedSessions())
        {
            surveyItemGo = Instantiate(surveyScorePrefab, inputPanel);
            _scoresTables.Add(session.guardColor, surveyItemGo.GetComponent<ScoresTable>());
            _scoresTables[session.guardColor].Initiate(session);
        }

        choices = new List<Choice>();
        buttons = new List<Button>();

        BtnPrefab = (GameObject) Resources.Load(btnPath);

        GameObject buttonPanel = new GameObject();
        buttonPanel.transform.parent = inputPanel;

        VerticalLayoutGroup verticalLayoutGroup = buttonPanel.AddComponent<VerticalLayoutGroup>();
        verticalLayoutGroup.childControlWidth = true;
        verticalLayoutGroup.childControlHeight = true;
        verticalLayoutGroup.childForceExpandWidth = true;
        // verticalLayoutGroup.

        AddChoice(
            new Choice("sameUser", "Play Again!\n(Same user)", ButtonType.Survey,
                ColorBlock.defaultColorBlock.normalColor), buttonPanel.transform);
        AddChoice(
            new Choice("newUser", "Play Again!\n(New user)", ButtonType.Survey,
                ColorBlock.defaultColorBlock.normalColor), buttonPanel.transform);

        // surveyItemGo.SetActive(false);
    }

    public void AddChoice(Choice choice, Transform parent)
    {
        choices.Add(choice);
        GameObject btnGo = Instantiate(BtnPrefab, parent);
        btnGo.name = choice.value + "_btn";
        btnGo.transform.Find("Text").GetComponent<TextMeshProUGUI>().text = choice.label;
        Button btn = btnGo.GetComponent<Button>();
        ButtonController btnCon = btnGo.GetComponent<ButtonController>();
        btnCon.Initiate(this, choice);

        // Set the color
        ColorBlock colorBlock = ColorBlock.defaultColorBlock;
        colorBlock.normalColor = choice.color - new Color(0.3f, 0.3f, 0.3f, 0f);
        ;
        colorBlock.highlightedColor = colorBlock.normalColor - new Color(0.2f, 0.2f, 0.2f, 0f);
        btn.colors = colorBlock;
        btn.transform.GetChild(0).GetComponent<TextMeshProUGUI>().color = new Color(1 - colorBlock.normalColor.r,
            1 - colorBlock.normalColor.g, 1 - colorBlock.normalColor.b, 1);

        buttons.Add(btn);
    }


    public override void ProcessAnswer(string answer)
    {
        if (Equals(answer, "newUser")) PlayerData.PlayerName = "";

        SceneManager.LoadScene("Main");
    }

    public override bool IsAnswerValid(string answer)
    {
        return true;
    }
}