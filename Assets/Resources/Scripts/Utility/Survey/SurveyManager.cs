using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SurveyManager : MonoBehaviour
{
    private Survey m_currentSurvey;

    private float m_FadeInSpeed = 0.01f;
    private Image m_FadeInScreen;

    // Survey item element
    List<Choice> m_Choices;
    string m_itemName;
    string m_itemDesc;

    public void Initiate()
    {
        m_Choices = new List<Choice>();
        m_currentSurvey = gameObject.AddComponent<Survey>();
        m_currentSurvey.Initiate(this);

        GameObject fadeInGameObject = new GameObject();
        fadeInGameObject.name = "FadeInScreen";
        fadeInGameObject.transform.parent = transform;
        fadeInGameObject.transform.localPosition = Vector3.zero;
        m_FadeInScreen = fadeInGameObject.AddComponent<Image>();
        fadeInGameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(
            GameManager.MainCamera.orthographicSize * 200f, GameManager.MainCamera.orthographicSize * 100f);
        Color bKColor = GameManager.MainCamera.backgroundColor;
        bKColor.a = 0f;
        m_FadeInScreen.color = bKColor;
    }

    public void SetSession(Session session)
    {
        m_currentSurvey.SetSession(session);
    }

    public void CreateSurvey(int timeStamp, SurveyType type, float score)
    {
        m_currentSurvey.ResetSurvey(type, timeStamp);

        switch (type)
        {
            case SurveyType.NewUser:
                CreateNewUserSurvey();
                break;

            case SurveyType.EndTutorial:
                m_currentSurvey.AddTutorialRepeat();
                break;

            case SurveyType.EndEpisode:
                DisplayScore(score);
                AddEndEpisodeQuestions();
                break;

            case SurveyType.BehaviorEval:
                DisplayScore(score);
                AddEndEpisodeQuestions();
                AddEvalBehaviorQuestions();
                break;

            case SurveyType.SpeechEval:
                DisplayScore(score);
                AddEndEpisodeQuestions();
                AddEvalSpeechQuestions();
                break;

            case SurveyType.End:
                EndGame();
                break;
        }
    }

    public void ShowSurvey()
    {
        StartCoroutine(FadeInSurvey());
    }

    private IEnumerator FadeInSurvey()
    {
        float alpha = 0f;
        Color bKColor = m_FadeInScreen.color;
        bKColor.a = alpha;
        m_FadeInScreen.color = bKColor;

        while (alpha <= 1f)
        {
            yield return new WaitForSecondsRealtime(0.01f);
            alpha += m_FadeInSpeed;
            bKColor.a = alpha;
            m_FadeInScreen.color = bKColor;
        }


        GameManager.Instance.SetGameActive(false);
        GameManager.Instance.EndNonTutorialGame();
        m_currentSurvey.StartSurvey();
    }

    public void ClearImage()
    {
        Color bKColor = m_FadeInScreen.color;
        bKColor.a = 0f;
        m_FadeInScreen.color = bKColor;
    }


    private void CreateNewUserSurvey()
    {
        // Consent
        m_Choices.Clear();
        m_Choices.Add(new Choice("Accept", "Accept", ButtonType.Survey, ColorBlock.defaultColorBlock.normalColor));
        m_itemName = "consent";
        m_itemDesc =
            "By participating in this study, I declare that I am over 18 years of age. I consent that the data I produce by playing this game can be used by the School of Computer Science at McGill University.";
        m_currentSurvey.AddItemMultiple(m_itemName, "", m_itemDesc, m_Choices);

        // Played before?
        m_Choices.Clear();
        m_Choices.Add(new Choice("Yes", "Yes", ButtonType.Survey, ColorBlock.defaultColorBlock.normalColor));
        m_Choices.Add(new Choice("No", "No", ButtonType.Survey, ColorBlock.defaultColorBlock.normalColor));
        m_itemName = "played before";
        m_itemDesc = "Have you played this game before?";
        m_currentSurvey.AddItemMultiple(m_itemName, "", m_itemDesc, m_Choices);
        
        // Consent
        m_Choices.Clear();
        m_Choices.Add(new Choice("Ok", "Next", ButtonType.Survey, ColorBlock.defaultColorBlock.normalColor));
        m_itemName = "nickname";
        m_itemDesc =
            "Set the nickname";
        m_currentSurvey.AddTextItem(m_itemName, "", m_itemDesc, m_Choices, true);

        // Video game skill
        m_Choices.Clear();
        m_Choices.Add(new Choice("Not at all", "Not at all", ButtonType.Survey, ColorBlock.defaultColorBlock.normalColor));
        m_Choices.Add(new Choice("Beginner", "Beginner", ButtonType.Survey, ColorBlock.defaultColorBlock.normalColor));
        m_Choices.Add(new Choice("Intermediate", "Intermediate", ButtonType.Survey, ColorBlock.defaultColorBlock.normalColor));
        m_Choices.Add(new Choice("Advanced", "Advanced", ButtonType.Survey, ColorBlock.defaultColorBlock.normalColor));
        m_itemName = "video games skill";
        m_itemDesc = "How much are you experienced with video games?";
        m_currentSurvey.AddItemMultiple(m_itemName, "", m_itemDesc, m_Choices);

        // Ask if play want to play the tutorial level
        m_currentSurvey.AddTutorialSkip();

        // Explain the game
        m_Choices.Clear();
        m_Choices.Add(new Choice("Next", "Next", ButtonType.Survey, ColorBlock.defaultColorBlock.normalColor));
        m_itemName = "explain game";
        
        string gameDesc = "Avoid detection and collect coins to reach 100.\nIf you are seen, your score will decrease with time.";
        m_itemDesc = gameDesc;
        m_currentSurvey.AddItemMultiple(m_itemName, "", m_itemDesc, m_Choices);

        m_Choices.Clear();
        m_Choices.Add(new Choice("Go", "Go!", ButtonType.Survey, ColorBlock.defaultColorBlock.normalColor));
        m_itemName = "explain game 2 ";
        m_itemDesc =
            "You are spotted! Lose track of the robots and get a score of 100 before time runs out!\nMove your character with direction arrows.";
        m_currentSurvey.AddItemMultiple(m_itemName, "", m_itemDesc, m_Choices);
    }


    public void AddIntroToFirstHalf()
    {
        // Info for the next round
        m_Choices.Clear();
        m_Choices.Add(new Choice("Next", "Next", ButtonType.Survey, ColorBlock.defaultColorBlock.normalColor));
        m_itemName = "info";
        m_itemDesc =
            "Now, you will be playing 4 rounds in a new map. In each round, you will be playing against a different team of robots.";
        m_currentSurvey.AddItemMultiple(m_itemName, "", m_itemDesc, m_Choices);

        // Info for the next round
        m_Choices.Clear();
        m_Choices.Add(new Choice("Start", "Start Game", ButtonType.Survey, ColorBlock.defaultColorBlock.normalColor));
        m_itemName = "info";
        m_itemDesc =
            "Try to observe their behavior, as you will be asked some questions on their behavior after each round.";
        m_currentSurvey.AddItemMultiple(m_itemName, "", m_itemDesc, m_Choices);
    }

    // Display the score of the player
    private void DisplayScore(float score)
    {
        string message = "";
        m_Choices.Clear();

        if (score >= 100)
        {
            message = "Well Done!! Mission Accomplished!";
            m_Choices.Add(new Choice("Win", "Next", ButtonType.Survey, ColorBlock.defaultColorBlock.normalColor));
        }
        else if (score < 100)
        {
            message = "Mission Failed! Better luck next time!";
            m_Choices.Add(new Choice("Loss", "Next", ButtonType.Survey, ColorBlock.defaultColorBlock.normalColor));
        }

        m_itemName = "win";
        m_itemDesc = message;
        m_currentSurvey.AddItemMultiple(m_itemName, "", m_itemDesc, m_Choices);
    }


    private void AddEndEpisodeQuestions()
    {
        // Fun
        m_Choices.Clear();
        m_Choices.Add(new Choice("Not so much", "Not so much", ButtonType.Survey, ColorBlock.defaultColorBlock.normalColor));
        m_Choices.Add(new Choice("Fairly fun", "Fairly fun", ButtonType.Survey, ColorBlock.defaultColorBlock.normalColor));
        m_Choices.Add(new Choice("So much fun", "So much fun", ButtonType.Survey, ColorBlock.defaultColorBlock.normalColor));
        m_itemName = "fun";
        m_itemDesc = "How much did you enjoy playing against " + m_currentSurvey.GetGuardColor() + "?";
        m_currentSurvey.AddItemMultiple(m_itemName, "", m_itemDesc, m_Choices);

        // Difficulty
        m_Choices.Clear();
        m_Choices.Add(new Choice("Easy", "Easy", ButtonType.Survey, ColorBlock.defaultColorBlock.normalColor));
        m_Choices.Add(new Choice("Medium", "Medium", ButtonType.Survey, ColorBlock.defaultColorBlock.normalColor));
        m_Choices.Add(new Choice("Hard", "Hard", ButtonType.Survey, ColorBlock.defaultColorBlock.normalColor));
        m_itemName = "level difficulty";
        m_itemDesc = "How hard was it to play against " + m_currentSurvey.GetGuardColor() + "?";
        m_currentSurvey.AddItemMultiple(m_itemName, "", m_itemDesc, m_Choices);

        // Naturalness
        m_Choices.Clear();
        m_Choices.Add(new Choice("Not natural", "Not natural", ButtonType.Survey, ColorBlock.defaultColorBlock.normalColor));
        m_Choices.Add(new Choice("Acceptable", "Acceptable", ButtonType.Survey, ColorBlock.defaultColorBlock.normalColor));
        m_Choices.Add(new Choice("Very natural", "Very natural", ButtonType.Survey, ColorBlock.defaultColorBlock.normalColor));
        m_itemName = "behavior naturalness";
        m_itemDesc = "How natural the " + m_currentSurvey.GetGuardColor() + " team`s behavior was?";
        m_currentSurvey.AddItemMultiple(m_itemName, "", m_itemDesc, m_Choices);
    }

    private void AddEvalBehaviorQuestions()
    {
        // Most enjoyable enemy
        // 1st
        m_Choices.Clear();
        for (int i = 0; i < SessionsSetup.GetPlayedSessions().Count; i++)
        {
            string color = SessionsSetup.GetPlayedSessions()[i];
            {
                ColorUtility.TryParseHtmlString(color, out Color parsedColor);
                m_Choices.Add(new Choice(color, "Team " + (i + 1) + " (" + color + ")", ButtonType.Survey,
                    parsedColor));
            }
        }

        m_itemName = "preferred enemy q1";
        m_itemDesc = "Which robot team was the most FUN?";
        m_currentSurvey.AddItemMultiple(m_itemName, "color", m_itemDesc, m_Choices);

        if (SessionsSetup.GetPlayedSessions().Count > 2)
        {
            // 2nd
            m_Choices.Clear();
            for (int i = 0; i < SessionsSetup.GetPlayedSessions().Count; i++)
            {
                string color = SessionsSetup.GetPlayedSessions()[i];
                ColorUtility.TryParseHtmlString(color, out Color parsedColor);
                m_Choices.Add(new Choice(color, "Team " + (i + 1) + " (" + color + ")", ButtonType.Survey,
                    parsedColor));
            }

            m_itemName = "preferred enemy q2";
            m_itemDesc = "Which robot team was the second most FUN?";
            m_currentSurvey.AddItemMultiple(m_itemName, "color", m_itemDesc, m_Choices);
        }

        // Enemy Difficulty ranking
        // 1st
        m_Choices.Clear();
        for (int i = 0; i < SessionsSetup.GetPlayedSessions().Count; i++)
        {
            string color = SessionsSetup.GetPlayedSessions()[i];
            ColorUtility.TryParseHtmlString(color, out Color parsedColor);
            m_Choices.Add(new Choice(color, "Team " + (i + 1) + " (" + color + ")", ButtonType.Survey, parsedColor));
        }

        m_itemName = "difficult enemy q1";
        m_itemDesc = "Which robot team was the most DIFFICULT?";
        m_currentSurvey.AddItemMultiple(m_itemName, "color", m_itemDesc, m_Choices);

        if (SessionsSetup.GetPlayedSessions().Count > 2)
        {
            // 2nd
            m_Choices.Clear();
            for (int i = 0; i < SessionsSetup.GetPlayedSessions().Count; i++)
            {
                string color = SessionsSetup.GetPlayedSessions()[i];
                ColorUtility.TryParseHtmlString(color, out Color parsedColor);
                m_Choices.Add(new Choice(color, "Team " + (i + 1) + " (" + color + ")", ButtonType.Survey,
                    parsedColor));
            }

            m_itemName = "difficult enemy q2";
            m_itemDesc = "Which robot team was the second most DIFFICULT?";
            m_currentSurvey.AddItemMultiple(m_itemName, "color", m_itemDesc, m_Choices);
        }
    }

    private void AddEvalSpeechQuestions()
    {
        // Consent
        m_Choices.Clear();
        for (int i = 0; i < SessionsSetup.speechColors.Count; i++)
        {
            string color = SessionsSetup.speechColors[i];
            ColorUtility.TryParseHtmlString(color, out Color parsedColor);
            m_Choices.Add(new Choice(color, "Team " + (i + 1) + " (" + color + ")", ButtonType.Survey, parsedColor));
        }

        m_itemName = "preferred enemy";
        m_itemDesc = "Which robot team was the most FUN?";
        m_currentSurvey.AddItemMultiple(m_itemName, "color", m_itemDesc, m_Choices);


        m_Choices.Clear();
        for (int i = 0; i < SessionsSetup.speechColors.Count; i++)
        {
            string color = SessionsSetup.speechColors[i];
            ColorUtility.TryParseHtmlString(color, out Color parsedColor);

            m_Choices.Add(new Choice(color, "Team " + (i + 1) + " (" + color + ")", ButtonType.Survey, parsedColor));
        }

        m_itemName = "preferred enemy";
        m_itemDesc = "Which robot team was the most DIFFICULT?";
        m_currentSurvey.AddItemMultiple(m_itemName, "color", m_itemDesc, m_Choices);
    }

    private void EndGame()
    {
        m_Choices.Clear();
        // m_Choices.Add(new Choice("Exit", "Exit", ButtonType.Survey));
        m_itemName = "End";
        m_itemDesc =
            "Thank for playing!You can play against other teams by restarting the game.\nTo restart you can press F5 on the keyboard.";
        m_currentSurvey.AddItemMultiple(m_itemName, "", m_itemDesc, m_Choices);
    }
}

public enum SurveyType
{
    // Survey for a new user
    NewUser,

    // Survey displayed after the end of the tutorial
    EndTutorial,

    // End of the episode
    EndEpisode,

    // Survey regarding the 3 behaviors
    BehaviorEval,

    // Survey regarding the speech impact
    SpeechEval,

    // The end of the study 
    End
}


public struct Choice
{
    // Value of the choice
    public string value;

    // Label of the choice (visible for user; like in a button)
    public string label;
    public ButtonType type;

    public Color color;

    public Choice(string _value, string _label, ButtonType _buttonType, Color _color = default)
    {
        value = _value;
        label = _label;
        type = _buttonType;
        color = _color;
    }
}