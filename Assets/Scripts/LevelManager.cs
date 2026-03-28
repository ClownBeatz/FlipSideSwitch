// LevelManager.cs
// AI-ASSISTED: Generated with Cursor, reviewed by developer.
// Attach to: An empty GameObject called "LevelManager" in every game scene.
// Singleton — access anywhere via LevelManager.Instance
//
// CHANGE: Door activation messages now use UIManager.SetLevelLabel which
//         triggers the label scale-punch (satisfying visual pop).
//         Everything else is unchanged.

using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    [Header("UI")]
    [SerializeField] private LevelCompleteUI levelCompleteUI;

    // ------------------------------------------------------------------ //
    // State
    // ------------------------------------------------------------------ //
    private bool arcExitReached = false;
    private bool echoExitReached = false;
    private bool levelEnded = false;

    // ------------------------------------------------------------------ //
    // Called by ExitDoor when a character reaches their exit
    // ------------------------------------------------------------------ //
    public void DoorActivated(ExitDoor.DoorOwner owner)
    {
        if (levelEnded) return;

        if (owner == ExitDoor.DoorOwner.ARC)
        {
            arcExitReached = true;
            Debug.Log("ARC reached the exit.");

            if (!echoExitReached && CharacterSwitcher.Instance != null)
            {
                CharacterSwitcher.Instance.SwitchToECHO();

                // SetLevelLabel now triggers a scale-punch animation on the label
                if (UIManager.Instance != null)
                    UIManager.Instance.SetLevelLabel("NOW GUIDE ECHO");
            }
        }
        else if (owner == ExitDoor.DoorOwner.ECHO)
        {
            echoExitReached = true;
            Debug.Log("ECHO reached the exit.");

            if (!arcExitReached && CharacterSwitcher.Instance != null)
            {
                CharacterSwitcher.Instance.SwitchToARC();

                if (UIManager.Instance != null)
                    UIManager.Instance.SetLevelLabel("NOW GUIDE ARC");
            }
        }

        if (arcExitReached && echoExitReached)
            LevelComplete();
    }

    // ------------------------------------------------------------------ //
    // Level Complete
    // ------------------------------------------------------------------ //
    private void LevelComplete()
    {
        levelEnded = true;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(AudioManager.SFX.LevelComplete);

        if (LevelTimer.Instance != null)
            LevelTimer.Instance.StopTimers();

        FreezeAllCharacters();

        if (levelCompleteUI != null)
            levelCompleteUI.Show();
        else
            Invoke(nameof(LoadNextLevel), 1.5f);
    }

    private void FreezeAllCharacters()
    {
        if (CharacterSwitcher.Instance == null) return;

        PlayerController arc = CharacterSwitcher.Instance.GetARC();
        PlayerController echo = CharacterSwitcher.Instance.GetECHO();

        if (arc != null) arc.SetActive(false);
        if (echo != null) echo.SetActive(false);
    }

    public void LoadNextLevel()
    {
        int nextIndex = SceneManager.GetActiveScene().buildIndex + 1;
        SceneManager.LoadScene(nextIndex < SceneManager.sceneCountInBuildSettings ? nextIndex : 0);
    }

    // ------------------------------------------------------------------ //
    // Reset
    // ------------------------------------------------------------------ //
    public void ResetLevel()
    {
        if (levelEnded) return;

        Debug.Log("Resetting level.");

        if (UIManager.Instance != null)
            UIManager.Instance.FlashDeath();

        Invoke(nameof(DoReset), 0.3f);
    }

    private void DoReset()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}