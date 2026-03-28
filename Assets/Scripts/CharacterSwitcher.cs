// CharacterSwitcher.cs
// AI-ASSISTED: Generated with Cursor, reviewed by developer.
// Attach to: An empty GameObject called "CharacterSwitcher" in every game scene.
// UPDATED: Calls AudioManager on switch + world music crossfade.

using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterSwitcher : MonoBehaviour
{
    public static CharacterSwitcher Instance { get; private set; }

    [Header("Characters")]
    [SerializeField] private PlayerController arc;
    [SerializeField] private PlayerController echo;

    [Header("Cameras")]
    [SerializeField] private Camera cameraARC;
    [SerializeField] private Camera cameraECHO;

    [Header("Brightness")]
    [SerializeField] private Color activeBgColour = new Color(0.18f, 0.18f, 0.22f, 1f);
    [SerializeField] private Color inactiveBgColour = new Color(0.04f, 0.04f, 0.07f, 1f);
    [SerializeField] private float activateSpeed = 9f;
    [SerializeField] private float deactivateSpeed = 5f;

    private bool arcIsActive = true;
    private Color arcCurrentColour;
    private Color arcTargetColour;
    private Color echoCurrentColour;
    private Color echoTargetColour;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        arcCurrentColour = activeBgColour;
        echoCurrentColour = inactiveBgColour;
        arcTargetColour = activeBgColour;
        echoTargetColour = inactiveBgColour;

        ActivateCharacter(arc, echo);
        ApplyBgColour(cameraARC, arcCurrentColour);
        ApplyBgColour(cameraECHO, echoCurrentColour);
    }

    private void Update()
    {
        float arcSpeed = arcCurrentColour.r < arcTargetColour.r ? activateSpeed : deactivateSpeed;
        float echoSpeed = echoCurrentColour.r < echoTargetColour.r ? activateSpeed : deactivateSpeed;

        arcCurrentColour = Color.Lerp(arcCurrentColour, arcTargetColour, Time.deltaTime * arcSpeed);
        echoCurrentColour = Color.Lerp(echoCurrentColour, echoTargetColour, Time.deltaTime * echoSpeed);

        ApplyBgColour(cameraARC, arcCurrentColour);
        ApplyBgColour(cameraECHO, echoCurrentColour);
    }

    public void OnSwitch(InputValue value)
    {
        if (value.isPressed) ToggleActiveCharacter();
    }

    public void ToggleActiveCharacter()
    {
        arcIsActive = !arcIsActive;

        if (arcIsActive) ActivateCharacter(arc, echo);
        else ActivateCharacter(echo, arc);

        // Flash dividing line
        if (UIManager.Instance != null)
            UIManager.Instance.FlashDividingLine();

        // Play switch SFX
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(AudioManager.SFX.Switch);

        // Crossfade world music to match who is now active
        if (AudioManager.Instance != null)
            AudioManager.Instance.UpdateWorldMusic(arcIsActive);

        Debug.Log($"Switched to: {(arcIsActive ? "ARC" : "ECHO")}");
    }

    public void SwitchToARC() { if (!arcIsActive) ToggleActiveCharacter(); }
    public void SwitchToECHO() { if (arcIsActive) ToggleActiveCharacter(); }

    private void ActivateCharacter(PlayerController activate, PlayerController deactivate)
    {
        activate.SetActive(true);
        deactivate.SetActive(false);

        bool activatingARC = (activate == arc);
        arcTargetColour = activatingARC ? activeBgColour : inactiveBgColour;
        echoTargetColour = activatingARC ? inactiveBgColour : activeBgColour;
    }

    private void ApplyBgColour(Camera cam, Color colour)
    {
        if (cam != null) cam.backgroundColor = colour;
    }

    public bool ArcIsActive() => arcIsActive;
    public PlayerController GetActiveCharacter() => arcIsActive ? arc : echo;
    public PlayerController GetARC() => arc;
    public PlayerController GetECHO() => echo;
}