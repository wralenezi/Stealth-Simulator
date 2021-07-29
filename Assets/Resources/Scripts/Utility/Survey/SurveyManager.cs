using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SurveyManager : MonoBehaviour
{
    private Survey m_currentSurvey;

    // Survey item element
    List<Choice> m_choices = new List<Choice>();
    string m_itemName;
    string m_itemDesc;

    public void Initiate()
    {
        m_currentSurvey = gameObject.AddComponent<Survey>();
        m_currentSurvey.Initiate();
    }

    public void SetSession(Session session)
    {
        m_currentSurvey.SetSession(session);
    }

    public void CreateNewUserSurvey(int timeStamp)
    {
        m_currentSurvey.ResetSurvey(SurveyType.NewUser, timeStamp);

        // Consent
        m_choices.Clear();
        m_choices.Add(new Choice("Accept", ButtonType.Survey));
        m_itemName = "consent";
        m_itemDesc =
            "By participating in this study, I declare that I am over 18 years of age. I consent that the data I produce by playing this game can be used by the School of Computer Science at McGill University.";
        m_currentSurvey.AddSurveyMultiple(m_itemName, m_itemDesc, m_choices);
        
        // Video game skill
        m_choices.Clear();
        m_choices.Add(new Choice("Stealth", ButtonType.GameStealth));
        m_choices.Add(new Choice("CollectCoin", ButtonType.GameCoin));
        m_itemName = "game Type";
        m_itemDesc = "Which game type you want to play?";
        m_currentSurvey.AddSurveyMultiple(m_itemName, m_itemDesc, m_choices);
        
        // Video game skill
        m_choices.Clear();
        m_choices.Add(new Choice("Not at all", ButtonType.Survey));
        m_choices.Add(new Choice("Beginner", ButtonType.Survey));
        m_choices.Add(new Choice("Intermediate", ButtonType.Survey));
        m_choices.Add(new Choice("Advanced", ButtonType.Survey));
        m_itemName = "video games skill";
        m_itemDesc = "How much are you experienced with video games?";
        m_currentSurvey.AddSurveyMultiple(m_itemName, m_itemDesc, m_choices);

        // Start the tutorial level
        m_choices.Clear();
        m_choices.Add(new Choice("Start", ButtonType.Survey));
        m_itemName = "start tutorial level";
        m_itemDesc = "Play tutorial level";
        m_currentSurvey.AddSurveyMultiple(m_itemName, m_itemDesc, m_choices);

        // Show the survey
        m_currentSurvey.StartSurvey();
    }


    public void CreateEndAreaSurvey(int timeStamp)
    {
        m_currentSurvey.ResetSurvey(SurveyType.EndSession, timeStamp);

        // Consent
        m_choices.Clear();
        m_choices.Add(new Choice("Easy", ButtonType.Survey));
        m_choices.Add(new Choice("Medium", ButtonType.Survey));
        m_choices.Add(new Choice("Hard", ButtonType.Survey));
        m_itemName = "level difficulty";
        m_itemDesc = "How hard was this level?";
        m_currentSurvey.AddSurveyMultiple(m_itemName, m_itemDesc, m_choices);

        m_choices.Clear();
        m_choices.Add(new Choice("Not much", ButtonType.Survey));
        m_choices.Add(new Choice("Fairly good", ButtonType.Survey));
        m_choices.Add(new Choice("Very much", ButtonType.Survey));
        m_itemName = "level difficulty";
        m_itemDesc = "How much did you enjoy it?";
        m_currentSurvey.AddSurveyMultiple(m_itemName, m_itemDesc, m_choices);

        // Show the survey
        m_currentSurvey.StartSurvey();
    }


    public void CreateEndSurvey(int timeStamp)
    {
        m_currentSurvey.ResetSurvey(SurveyType.EndSurvey, timeStamp);

        // Consent
        m_choices.Clear();
        m_choices.Add(new Choice("Red", ButtonType.Survey));
        m_choices.Add(new Choice("Green", ButtonType.Survey));
        m_choices.Add(new Choice("Yellow", ButtonType.Survey));
        m_itemName = "preferred enemy";
        m_itemDesc = "Against which enemy you enjoyed playing the most?";
        m_currentSurvey.AddSurveyMultiple(m_itemName, m_itemDesc, m_choices);

        // Show the survey
        m_currentSurvey.StartSurvey();
    }


    public void EndSurvey(int timeStamp)
    {
        m_currentSurvey.ResetSurvey(SurveyType.EndSession, timeStamp);

        m_choices.Clear();
        m_itemName = "End";
        m_itemDesc = "Thank for playing! If you would like to play again please refresh the page by pressing F5.";
        m_currentSurvey.AddSurveyMultiple(m_itemName, m_itemDesc, m_choices);

        // Show the survey
        m_currentSurvey.StartSurvey();
    }
}

public enum SurveyType
{
    // Survey for a new user
    NewUser,

    // Survey displayed at the end of each session
    EndSession,

    // The end of the study 
    EndSurvey
}

public struct Choice
{
    public string name;
    public ButtonType type;

    public Choice(string _name, ButtonType _buttonType)
    {
        name = _name;
        type = _buttonType;
    }
}