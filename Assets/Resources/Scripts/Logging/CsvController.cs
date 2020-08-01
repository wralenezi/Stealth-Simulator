using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

// CSV Handler 
public static class CsvController
{
    
    
    public static string GetPath(string planner, float mapScale, string worldRep, string mapName, int resetThreshold,
        string dataLevel)
    {
        return Properties.logFilesPath + planner + "_" + mapScale + "_" + worldRep + "_" + mapName + "_" + resetThreshold + "_" +
               dataLevel + ".csv";
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

    
    public static int GetFileLength(string gameCode, float mapScale, string worldRep, string mapName, int resetThreshold,
        string dataLevel)
    {
        string path = GetPath(gameCode, mapScale, worldRep, mapName, resetThreshold, dataLevel);

        FileInfo file = new FileInfo(path);

        if (file.Exists)
            return File.ReadLines(path).Count() - 1;
        else
            return 0;
    }
    
}
