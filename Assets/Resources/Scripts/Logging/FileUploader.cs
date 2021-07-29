using UnityEngine;
using System.Collections;
using System.Text;
using UnityEngine.Networking;

public static class FileUploader
{
    // public static string server = "http://cgi64-1.cs.mcgill.ca/~walene/";
    // public static string server = "https://www.cs.mcgill.ca/~walene/";
    public static string server = "https://isavage.cs.mcgill.ca/";
    // public static string server = "http://localhost/stealth_simulator/";


    // Generate the file name
    private static string GetFileName(Session sessionInfo, int timeStamp, string fileType)
    {
        string fileName = SystemInfo.deviceUniqueIdentifier;
        fileName += "_";
        fileName += timeStamp;
        fileName += "_";
        fileName += fileType;
        fileName += "_";
        fileName += sessionInfo.map;
        fileName += "_";
        fileName += sessionInfo.GetGuardsData()[0].guardPlanner.Value.search;


        return fileName;
    }

    private static string GetFileName(int timeStamp, string fileType)
    {
        string fileName = SystemInfo.deviceUniqueIdentifier;
        fileName += "_";
        fileName += timeStamp;
        fileName += "_";
        fileName += fileType;

        return fileName;
    }


    private static WWWForm GetForm(byte[] content, string fileName, string fileType)
    {
        WWWForm form = new WWWForm();
        // Add data and meta data to the form
        form.AddField("action", "data upload");
        form.AddField("file", "file");
        form.AddBinaryData("file", content, fileName, fileType);

        return form;
    }

    // Upload game data
    public static IEnumerator UploadData(Session? sessionInfo, int timeStamp, string dataType, string fileType,
        string gameData)
    {
        // Converting the xml to bytes to be ready for upload
        byte[] content = Encoding.UTF8.GetBytes(gameData);

        // first the device identifier.
        string fileName = sessionInfo == null
            ? GetFileName(timeStamp, dataType)
            : GetFileName(sessionInfo.Value, timeStamp, dataType);

        // Get the WWW form
        WWWForm form = GetForm(content, fileName, fileType);

        // Perform the request and change the url to the url of the php file
        WWW w = new WWW(server + "upload_data.php", form);
        
        yield return w;
        
        // Check if there was an error in the upload process.
        if (w.error != null)
        {
            Debug.Log("Error");
            Debug.Log(w.error);
        }
        else
        {
            Debug.Log(w.text);
        }
    }


    public static IEnumerator GetFile(string fileName, string type, float scale)
    {
        string requestAddress = server + "get_map.php?name=" + fileName + "&type=" + type + "&size=" + scale;
        UnityWebRequest www = UnityWebRequest.Get(requestAddress);

        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.Log(www.error);
        }
        else
        {
            // Or retrieve results as binary data
            if (type == "map")
                GameManager.instance.currentMapData = www.downloadHandler.text;
            else
                GameManager.instance.currentRoadMapData = www.downloadHandler.text;
            
        }
    }
}