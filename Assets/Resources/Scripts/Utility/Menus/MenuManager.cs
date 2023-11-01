using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    private Menu _currentMenu;

    private SessionSetup _setup;

    // Parameters
    private float m_FadeInSpeed = -0.05f;
    private Image m_FadeInScreen;

    // Temp
    private List<MenuOption> _choices;

    public static MenuManager Instance;

    public void Start()
    {
        Instance = this;
        _currentMenu = gameObject.AddComponent<Menu>();
        _currentMenu.Initiate();

        _setup = gameObject.AddComponent<SessionSetup>();

        _choices = new List<MenuOption>();
        GameObject fadeInGameObject = new GameObject();
        fadeInGameObject.name = "FadeInScreen";
        fadeInGameObject.transform.parent = transform.parent;
        fadeInGameObject.transform.localPosition = Vector3.zero;
        m_FadeInScreen = fadeInGameObject.AddComponent<Image>();
        fadeInGameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(
            Camera.main.orthographicSize * 300f, Camera.main.orthographicSize * 150f);
        Color bKColor = Camera.main.backgroundColor;
        bKColor.a = 0f;
        m_FadeInScreen.color = bKColor;
        
        Debug.Log("Start");
        
        DontDestroyOnLoad(transform.parent.gameObject);
        CreateMainMenu();
        ShowSurvey();
    }

    public void ShowSurvey()
    {
        StartCoroutine(FadeInSurvey());
    }

    public void ShowEndGame()
    {
        EndGame();
        ShowSurvey();
    }

    private IEnumerator FadeInSurvey()
    {
        float alpha = 1f;
        Color bKColor = m_FadeInScreen.color;
        bKColor.a = alpha;
        m_FadeInScreen.color = bKColor;
        _currentMenu.StartSurvey();

        while (alpha > 0f)
        {
            yield return new WaitForSecondsRealtime(0.01f);
            alpha += m_FadeInSpeed;
            bKColor.a = alpha;
            m_FadeInScreen.color = bKColor;
        }

        m_FadeInScreen.gameObject.SetActive(false);
    }


    private void CreateMainMenu()
    {
        _currentMenu.Reset();
        _choices.Clear();
        _choices.Add(new MenuOption("start", "start", delegate() { }, ""));
        _currentMenu.AddRadioButtons("start", "", "Stealth Simulator", _choices);

        _choices.Clear();
        _choices.Add(new MenuOption("RoadMap", "RoadMap", delegate()
        {
            _setup.AssignPatrolBehavior(new RoadMapPatrolerParams(1f, 1f, 1f, 0.5f, RMDecision.DijkstraPath,
                RMPassingGuardsSenstivity.Max, 0f, 0f, 0f));
        }, ""));
        _choices.Add(new MenuOption("VisMesh", "VisMesh", delegate()
        {
            _setup.AssignPatrolBehavior(new VisMeshPatrolerParams(0.9f, 1f, 1f,
                1f, 1f, VMDecision.Weighted));
        }, ""));
        _choices.Add(new MenuOption("Random", "Random",
            delegate() { _setup.AssignPatrolBehavior(new RandomPatrolerParams()); }, ""));
        _currentMenu.AddRadioButtons("patrol", "", "Choose the patrol behavior of the guards.", _choices);

        _choices.Clear();
        _choices.Add(new MenuOption("RoadMap", "RoadMap", delegate
        {
            _setup.AssignSearchBehavior(new RoadMapSearcherParams(1f, 1f, 0.5f, 0f, RMDecision.DijkstraPath,
                RMPassingGuardsSenstivity.Max, 0f, 0.5f, 0.5f, ProbabilityFlowMethod.Propagation));
        }, ""));
        _choices.Add(new MenuOption("Cheating", "Cheating",
            delegate { _setup.AssignSearchBehavior(new CheatingSearcherParams()); }, ""));
        _choices.Add(new MenuOption("Random", "Random",
            delegate { _setup.AssignSearchBehavior(new RandomSearcherParams()); }, ""));
        _currentMenu.AddRadioButtons("search", "", "Choose the search behavior of the guards.", _choices);

        _choices.Clear();
        _choices.Add(new MenuOption("MgsDock", "",
            () => { _setup.SetMapGuards(new MapData("MgsDock", 2f), 3); }, "MgsDock"));
        _choices.Add(new MenuOption("AlienIsolation", "",
            () => { _setup.SetMapGuards(new MapData("AlienIsolation", 3f), 3); }, "AlienIsolation"));
        _choices.Add(new MenuOption("AmongUs", "",
            () => { _setup.SetMapGuards(new MapData("AmongUs",0.5f), 4); }, "AmongUs"));
        _choices.Add(new MenuOption("Warehouse", "",
            () => { _setup.SetMapGuards(new MapData("Boxes", 1f), 6); }, "Warehouse"));
        _currentMenu.AddRadioButtons("map", "", "Choose the game level layout.", _choices);

        _choices.Clear();
        _choices.Add(new MenuOption("start", "Start", () => { SceneManager.LoadScene("Main"); }, ""));
        _currentMenu.AddRadioButtons("ready", "", "Ready to play?", _choices);
    }

    public void EndGame()
    {
        _currentMenu.Reset();
        _choices.Clear();
        _choices.Add(new MenuOption("end", "Play Again", delegate()
        {
            Destroy(transform.parent.gameObject);
            SceneManager.LoadScene("MainMenu");
        }, ""));
        _currentMenu.AddRadioButtons("start", "", "You scored " + ScoreController.Instance.Score, _choices);
    }
}

public struct MenuOption
{
    // Value of the choice
    public string value;

    // Label of the choice (visible for user; like in a button)
    public string label;

    public Color color;

    public string imagePath;

    public delegate void Chosen();

    public Chosen onClick;

    public MenuOption(string _value, string _label, Chosen _onClick, string _imagePath, Color _color = default)
    {
        value = _value;
        label = _label;
        color = _color;
        imagePath = _imagePath;
        onClick = _onClick;
    }
}