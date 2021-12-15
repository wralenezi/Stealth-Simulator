using System;
using System.Collections.Generic;
using System.Data;
using UnityEngine;
using Random = UnityEngine.Random;

public class LineLookUp
{
    // Lines associated with the dialog line
    private static Dictionary<string, Lines> _lineLookUp;

    // Rules associated with the dialog line
    private static Dictionary<string, Rules> _ruleLookUp;

    // Responses associated with the dialog line
    private static Dictionary<string, Responses> _rspnsLookUp;

    // the dialog priority
    private static Dictionary<string, int> _priorityLookUp;

    // Dialog groups to choose among for playing lines
    private static Dictionary<string, DialogGroup> _dialogs;

    public static void Initiate()
    {
        string path = Application.dataPath + "/" + "dialog_lines.csv";

        DataTable data = CsvController.ConvertCSVtoDataTable(path);

        _lineLookUp = new Dictionary<string, Lines>();
        _ruleLookUp = new Dictionary<string, Rules>();
        _rspnsLookUp = new Dictionary<string, Responses>();
        _priorityLookUp = new Dictionary<string, int>();
        _dialogs = new Dictionary<string, DialogGroup>();
        
        for (int i = 0; i < data.Rows.Count; i++)
        {
            DataRow row = data.Rows[i];
            AddLines(row["dialogId"].ToString(), row["lines"].ToString());
            AddRule(row["dialogId"].ToString(), row["rules"].ToString());
            AddResponses(row["dialogId"].ToString(), row["responses"].ToString());
            AddDialogGroup(row["type"].ToString(), row["dialogId"].ToString());
            _priorityLookUp.Add(row["dialogId"].ToString(), int.Parse(row["priority"].ToString()));
        }
    }

    private static void AddDialogGroup(string dialogType, string dialogId)
    {
        bool isGroupExist = _dialogs.TryGetValue(dialogType, out DialogGroup dialogGroup);

        if (Equals(dialogType, "")) return;

        if (!isGroupExist)
        {
            dialogGroup = new DialogGroup();
            _dialogs.Add(dialogType, dialogGroup);
        }

        dialogGroup.AddLineId(dialogId);
    }

    private static void AddLines(string dialogId, string linesData)
    {
        Lines lines = new Lines(linesData);
        _lineLookUp.Add(dialogId, lines);
    }

    private static void AddRule(string dialogId, string ruleData)
    {
        Rules rules = new Rules(ruleData);
        _ruleLookUp.Add(dialogId, rules);
    }

    private static void AddResponses(string dialogId, string responseData)
    {
        Responses responses = new Responses(responseData);
        _rspnsLookUp.Add(dialogId, responses);
    }

    public static string GetLineForDialog(string dialogId)
    {
        try
        {
            return _lineLookUp[dialogId].GetLine();
        }
        catch (Exception e)
        {
            Debug.LogError(dialogId + " - Error");
            throw;
        }
    }

    public static string GetResponseForDialog(string dialogId)
    {
        return _rspnsLookUp[dialogId].GetResponse();
    }

    // Check if a dialog line has possible responses
    public static bool IsDlgHasRspns(string dialogId)
    {
        return _rspnsLookUp[dialogId].IsResponseAvailable();
    }

    public static int GetPriority(string dialogId)
    {
        return _priorityLookUp[dialogId];
    }

    public static Rules GetRuleSet(string dialogId)
    {
        return _ruleLookUp[dialogId];
    }

    public static int GetRuleCount(string dialogId)
    {
        return GetRuleSet(dialogId).GetRuleCount();
    }

    public static DialogGroup GetDialogGroup(string dialogType)
    {
        try
        {
            return _dialogs[dialogType];
        }
        catch (Exception e)
        {
            Debug.Log("There are no dialogs of type: " + dialogType);
            return null;
        }
    }
}

// The list of rules for a dialog line
public class Rules
{
    private List<string> m_Rules;

    public Rules(string _rules)
    {
        FillRules(_rules);
    }


    private void FillRules(string _rules)
    {
        m_Rules = new List<string>();
        foreach (var rule in _rules.Split('+'))
            m_Rules.Add(rule);
    }

    public List<string> GetRules()
    {
        return m_Rules;
    }

    public int GetRuleCount()
    {
        return m_Rules.Count;
    }
}


// The list of lines to say
public class Lines
{
    private int index;
    private List<string> m_Lines;

    public Lines(string linesData)
    {
        FillLines(linesData);
        index = Random.Range(0, m_Lines.Count);
    }

    // Shuffle the lines to prevent repetition
    private void ShuffleLines()
    {
        int n = m_Lines.Count;

        while (n > 1)
        {
            n--;
            int k = Random.Range(0, n + 1);
            string value = m_Lines[k];
            m_Lines[k] = m_Lines[n];
            m_Lines[n] = value;
        }
    }

    private void FillLines(string _line)
    {
        m_Lines = new List<string>();

        foreach (var line in _line.Split('-'))
            m_Lines.Add(line);
    }

    // Get a line
    public string GetLine()
    {
        if (m_Lines.Count == 0)
            return "";

        if (m_Lines.Count == 1)
            return m_Lines[0];

        string line = "";

        if (index < m_Lines.Count)
        {
            line = m_Lines[index++];
            return line;
        }

        line = m_Lines[m_Lines.Count - 1];
        index = 0;

        do
        {
            ShuffleLines();
        } while (line == m_Lines[index]);

        return line;
    }
}

// The list of possible responses
public class Responses
{
    private int index;
    private List<string> m_Responses;

    public Responses(string linesData)
    {
        FillResponses(linesData);
        index = Random.Range(0, m_Responses.Count);
    }

    private void FillResponses(string _line)
    {
        m_Responses = new List<string>();

        foreach (var line in _line.Split('+'))
            m_Responses.Add(line);
    }

    // Shuffle the lines to prevent repetition
    private void ShuffleResponses()
    {
        int n = m_Responses.Count;

        while (n > 1)
        {
            n--;
            int k = Random.Range(0, n + 1);
            string value = m_Responses[k];
            m_Responses[k] = m_Responses[n];
            m_Responses[n] = value;
        }
    }

    // Get a response
    public string GetResponse()
    {
        if (!IsResponseAvailable())
            return "";

        if (m_Responses.Count == 1)
            return m_Responses[0];

        string response = "";

        if (index < m_Responses.Count)
        {
            response = m_Responses[index++];
            return response;
        }

        response = m_Responses[m_Responses.Count - 1];
        index = 0;

        do
        {
            ShuffleResponses();
        } while (response == m_Responses[index]);

        return response;
    }

    public bool IsResponseAvailable()
    {
        return m_Responses.Count > 0 && m_Responses[0] != "";
    }
}