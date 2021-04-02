using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

// CSV Handler 
public static class CsvController
{
    public static string GetPath(Session sa, int episodesCount)
    {
        return GameManager.LogsPath + GetFileName(sa) + " " + episodesCount + ".csv";
    }

    // Get the number of files that starts with a certain string
    public static int ReadFileStartWith(Session sa)
    {
        int episodesCount = 0;

        string[] allFiles = Directory.GetFiles(GameManager.LogsPath);

        string fileName = GameManager.LogsPath + GetFileName(sa);

        foreach (var file in allFiles)
        {
            if (file.StartsWith(fileName) && !file.EndsWith(".meta"))
                episodesCount++;
        }

        return episodesCount;
    }


    private static string GetFileName(Session sa)
    {
        return sa.ToString();
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
        //Read the text from directly from the test.txt file
        StreamReader reader = new StreamReader(path);

        string data = reader.ReadToEnd();
        reader.Close();

        return data;
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


    // public static int GetFileLength(string gameCode, float mapScale, string worldRep, string mapName,
    //     int resetThreshold,
    //     string dataLevel)
    // {
    //     string path = GetPath(gameCode, mapScale, worldRep, mapName, resetThreshold, dataLevel);
    //
    //     FileInfo file = new FileInfo(path);
    //
    //     if (file.Exists)
    //         return File.ReadLines(path).Count() - 1;
    //     else
    //         return 0;
    // }
}