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
    private static string GetFileName(Session sessionInfo, string fileType)
    {
        string sep = "_";

        string fileName= "";//GetLocalIPv4();//SystemInfo.deviceUniqueIdentifier;
        // fileName += sep;
        fileName += GameManager.GetRunId();
        fileName += sep;
        fileName += fileType;
        fileName += sep;
        fileName += sessionInfo.map;
        fileName += sep;
        fileName += sessionInfo.GetGuardsData()[0].guardPlanner.Value.search;
        fileName += sep;
        fileName += sessionInfo.isDialogEnabled?"dialogEnabled":"dialogDisabled";
        fileName += sep;
        fileName += sessionInfo.gameCode;
        fileName += sep;
        fileName += sessionInfo.id;



        return fileName;
    }

    private static string GetFileName(string fileType)
    {
        string sep = "_";

        string fileName = "";//GetLocalIPv4();//SystemInfo.deviceUniqueIdentifier;
        // fileName += sep;
        fileName += GameManager.GetRunId();
        fileName += sep;
        fileName += fileType;

        return fileName;
    }

    // private static string GetLocalIPv4()
    // {
    //     return Dns.GetHostEntry(Dns.GetHostName())
    //         .AddressList.First(
    //             f => f.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
    //         .ToString();
    // }    

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
    public static IEnumerator UploadData(Session? sessionInfo, string dataType, string fileType,
        string gameData)
    {
        // Converting the xml to bytes to be ready for upload
        byte[] content = Encoding.UTF8.GetBytes(gameData);

        // first the device identifier.
        // Determine if the file is a survey file or game data logs
        string fileName = sessionInfo == null
            ? GetFileName(dataType)
            : GetFileName(sessionInfo.Value, dataType);

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


    public static IEnumerator GetFile(string fileName, string type, float scale = 0f)
    {
        string requestAddress = server + "get_map.php?name=" + fileName + "&type=" + type + "&size=" + scale;

        UnityWebRequest www = UnityWebRequest.Get(requestAddress);

        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success || www.responseCode != 200)
        {
            string error = ResponseCodeLookUp.GetMeaning(www.responseCode);
            Debug.LogError(www.responseCode + " - Map:" + fileName + " - MapScale:" + scale + " - " + error);
        }
        else
        {
            // Or retrieve results as binary data
            if (type == "map")
                GameManager.Instance.currentMapData = www.downloadHandler.text;
            else if(type == "roadMap")
                GameManager.Instance.currentRoadMapData = www.downloadHandler.text;
            else if(type == "dialogs")
                GameManager.DialogLines = www.downloadHandler.text;
        }
    }
}