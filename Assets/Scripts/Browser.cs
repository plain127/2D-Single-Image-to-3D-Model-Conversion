using UnityEngine;
using System.Collections;
using System.IO;
using SimpleFileBrowser;
using System.Diagnostics;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System;

public class Browser : MonoBehaviour
{
    public ModelLoader modelLoader;

    public void OpenBrowser()
    {
        FileBrowser.SetFilters(true, new FileBrowser.Filter("Images", ".jpg", ".png"), new FileBrowser.Filter("Text Files", ".txt", ".pdf"));
        FileBrowser.SetDefaultFilter(".jpg");
        FileBrowser.SetExcludedExtensions(".lnk", ".tmp", ".zip", ".rar", ".exe");
        FileBrowser.AddQuickLink("Users", "C:\\Users", null);
        StartCoroutine(ShowLoadDialogCoroutine());
    }

    IEnumerator ShowLoadDialogCoroutine()
    {
        yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.FilesAndFolders, true, null, null, "Load Files and Folders", "Load");

        if (FileBrowser.Success)
        {
            UnityEngine.Debug.Log("success");
            byte[] bytes = FileBrowserHelpers.ReadBytesFromFile(FileBrowser.Result[0]);

            string destinationPath = Path.Combine(Application.persistentDataPath, FileBrowserHelpers.GetFilename(FileBrowser.Result[0]));
            FileBrowserHelpers.CopyFile(FileBrowser.Result[0], destinationPath);

            SendImagePathToPython(destinationPath);
        }
    }

    void SendImagePathToPython(string imagePath)
    {

        string pythonExePath = "./PythonLibs/myvenv/Scripts/python.exe";
        string pythonScriptPath = "./PythonLibs/detect_object.py";

        ProcessStartInfo startInfo = new ProcessStartInfo(pythonExePath);
        startInfo.Arguments = $"\"{pythonScriptPath}\" \"{imagePath}\"";
        startInfo.UseShellExecute = false;
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;
        startInfo.CreateNoWindow = true;

        Process process = new Process();
        process.StartInfo = startInfo;
        process.Start();

        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();

        if (string.IsNullOrEmpty(error))
        {
            string[] outputLines = output.Trim().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            string[] detectedClasses = outputLines[0].Trim().Split(',');
            string[] detectedCoordiates = outputLines[1].Trim().Split(',');
            var detectedObjects = new Dictionary<(string name, int count), (float x, float y)>();

            if (detectedClasses.Length == detectedCoordiates.Length)
            {
                for (int i = 0; i < detectedClasses.Length; i++)
                {
                    string detectedClass = detectedClasses[i].Trim();
                    string[] coordinates = detectedCoordiates[i].Trim().Split(' ');

                    if (coordinates.Length == 2 && float.TryParse(coordinates[0], out float x) && float.TryParse(coordinates[1], out float y))
                    {
                        int count = 0;
                        foreach (var key in detectedObjects.Keys)
                        {
                            if (key.name == detectedClass)
                            {
                                count = key.count;
                            }
                        }

                        foreach (var key in detectedObjects.Keys)
                        {
                            detectedObjects[(detectedClass, count + 1)] = (x, y);
                        }
                    }
                }
            }
            UnityEngine.Debug.Log("Calling LoadModels");
            modelLoader.LoadModels(detectedObjects);
        }
        else
        {
            UnityEngine.Debug.Log("failed");
            UnityEngine.Debug.Log(error);
        }
    }
}
