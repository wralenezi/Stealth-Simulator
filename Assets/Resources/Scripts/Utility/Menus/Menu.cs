using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

public class Menu : MonoBehaviour
{
    private string _menuItemPath = "Prefabs/UIs/MenuItem";
    private GameObject _menuItemPrefab;
    
    private List<MenuItem> _items;

    private int currentMenuIndex;

    public void Initiate()
    {
        _menuItemPrefab = (GameObject) Resources.Load(_menuItemPath);
        _items = new List<MenuItem>();
    }
    
    public void StartSurvey()
    {
        if (_items.Count > 0)
        {
            _items[0].gameObject.SetActive(true);
        }
    }

    public void AddRadioButtons(string id, string code, string question, List<MenuOption> choices)
    {
        // Create the item object and hide it
        GameObject menuItemGo = Instantiate(_menuItemPrefab, transform);
        menuItemGo.SetActive(false);

        MenuRadioButtonItem menuMultiple = menuItemGo.AddComponent<MenuRadioButtonItem>();
        menuMultiple.Initiate(id, this, code);

        // Add the question
        menuMultiple.SetQuestion(question);

        // Add the options
        foreach (var choice in choices)
            menuMultiple.AddChoice(choice);

        _items.Add(menuMultiple);
    }

    
    public void NextItem()
    {
        currentMenuIndex++;

        if (currentMenuIndex < _items.Count)
            ActiveItem();
        // else
        //     EndSurvey();
    }
    
    private void ActiveItem()
    {
        _items[currentMenuIndex].gameObject.SetActive(true);

        if (Equals(_items[currentMenuIndex].name, "End"))
        {
            string surveyJson = JsonConvert.SerializeObject(this);

        }
        
    }
}
