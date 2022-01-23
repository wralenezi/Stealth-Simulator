using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using UnityEngine.UI;

[JsonObject(MemberSerialization.OptIn)]
public class Survey : MonoBehaviour
{
    private SurveyManager m_SurveyManager;
    private SurveyType m_type;

    private int currentSurveyIndex;

    private Session currentSession;

    // A container for the selected button in this survey
    private string previousButtonName;

    [JsonProperty("timeStamp")] private int currentSurveyTimeStamp;

    [JsonProperty("questions")] private List<SurveyItem> items;

    private string surveyItemPath = "Prefabs/UIs/SurveyItem";
    private GameObject surveyItemPrefab;

    public void Initiate(SurveyManager _surveyManager)
    {
        surveyItemPrefab = (GameObject) Resources.Load(surveyItemPath);
        items = new List<SurveyItem>();
        // gameObject.SetActive(false);
        currentSurveyIndex = 0;
        m_SurveyManager = _surveyManager;
    }


    /// <summary>
    /// Reset the survey properties and questions
    /// </summary>
    /// <param name="type">The type of the new survey</param>
    /// <param name="timeStamp">Timestamp of the session, this is to line the survey to the session</param>
    public void ResetSurvey(SurveyType type, int timeStamp)
    {
        previousButtonName = "";

        m_type = type;
        currentSurveyTimeStamp = timeStamp;

        // clear the survey items
        while (items.Count > 0)
        {
            DestroyImmediate(items[0].gameObject);
            items.RemoveAt(0);
        }

        currentSurveyIndex = 0;
    }


    /// <summary>
    /// Add a multiple choice item to the survey
    /// </summary>
    /// <param name="id"></param>
    /// <param name="code"></param>
    /// <param name="question"></param>
    /// <param name="choices"></param>
    public void AddItemMultiple(string id, string code, string question, List<Choice> choices)
    {
        // Create the item object and hide it
        GameObject surveyItemGo = Instantiate(surveyItemPrefab, transform);
        surveyItemGo.SetActive(false);

        SurveyMultiple surveyMultiple = surveyItemGo.AddComponent<SurveyMultiple>();
        surveyMultiple.Initiate(id, ItemType.Survey, this, code);

        // Add the question
        surveyMultiple.SetQuestion(question);

        // Add the options
        foreach (var choice in choices)
            surveyMultiple.AddChoice(choice);

        items.Add(surveyMultiple);
    }


    public void AddTutorialRepeat()
    {
        // Create the item object and hide it
        GameObject surveyItemGo = Instantiate(surveyItemPrefab, transform);
        surveyItemGo.SetActive(false);

        GameControlQuestion repeatTutorial = surveyItemGo.AddComponent<GameControlQuestion>();
        string name = "repeatTutorial";
        string question = "Would you like to repeat the tutorial level?";
        repeatTutorial.Initiate(name,ItemType.RepeatEpisode, this, "");

        // Add the question
        repeatTutorial.SetQuestion(question);

        List<Choice> choices = new List<Choice>();
        choices.Add(new Choice("Yes", "Yes", ButtonType.Survey, ColorBlock.defaultColorBlock.normalColor));
        choices.Add(new Choice("No", "No", ButtonType.Survey, ColorBlock.defaultColorBlock.normalColor));

        // Add the options
        foreach (var choice in choices)
            repeatTutorial.AddChoice(choice);

        items.Add(repeatTutorial);
    }
    
    public void AddTutorialSkip()
    {
        // Create the item object and hide it
        GameObject surveyItemGo = Instantiate(surveyItemPrefab, transform);
        surveyItemGo.SetActive(false);

        GameControlQuestion skipTutorial = surveyItemGo.AddComponent<GameControlQuestion>();
        string name = "skipTutorial";
        string question = "Would you like to skip the tutorial level?";
        skipTutorial.Initiate(name,ItemType.SkipEpisode, this, "");

        // Add the question
        skipTutorial.SetQuestion(question);

        List<Choice> choices = new List<Choice>();
        choices.Add(new Choice("Yes", "Yes", ButtonType.Survey, ColorBlock.defaultColorBlock.normalColor));
        choices.Add(new Choice("No", "No", ButtonType.Survey, ColorBlock.defaultColorBlock.normalColor));

        // Add the options
        foreach (var choice in choices)
            skipTutorial.AddChoice(choice);

        items.Add(skipTutorial);
    }
    
    public SurveyManager GetManager()
    {
        return m_SurveyManager;
    }

    public void SetSession(Session session)
    {
        currentSession = session;
    }

    public string GetGuardColor()
    {
        return currentSession.guardColor;
    }

    public void StartSurvey()
    {
        if (items.Count > 0)
            items[0].gameObject.SetActive(true);
    }

    public void ShowLoading()
    {
    }

    public void UpdateName(string itemName, string answer)
    {
        if (itemName.Contains("q1"))
        {
            previousButtonName = answer;
        }
    }

    private void ActiveItem()
    {
        items[currentSurveyIndex].gameObject.SetActive(true);

        if (items[currentSurveyIndex].name.Contains("q2"))
        {
            items[currentSurveyIndex].DeactivateInput(previousButtonName);
        }
    }
    
    public void ClearSurveyItems()
    {        
        items.Clear();
    }

    public void NextItem()
    {
        currentSurveyIndex++;

        if (currentSurveyIndex < items.Count)
            ActiveItem();
        else
            EndSurvey();
    }

    public void SetQuestionIndex(int index)
    {
        currentSurveyIndex = Mathf.Clamp(index,0, items.Count);
    }

    public int GetQuestionsCount()
    {
        return items.Count;
    }



    public void EndSurvey()
    {
        string surveyJson = JsonConvert.SerializeObject(this);

        switch (m_type)
        {
            case SurveyType.NewUser:
                StartCoroutine(FileUploader.UploadData(null,
                    FileType.User, "application/json", surveyJson));
                break;

            case SurveyType.EndTutorial:
                // StartCoroutine(FileUploader.UploadData(null,
                //     currentSurveyTimeStamp, "userSurvey", "application/json", surveyJson));
                break;

            case SurveyType.BehaviorEval:
                StartCoroutine(FileUploader.UploadData(currentSession,
                    FileType.Survey, "application/json", surveyJson));
                break;

            case SurveyType.EndEpisode:
                StartCoroutine(FileUploader.UploadData(currentSession,
                    FileType.Survey, "application/json", surveyJson));
                break;

            case SurveyType.SpeechEval:
                StartCoroutine(FileUploader.UploadData(currentSession,
                    FileType.Survey, "application/json", surveyJson));
                break;

            case SurveyType.End:
                Application.Quit();
                break;
        }
        

        m_SurveyManager.ClearImage();
        // GameManager.Instance.StartAreaAfterSurvey();
        StartCoroutine(GameManager.Instance.StartGamePostSurvey());
    }
}