using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

[JsonObject(MemberSerialization.OptIn)]
public class Survey : MonoBehaviour
{
    private SurveyType m_type;

    private int currentSurveyIndex;

    private Session currentSession;


    [JsonProperty("timeStamp")] private int currentSurveyTimeStamp;

    [JsonProperty("questions")] private List<SurveyItem> items;

    private string surveyItemPath = "Prefabs/UIs/SurveyItem";
    private GameObject surveyItemPrefab;

    public void Initiate()
    {
        surveyItemPrefab = (GameObject) Resources.Load(surveyItemPath);
        items = new List<SurveyItem>();
        gameObject.SetActive(false);
        currentSurveyIndex = 0;
    }


    public void ResetSurvey(SurveyType type, int timeStamp)
    {
        m_type = type;
        currentSurveyTimeStamp = timeStamp;

        while (items.Count > 0)
        {
            DestroyImmediate(items[0].gameObject);
            items.RemoveAt(0);
        }

        currentSurveyIndex = 0;
    }


    public void AddSurveyMultiple(string name, string question, List<Choice> choices)
    {
        GameObject surveyItemGo = Instantiate(surveyItemPrefab, transform);
        surveyItemGo.SetActive(false);

        SurveyMultiple surveyMultiple = surveyItemGo.AddComponent<SurveyMultiple>();
        surveyMultiple.Initiate(name, this);

        // Add the question
        surveyMultiple.SetQuestion(question);

        // Add the options
        foreach (var choice in choices)
            surveyMultiple.AddOption(choice.name,choice.type);
        
        items.Add(surveyMultiple);
    }

    public void SetSession(Session session)
    {
        currentSession = session;
    }

    public void StartSurvey()
    {
        gameObject.SetActive(true);

        if (items.Count > 0)
            items[0].gameObject.SetActive(true);
    }

    public void NextItem()
    {
        currentSurveyIndex++;

        if (currentSurveyIndex < items.Count)
            items[currentSurveyIndex].gameObject.SetActive(true);
        else
            EndSurvey();
    }

    public void EndSurvey()
    {
        string surveyJson = JsonConvert.SerializeObject(this);
        
        switch (m_type)
        {
            case SurveyType.NewUser:
                StartCoroutine(FileUploader.UploadData(null,
                    currentSurveyTimeStamp, "userSurvey", "application/json", surveyJson));
                GameManager.instance.StartAreaAfterSurvey();
                break;

            case SurveyType.EndSession:
                StartCoroutine(FileUploader.UploadData(currentSession,
                    currentSurveyTimeStamp, "sessionSurvey", "application/json", surveyJson));
                GameManager.instance.StartAreaAfterSurvey();

                break;

            case SurveyType.EndSurvey:
                StartCoroutine(FileUploader.UploadData(null,
                    currentSurveyTimeStamp, "endSurvey", "application/json", surveyJson));
                GameManager.SurveyManager.EndSurvey(currentSurveyTimeStamp);
                break;
        }
    }
}