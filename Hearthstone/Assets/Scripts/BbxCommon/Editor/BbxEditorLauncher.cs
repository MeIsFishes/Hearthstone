#if UNITY_EDITOR
using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BbxCommon.Editor
{
    internal static class BbxEditorLauncher
    {
        private const string RelativeExecutablePath = "../../Tools/BbxEditor/BbxEditor.exe";

        [MenuItem("BbxCommon/BbxEditor")]
        private static void Open()
        {
            var executablePath = Path.GetFullPath(Path.Combine(Application.dataPath, RelativeExecutablePath));
            if (!File.Exists(executablePath))
            {
                EditorUtility.DisplayDialog(
                    "BbxEditor Not Found",
                    "Could not find BbxEditor at:\n" + executablePath,
                    "OK");
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = executablePath,
                    WorkingDirectory = Path.GetDirectoryName(executablePath),
                    UseShellExecute = true,
                });
            }
            catch (Exception exception)
            {
                UnityEngine.Debug.LogException(exception);
                EditorUtility.DisplayDialog(
                    "Could Not Open BbxEditor",
                    exception.Message,
                    "OK");
            }
        }
    }
}
#endif
