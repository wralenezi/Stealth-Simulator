using UnityEngine;


public class StealthArea : MonoBehaviour
{
    // The episode time 
    private static float _episodeStartTime;

    // Session data
    public static Session SessionInfo;

    public MapManager Map { private set; get; }

    public NpcsManager NpcManager { private set; get; }

    public CollectablesManager CollectManager { private set; get; }

    // Logging manager
    public PerformanceLogger performanceMonitor { get; set; }

    public ScoreController scoreController { get; set; }

    // UI label manager
    public AreaUIManager AreaUiManager { get; set; }

    // Initiate the area
    public void InitiateArea(Session session)
    {
        SessionInfo = session;
        SessionInfo.SetTimestamp();

        scoreController = UnityHelper.AddChildComponent<ScoreController>(transform, "Scores");
        scoreController.Reset();

        AreaUiManager = transform.Find("Canvas").gameObject.GetComponent<AreaUIManager>();
        AreaUiManager.Initiate();

        // Get the map object 
        Map = UnityHelper.AddChildComponent<MapManager>(transform, "Map");
        Map.Initiate(GetSessionInfo().GetMap());

        NpcManager = UnityHelper.AddChildComponent<NpcsManager>(transform, "NPCs");
        NpcManager.Initialize(GetSessionInfo(), Map);

        CollectManager = UnityHelper.AddChildComponent<CollectablesManager>(transform, "Collectables");
        CollectManager.Initialize(GetSessionInfo());
        
        // Reference for recording the performance
        performanceMonitor = gameObject.AddComponent<PerformanceLogger>();
        performanceMonitor.SetArea(GetSessionInfo());
        performanceMonitor.Initialize();
        performanceMonitor.ResetResults();
        
        // World state variables
        WorldState.Reset();

        // Reset World Representation and NPCs
        ResetArea();
    }

    public void ResetArea()
    {
        _episodeStartTime = Time.time;
        WorldState.Set("episodeTime", _episodeStartTime.ToString());
        WorldState.Set("guardsCount", SessionInfo.guardsCount.ToString());

        NpcManager.Reset(Map.GetNavMesh(), SessionInfo);
        
        performanceMonitor.ResetResults();

        Bounds bounds = Map.mapRenderer.GetMapBoundingBox();
        GameManager.MainCamera.transform.position = new Vector3((bounds.min.x + bounds.max.x) * 0.5f,
            (bounds.min.y + bounds.max.y) * 0.5f, -1f);

        float mapWidth = bounds.max.x - bounds.min.x + 5f;
        float unitsPerPixel = mapWidth / Screen.width;
        float desiredHalfHeight = 0.5f * unitsPerPixel * Screen.height;
        GameManager.MainCamera.orthographicSize = desiredHalfHeight;

        ColorUtility.TryParseHtmlString(SessionInfo.guardColor, out Color parsedColor);
        GameManager.MainCamera.backgroundColor = parsedColor - new Color(0.5f, 0.5f, 0.5f, 0.1f);

        AreaUiManager.Reset();
        AreaUiManager.UpdateGuardLabel(SessionInfo.guardColor, parsedColor);

        if (GameManager.Instance.showSurvey) Time.timeScale = 0f;
    }

    public static float GetElapsedTime()
    {
        return Time.time - _episodeStartTime;
    }

    public void StartArea()
    {
        ResetArea();
        gameObject.SetActive(true);
        // if (GameManager.Instance.showSurvey) StartCoroutine(Countdown());
    }

    private void EndEpisode()
    {
        NpcManager.Done();
    }


    private void Update()
    {
        // Update the time label
        AreaUiManager.UpdateTime(GetRemainingTime());

        // Check for game end
        CheckGameEnd();
    }

    private void FixedUpdate()
    {
        float deltaTime = Time.deltaTime;
        NpcManager.Move(deltaTime);
    }

    private void LateUpdate()
    {
        // Let the agents cast their visions
        NpcManager.CastVision();

        // Update the guards vision and apply the vision affects (seeing intruders,etc) 
        NpcManager.ProcessNpcsVision(SessionInfo);

        // Idle NPCs make decisions
        NpcManager.MakeDecisions(GetSessionInfo().gameType);
    }

    public Session GetSessionInfo()
    {
        return SessionInfo;
    }

    void CheckGameEnd()
    {
        bool timeOver = GetElapsedTime() >= Properties.EpisodeLength;
        bool highscoreReached = scoreController.score > 0f;
        bool spotted = NpcManager.GetState() is Chase;

        bool finished = timeOver || highscoreReached || spotted;

        // Log Guards progress
        performanceMonitor.Log();

        if (!finished) return;

        FinishArea();
    }


    public void FinishArea()
    {
        // End the episode
        performanceMonitor.FinalizeLogging(GameManager.Instance.loggingMethod);

        if (GameManager.Instance.showSurvey)
        {
            if (!Equals(GetSessionInfo().gameCode, "tutorial"))
                SessionsSetup.AddSessionColor(GetSessionInfo().guardColor);

            GameManager.SurveyManager.CreateSurvey(GameManager.GetRunId(), GetSessionInfo().surveyType,
                scoreController.score);
            GameManager.SurveyManager.ShowSurvey();
        }

        // End the episode for the ML agents
        EndEpisode();

        ResetArea();

        // Prevent the current area to be removed if it is a tutorial session
        if (Equals(SessionInfo.gameCode, "tutorial") && GameManager.Instance.showSurvey) return;

        EndArea();
    }

    // Destroy the area
    public void EndArea()
    {
        if (!GameManager.Instance.showSurvey && performanceMonitor.IsDone())
            GameManager.Instance.RemoveArea(gameObject);
    }

    int GetRemainingTime()
    {
        return Mathf.RoundToInt(Properties.EpisodeLength - GetElapsedTime());
    }
}