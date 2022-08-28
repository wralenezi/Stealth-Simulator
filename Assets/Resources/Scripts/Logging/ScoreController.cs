using UnityEngine;

public class ScoreController : MonoBehaviour
{
    public float seenTime { get; private set; }

    public int coin { get; private set; }

    public float Score { get; private set; }

    public static ScoreController Instance; 

    public void Reset()
    {
        Instance = this;
        seenTime = 0f;
        coin = 0;
        Score = 0f;
    }

    public float CalculateScore()
    {
        float coinValue = 100f;
        float timeMultiplier = 100f;
        float score;
        
        // score = coinValue * coin / (1f + seenTime);
        score = coinValue * coin - seenTime * timeMultiplier;
        Score = Mathf.Round(score);
        
        return Score;
    }

    public void IncrementCoin()
    {
        coin++;
    }

    public void IncrementSeenTime(float time)
    {
        seenTime += time;
    }

    public void UpdateScore(float _score)
    {
        Score = _score;
    }

    public void IncrementScore(float _score)
    {
        float oldScore = Score;
        Score += _score;
        Score = Mathf.Max(0, Score);
        AreaUIManager.Instance.UpdateScore(Score, oldScore);
    }

    
}
