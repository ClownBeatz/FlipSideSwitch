// SplitScreenSetup.cs
// AI-ASSISTED: Generated with Cursor, adjusted manually by developer.
// Attach to: An empty GameObject called "SplitScreenManager" in your scene.
//
// Sets up two cameras to each render half the screen:
//   Camera ARC  = top half
//   Camera ECHO = bottom half
//
// HOW TO SET UP:
// 1. Create two Cameras: name them "CameraARC" and "CameraECHO"
// 2. Create this SplitScreenManager GameObject and attach this script
// 3. Drag each camera into its slot in the Inspector
// 4. Optionally drag the DividingLine UI Image into its slot
// 5. Press Play — viewports are set automatically

using UnityEngine;

public class SplitScreenSetup : MonoBehaviour
{
    [Header("Cameras")]
    [SerializeField] private Camera cameraARC;
    [SerializeField] private Camera cameraECHO;

    [Header("Dividing Line (optional — UI Image at screen centre)")]
    [SerializeField] private GameObject dividingLine;

    private void Awake()
    {
        SetupViewports();
    }

    private void SetupViewports()
    {
        if (cameraARC == null || cameraECHO == null)
        {
            Debug.LogError("SplitScreenSetup: Assign both cameras in the Inspector!");
            return;
        }

        // Top half — ARC's world
        // Rect(x, y, width, height) all 0-to-1 (fraction of screen)
        cameraARC.rect = new Rect(0f, 0.5f, 1f, 0.5f);
        cameraARC.depth = 1;    // Higher depth = drawn on top

        // Bottom half — ECHO's world
        cameraECHO.rect = new Rect(0f, 0f, 1f, 0.5f);
        cameraECHO.depth = 0;

        // Show the dividing line now that cameras are placed
        if (dividingLine != null)
            dividingLine.SetActive(true);

        Debug.Log("SplitScreenSetup: Split-screen viewports configured.");
    }

    // Call this if you ever want to return to full-screen (title screen, cutscene, etc.)
    public void SetFullScreen(Camera cam)
    {
        if (cam != null) cam.rect = new Rect(0f, 0f, 1f, 1f);
    }
}