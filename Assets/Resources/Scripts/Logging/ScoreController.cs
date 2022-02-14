using UnityEngine;

public class ScoreController : MonoBehaviour
{
    public float score { get; private set; }

    public static ScoreController Instance; 

    public void Initialize()
    {
        Instance ??= this;
        
        score = 0f;
    }
    
    public void UpdateScore(float _score)
    {
        score = _score;

        // if (GameManager.Instance.GetActiveArea().GetSessionInfo().gameType == GameType.CoinCollection)
        //     scoreLabel.text = "Score: " + score;
        // else
        //     scoreLabel.text = "Score: " + score + " %";
    }

    
}
