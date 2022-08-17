using UnityEngine;
using System.Collections;
using System.Linq;
using System.Net;
using System.Text;
using UnityEngine.Networking;

public static class FileUploader
{
    // public static string server = "http://cgi64-1.cs.mcgill.ca/~walene/";
    // public static string server = "https://www.cs.mcgill.ca/~walene/";
    // public static string server = "https://isavage.cs.mcgill.ca/";
    public static string server = "http://localhost/isavage/";


    // Generate the file name
    private static string GetFileName(Session? sessionInfo, FileType fileType)
    {
        string sep = "_";

        string fileName = "";
        fileName += GameManager.GetRunId();
        fileName += sep;
        fileName += fileType;

        if (!Equals(sessionInfo, null))
        {
            fileName += sep;
            fileName += sessionInfo;
        }

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
    public static IEnumerator UploadData(Session? sessionInfo, FileType file, string fileType,
        string gameData)
    {
        // Converting the xml to bytes to be ready for upload
        byte[] content = Encoding.UTF8.GetBytes(gameData);

        // first the device identifier.
        // Determine if the file is a survey file or game data logs
        string fileName = GetFileName(sessionInfo, file);

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
    }


    public static IEnumerator UploadScore(Session? sessionInfo, float score)
    {
        string requestAddress = server + "get_scores.php?behavior=" + sessionInfo.sessionVariable + "&name=" +
                                GameManager.playerName + "&score=" + score;

        UnityWebRequest www = UnityWebRequest.Get(requestAddress);

        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success || www.responseCode != 200)
        {
            string error = ResponseCodeLookUp.GetMeaning(www.responseCode);
            Debug.LogError("Error with sending score ");
        }
        else
            sessionInfo.LoadScores(www.downloadHandler.text);

        // PatrolUserStudy.LoadScores(sessionInfo, www.downloadHandler.text);
    }


    public static IEnumerator GetFile(string fileName, string type, float scale = 0f)
    {
        string requestAddress = server + "get_map.php?name=" + fileName + "&type=" + type + "&size=" + scale;

        UnityWebRequest www = UnityWebRequest.Get(requestAddress);

        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success || www.responseCode != 200)
        {
            string error = ResponseCodeLookUp.GetMeaning(www.responseCode);
            Debug.LogError("Error with requesting " + type + " - " + www.responseCode + " - Map:" + fileName +
                           " - MapScale:" + scale + " - " + error);
        }
        else
        {
            // Or retrieve results as binary data
            if (type == "map")
                GameManager.Instance.currentMapData = www.downloadHandler.text;
            else if (type == "roadMap")
                GameManager.Instance.currentRoadMapData = www.downloadHandler.text;
            else if (type == "dialogs")
                GameManager.DialogLines = www.downloadHandler.text;
        }
    }
}