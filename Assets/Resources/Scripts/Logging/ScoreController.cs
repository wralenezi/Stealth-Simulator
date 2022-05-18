using UnityEngine;

public class ScoreController : MonoBehaviour
{
    public float score { get; private set; }

    public static ScoreController Instance; 

    public void Reset()
    {
        Instance = this;
        
        score = 0f;
    }
    
    
    public void UpdateScore(float _score)
    {
        score = _score;
    }

    
}
