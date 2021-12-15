using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RepeatTutorial : SurveyMultiple
{
    public override void Answer(string _answer)
    {
        // Mark as answered and hide the game object
        isAnswered = true;
        gameObject.SetActive(false);
        
        if (Equals(_answer, "No"))
        {
            GameManager.Instance.EndCurrentGame();
            survey.GetManager().AddIntroToFirstHalf();
        }

        survey.NextItem();
    }
}