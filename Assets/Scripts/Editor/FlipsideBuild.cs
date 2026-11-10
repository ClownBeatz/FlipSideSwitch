// FlipsideBuild.cs (Editor only)
// Menu: Flipside > Build Windows
// Builds every enabled scene in Build Settings to Builds/Windows/FLIPSIDE.exe.
// Same result as File > Build Profiles > Windows > Build, just one click.
//
// Menu: Flipside > Reset Level Select Progress
// Clears the CLEARED marks shown on the level select screen (Editor play mode only).

using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class FlipsideBuild
{
    private const string OutputPath = "Builds/Windows/FLIPSIDE.exe";

    [MenuItem("Flipside/Build Windows")]
    public static void BuildWindows()
    {
        string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));

        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = OutputPath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"FLIPSIDE build succeeded: {Path.GetFullPath(OutputPath)} ({summary.totalSize / (1024 * 1024)} MB)");
            if (!Application.isBatchMode) EditorUtility.RevealInFinder(OutputPath);
        }
        else
        {
            Debug.LogError($"FLIPSIDE build failed: {summary.result}, {summary.totalErrors} errors");
            if (Application.isBatchMode) EditorApplication.Exit(1);
        }
    }

    [MenuItem("Flipside/Reset Level Select Progress")]
    public static void ResetProgress()
    {
        for (int i = 1; i <= 7; i++)
            PlayerPrefs.DeleteKey($"{LevelCompleteUI.CompletedKeyPrefix}Level_{i:00}");
        PlayerPrefs.Save();
        Debug.Log("FLIPSIDE: level select progress cleared.");
    }
}
