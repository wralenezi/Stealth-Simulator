using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SurveyScores : SurveyItem
{

    private string surveyScorePath = "Prefabs/UIs/ScoresTable";
    private GameObject surveyScorePrefab;
    
    public override void Initiate(string _name, ItemType _type, Survey _survey, string _code)
    {
        base.Initiate(_name, _type, _survey, _code);
        
        surveyScorePrefab = (GameObject) Resources.Load(surveyScorePath);

        GameObject surveyItemGo = Instantiate(surveyScorePrefab, inputPanel);
        Instantiate(surveyScorePrefab, inputPanel);
        Instantiate(surveyScorePrefab, inputPanel);
        // surveyItemGo.SetActive(false);
    }

    public override void ProcessAnswer(string answer)
    {
        throw new System.NotImplementedException();
    }
}
