using UnityEngine;
using System.Xml;
using System.IO;
using System.Collections;
using System.Text;

public static class FileUploader
{
    public static string server = "http://127.0.0.1/";

    public static IEnumerator UploadLevel(Session sessionInfo, string gameData)
    {
        //converting the xml to bytes to be ready for upload
        byte[] data = Encoding.UTF8.GetBytes(gameData);

        // Generate the file name
        // first the device identifier.
        string fileName = SystemInfo.deviceUniqueIdentifier;
        fileName += "_";
        fileName += sessionInfo.gameCode;
        fileName += "_";
        fileName += sessionInfo.map;
        fileName += "_";
        fileName += sessionInfo.mapScale;
        fileName += "_";
        fileName += sessionInfo.worldRepType;

        WWWForm form = new WWWForm();
        // Add data and meta data to the form
        form.AddField("action", "data upload");
        form.AddField("file", "file");
        form.AddBinaryData("file", data, fileName, "text/csv");

        //change the url to the url of the php file
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
            //this part validates the upload, by waiting 5 seconds then trying to retrieve it from the web
            if (w.uploadProgress == 1 && w.isDone)
            {
                yield return new WaitForSeconds(5);

                //change the url to the url of the folder you want it the levels to be stored, the one you specified in the php file
                WWW w2 = new WWW(server + "data/" + fileName + "_" + w.text + ".csv");
                yield return w2;

                if (w2.error != null)
                {
                    Debug.Log("Error");
                    Debug.Log(w2.error);
                }
                else
                {
                    //then if the retrieval was successful, validate its content to ensure the level file integrity is intact
                    if (w2.text != null && w2.text != "")
                    {
                        //and finally announce that everything went well
                        // Debug.Log("File validated!");
                    }
                    else
                    {
                        Debug.Log("Level File " + fileName + " is Empty");
                    }
                }
            }
        }
    }
}