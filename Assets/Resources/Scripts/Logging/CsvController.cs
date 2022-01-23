using System;
using System.Data;
using System.IO;
using UnityEngine;

// CSV Handler 
public static class CsvController
{
    public static string GetPath(Session sa, FileType fileType, int episodesCount)
    {
        return GameManager.LogsPath + GetFileName(fileType, sa) + " " + episodesCount + ".csv";
    }

    // Get the number of files that starts with a certain string
    public static int ReadFileStartWith(FileType fileType, Session sa)
    {
        int episodesCount = 0;

        string[] allFiles = Directory.GetFiles(GameManager.LogsPath);

        string fileName = GameManager.LogsPath + GetFileName(fileType, sa);

        foreach (var file in allFiles)
        {
            if (file.StartsWith(fileName) && !file.EndsWith(".meta"))
                episodesCount++;
        }

        return episodesCount;
    }

    public static DataTable ConvertCSVtoDataTable(string strFilePath)
    {
        if (GameManager.Instance.IsOnlineBuild) return GetOnlineDialogs();

        DataTable dt = new DataTable();
        using StreamReader sr = new StreamReader(strFilePath);

        string[] headers = sr.ReadLine().Split(',');

        foreach (string header in headers)
        {
            dt.Columns.Add(header);
        }

        while (!sr.EndOfStream)
        {
            string[] rows = sr.ReadLine().Split(',');
            DataRow dr = dt.NewRow();
            for (int i = 0; i < headers.Length; i++)
            {
                dr[i] = rows[i];
            }

            dt.Rows.Add(dr);
        }


        return dt;
    }


    public static DataTable GetOnlineDialogs()
    {
        // Split data by lines
        string[] lines = GameManager.DialogLines.Split('\n');

        char sep = ',';

        DataTable dt = new DataTable();

        for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            string[] row = lines[lineIndex].Split(sep);

            if (row.Length == 0) continue;

            if (lineIndex == 0)
            {
                foreach (var cell in row)
                {
                    dt.Columns.Add(cell);
                }
            }
            else
            {
                DataRow dr = dt.NewRow();

                for (int i = 0; i < dt.Columns.Count; i++)
                {
                    dr[i] = row[i];
                }

                dt.Rows.Add(dr);
            }
        }

        return dt;
    }

    private static string GetFileName(FileType fileType, Session sa)
    {
        return fileType + " " + sa;
    }


    // Write the data on a csv file
    public static void WriteString(string path, string data, bool isOverwrite)
    {
        //Write some text to the file
        StreamWriter writer = new StreamWriter(path, isOverwrite);
        writer.Write(data);
        writer.Close();
    }


    // Return the content of a CSV
    public static string ReadString(string path)
    {
        try
        {
            //Read the text from directly from the test.txt file
            StreamReader reader = new StreamReader(path);
            string data = reader.ReadToEnd();
            reader.Close();

            return data;
        }
        catch (Exception e)
        {
            Debug.LogError(path + " not found");
            return "";
        }
    }


    public static int ReadEpisodesCount(string path)
    {
        int episodesCount = 0;

        if (File.Exists(path))
        {
            //Read the text from directly from the test.txt file
            StreamReader reader = new StreamReader(path);

            string data = reader.ReadToEnd();

            reader.Close();

            int.TryParse(data, out episodesCount);
        }


        return episodesCount;
    }
}

public enum FileType
{
    Performance,

    Npcs,
    
    User,

    Survey
}