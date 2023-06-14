using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    private Menu _currentMenu;
    private List<MenuOption> _choices;
    
    private float m_FadeInSpeed = -0.05f;
    private Image m_FadeInScreen;

    
    public void Start()
    {
        _currentMenu = gameObject.AddComponent<Menu>();
        _currentMenu.Initiate();
        
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

        
        DontDestroyOnLoad(transform.parent.gameObject);
        CreateMainMenu();
        ShowSurvey();
        // SceneManager.LoadScene("Main");
    }
    
    public void ShowSurvey()
    {
        StartCoroutine(FadeInSurvey());
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
        _choices.Clear();
        _choices.Add(new MenuOption("start", "start"));
        _currentMenu.AddRadioButtons("start", "", "Click on start", _choices);
    }
}

public struct MenuOption
{
    // Value of the choice
    public string value;

    // Label of the choice (visible for user; like in a button)
    public string label;

    public Color color;

    public MenuOption(string _value, string _label, Color _color = default)
    {
        value = _value;
        label = _label;
        color = _color;
    }
}