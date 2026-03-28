// TutorialManager.cs
// AI-ASSISTED: Generated with Cursor, reviewed by developer.
// Attach to: An empty GameObject called "TutorialManager" in Level_01 ONLY.
//
// TUTORIAL DESIGN:
// The tutorial is NOT a wall of text. It is a sequence of short, contextual
// prompts that appear at the exact moment they become relevant.
// Each prompt fades in, holds briefly, then fades out automatically.
// The player never has to press anything to dismiss — it gets out of their way.
//
// PROMPT SEQUENCE (in order):
//
//   Step 0 — "YOU ARE ARC."
//             Appears immediately on level start.
//             Subtitle: "Navigate the light."
//
//   Step 1 — "MOVE"  [WASD or arrow keys icon]
//             Appears 1.5s after start — player has had a moment to look around.
//
//   Step 2 — "JUMP"  [Space key icon]
//             Appears when player first moves horizontally (they found the controls).
//
//   Step 3 — "TAB"   "SWITCH CONTROL"
//             Appears when player gets within 4 tiles of the exit.
//             This is the key mechanic — we reveal it right before they need it.
//
//   Step 4 — "GUIDE ECHO NOW"
//             Appears automatically when ARC reaches the exit and control switches.
//             LevelManager already shows "NOW GUIDE ECHO" in the label —
//             this gives it a bigger, more atmospheric visual moment.
//
//   Step 5 — "BOTH MUST REACH THE END"
//             Appears when ECHO starts moving (player has figured out the switch).
//             Reinforces the core goal one final time.
//
// HOW TO SET UP IN UNITY:
// 1. Create empty GameObject → name it "TutorialManager" → attach this script
// 2. Inside Canvas, create a Panel child called "TutorialPanel":
//      - Anchor: bottom centre, Pos Y: 80 (sits above the dividing line)
//      - Width: 700, Height: 120
//      - Background: black at 60% alpha
//      - CanvasGroup component on TutorialPanel
// 3. Inside TutorialPanel, add:
//      - TextMeshPro "TutorialMainText" — large, centre (font size 44, bold)
//      - TextMeshPro "TutorialSubText"  — smaller, centre, below main (font size 22)
// 4. Drag ARC into the arcTransform slot
// 5. Drag ExitDoor_ARC into the arcExitTransform slot
// 6. Drag TutorialPanel into tutorialPanel slot
// 7. Drag both TMP text objects into their slots

using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;

public class TutorialManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform arcTransform;       // ARC GameObject
    [SerializeField] private Transform arcExitTransform;   // ExitDoor_ARC
    [SerializeField] private PlayerController arcController;      // ARC's PlayerController
    [SerializeField] private PlayerController echoController;     // ECHO's PlayerController

    [Header("UI")]
    [SerializeField] private GameObject tutorialPanel;      // The whole panel
    [SerializeField] private CanvasGroup panelCanvasGroup;   // On TutorialPanel
    [SerializeField] private TextMeshProUGUI mainText;
    [SerializeField] private TextMeshProUGUI subText;

    [Header("Timing")]
    [SerializeField] private float fadeInDuration = 0.4f;
    [SerializeField] private float holdDuration = 2.8f;
    [SerializeField] private float fadeOutDuration = 0.5f;
    [SerializeField] private float exitProximity = 4f;    // Tiles from exit to trigger Tab hint

    [Header("Colours")]
    [SerializeField] private Color arcColour = new Color(0.85f, 0.95f, 1.00f); // Cool white-blue
    [SerializeField] private Color echoColour = new Color(0.75f, 0.45f, 1.00f); // Neon purple
    [SerializeField] private Color keyColour = new Color(1.00f, 0.90f, 0.40f); // Warm yellow for key labels

    // ------------------------------------------------------------------
    // State machine
    // ------------------------------------------------------------------
    private enum TutorialStep
    {
        Welcome, Move, Jump, TabHint, EchoIntro, BothMustFinish, Done
    }

    private TutorialStep currentStep = TutorialStep.Welcome;
    private bool stepInProgress = false;
    private bool hasMoved = false;
    private bool hasJumped = false;
    private bool arcExitReached = false;
    private bool echoHasMoved = false;

    private Coroutine displayCoroutine;

    // ------------------------------------------------------------------
    private void Start()
    {
        if (tutorialPanel != null) tutorialPanel.SetActive(false);
        if (panelCanvasGroup != null) panelCanvasGroup.alpha = 0f;

        StartCoroutine(RunTutorial());
    }

    // ------------------------------------------------------------------
    // Main tutorial sequence coroutine
    // ------------------------------------------------------------------
    private IEnumerator RunTutorial()
    {
        // ── Step 0: Welcome ────────────────────────────────────────────
        yield return new WaitForSeconds(0.8f);
        yield return ShowPrompt(
            $"<color=#{ColorToHex(arcColour)}>YOU ARE ARC</color>",
            "Navigate the light. Guide Echo through the dark.",
            3.2f);

        // ── Step 1: Move ────────────────────────────────────────────────
        yield return new WaitForSeconds(0.5f);
        yield return ShowPrompt(
            $"<color=#{ColorToHex(keyColour)}>[A] [D]</color>  or  <color=#{ColorToHex(keyColour)}>[← →]</color>",
            "Move left and right.",
            2.5f);

        currentStep = TutorialStep.Move;

        // ── Step 2: Wait until player moves, then show Jump hint ───────
        yield return new WaitUntil(() => hasMoved);
        yield return new WaitForSeconds(0.4f);
        yield return ShowPrompt(
            $"<color=#{ColorToHex(keyColour)}>[SPACE]</color>",
            "Jump.",
            2.2f);

        currentStep = TutorialStep.Jump;

        // ── Step 3: Wait until player approaches exit, show Tab hint ───
        yield return new WaitUntil(() => arcTransform != null && arcExitTransform != null &&
            Vector2.Distance(arcTransform.position, arcExitTransform.position) < exitProximity);

        yield return new WaitForSeconds(0.2f);
        yield return ShowPrompt(
            $"<color=#{ColorToHex(keyColour)}>[TAB]</color>  —  SWITCH",
            "ARC and ECHO each control one half.\nSwitch between them to guide both home.",
            4.0f);

        currentStep = TutorialStep.TabHint;

        // ── Step 4: Wait until ARC exits (LevelManager switches to ECHO) ─
        yield return new WaitUntil(() => arcExitReached);
        yield return new WaitForSeconds(0.6f);

        yield return ShowPrompt(
            $"<color=#{ColorToHex(echoColour)}>NOW YOU ARE ECHO</color>",
            "Navigate the dark. ARC is waiting.",
            3.0f);

        currentStep = TutorialStep.EchoIntro;

        // ── Step 5: Wait until ECHO moves, confirm the goal ────────────
        yield return new WaitUntil(() => echoHasMoved);
        yield return new WaitForSeconds(0.5f);

        yield return ShowPrompt(
            "BOTH MUST REACH THE END",
            "Neither can finish without the other.",
            3.2f);

        currentStep = TutorialStep.Done;
    }

    // ------------------------------------------------------------------
    // Update — track player state for trigger conditions
    // ------------------------------------------------------------------
    private void Update()
    {
        if (currentStep == TutorialStep.Done) return;

        // Track if ARC has moved
        if (!hasMoved && arcController != null && arcController.IsActive())
        {
            // Check if ARC is actually moving (velocity > small threshold)
            Rigidbody2D rb = arcController.GetComponent<Rigidbody2D>();
            if (rb != null && Mathf.Abs(rb.linearVelocity.x) > 0.5f)
                hasMoved = true;
        }

        // Track if ECHO has moved (after the switch)
        if (!echoHasMoved && echoController != null && echoController.IsActive())
        {
            Rigidbody2D rb = echoController.GetComponent<Rigidbody2D>();
            if (rb != null && Mathf.Abs(rb.linearVelocity.x) > 0.5f)
                echoHasMoved = true;
        }
    }

    // ------------------------------------------------------------------
    // Called by LevelManager/ExitDoor when ARC reaches their exit
    // ------------------------------------------------------------------
    public void NotifyARCExitReached()
    {
        arcExitReached = true;
    }

    // ------------------------------------------------------------------
    // Display a prompt — fade in, hold, fade out
    // ------------------------------------------------------------------
    private IEnumerator ShowPrompt(string main, string sub, float holdTime)
    {
        if (tutorialPanel == null || panelCanvasGroup == null) yield break;

        stepInProgress = true;
        tutorialPanel.SetActive(true);

        if (mainText != null) mainText.text = main;
        if (subText != null) subText.text = sub;

        // Fade in
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            panelCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
            yield return null;
        }
        panelCanvasGroup.alpha = 1f;

        // Hold
        yield return new WaitForSeconds(holdTime);

        // Fade out
        elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            panelCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeOutDuration);
            yield return null;
        }
        panelCanvasGroup.alpha = 0f;
        tutorialPanel.SetActive(false);

        stepInProgress = false;
    }

    // ------------------------------------------------------------------
    // Helper — converts Color to hex string for TMP rich text
    // ------------------------------------------------------------------
    private string ColorToHex(Color colour)
    {
        return ColorUtility.ToHtmlStringRGB(colour);
    }
}