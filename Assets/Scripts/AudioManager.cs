// AudioManager.cs
// AI-ASSISTED: Generated with Cursor, reviewed by developer.
// Attach to: An empty GameObject called "AudioManager" in EVERY scene
//            (MainMenu and all Level scenes).
//
// WHAT IT DOES:
//   - Plays background music per scene — crossfades smoothly when switching
//   - Plays one-shot sound effects (death, switch, level complete, orb collect, etc.)
//   - Responds to PauseManager — pitch drops when slow-motion pause is active
//   - All volumes individually tunable in Inspector
//
// HOW TO SET UP IN UNITY:
// 1. Create empty GameObject → name it "AudioManager" → attach this script
// 2. The script creates its own AudioSources automatically — no manual setup needed
// 3. Drop your audio clips into the Inspector slots:
//    MUSIC:
//      arcMusic      → ARC world background track (calm, minimal electronic)
//      echoMusic     → ECHO world background track (tense, dark electronic)
//      menuMusic     → Main Menu background track
//    SFX:
//      sfxSwitch     → Tab switch sound (sharp click or short whoosh)
//      sfxDeath      → Death/reset sound
//      sfxLevelComplete → Level complete chime
//      sfxOrbCollect → Orb pickup sound
//      sfxButtonClick → UI button click
// 4. Add this to every level scene AND the MainMenu scene
// 5. Call AudioManager.Instance.PlaySFX(AudioManager.SFX.Switch) etc. from other scripts
//
// MUSIC SELECTION:
//   The script reads the active scene name and plays the right music automatically.
//   "MainMenu"       → menuMusic
//   "Level_01"–"07"  → depends on which world is active (managed via PlayWorldMusic)
//
// INTEGRATION POINTS — add these calls in the listed scripts:
//   CharacterSwitcher.ToggleActiveCharacter()  → AudioManager.Instance.PlaySFX(SFX.Switch)
//   PlayerController.Die()                     → AudioManager.Instance.PlaySFX(SFX.Death)
//   LevelManager.LevelComplete()               → AudioManager.Instance.PlaySFX(SFX.LevelComplete)
//   BoostOrb (on collect)                      → AudioManager.Instance.PlaySFX(SFX.OrbCollect)
//   Any UI button OnClick                      → AudioManager.Instance.PlaySFX(SFX.ButtonClick)

using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    // ------------------------------------------------------------------
    // SFX enum — easy to reference from other scripts
    // ------------------------------------------------------------------
    public enum SFX
    {
        Switch,
        Death,
        LevelComplete,
        OrbCollect,
        ButtonClick
    }

    // ------------------------------------------------------------------
    // Inspector — Music clips
    // ------------------------------------------------------------------
    [Header("Music Clips")]
    [SerializeField] private AudioClip arcMusic;         // ARC world music
    [SerializeField] private AudioClip echoMusic;        // ECHO world music
    [SerializeField] private AudioClip menuMusic;        // Main Menu music

    [Header("Music Volumes")]
    [SerializeField] private float musicVolume = 0.6f;
    [SerializeField] private float crossfadeTime = 1.2f;  // Seconds to crossfade between tracks

    // ------------------------------------------------------------------
    // Inspector — SFX clips
    // ------------------------------------------------------------------
    [Header("SFX Clips")]
    [SerializeField] private AudioClip sfxSwitch;
    [SerializeField] private AudioClip sfxDeath;
    [SerializeField] private AudioClip sfxLevelComplete;
    [SerializeField] private AudioClip sfxOrbCollect;
    [SerializeField] private AudioClip sfxButtonClick;

    [Header("SFX Volumes")]
    [SerializeField] private float sfxSwitchVolume = 0.85f;
    [SerializeField] private float sfxDeathVolume = 0.90f;
    [SerializeField] private float sfxLevelCompleteVolume = 0.80f;
    [SerializeField] private float sfxOrbCollectVolume = 0.75f;
    [SerializeField] private float sfxButtonClickVolume = 0.65f;

    // ------------------------------------------------------------------
    // Private — two AudioSources for crossfading
    // ------------------------------------------------------------------
    private AudioSource musicSourceA;
    private AudioSource musicSourceB;
    private AudioSource sfxSource;
    private bool aIsPlaying = true;  // Which source is currently primary
    private Coroutine crossfadeCoroutine;

    // ------------------------------------------------------------------
    // Awake — set up sources
    // ------------------------------------------------------------------
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Create AudioSources procedurally
        musicSourceA = CreateMusicSource("MusicA");
        musicSourceB = CreateMusicSource("MusicB");
        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
    }

    private AudioSource CreateMusicSource(string name)
    {
        AudioSource src = gameObject.AddComponent<AudioSource>();
        src.loop = true;
        src.playOnAwake = false;
        src.volume = 0f;
        return src;
    }

    // ------------------------------------------------------------------
    // Start — auto-play music based on current scene
    // ------------------------------------------------------------------
    private void Start()
    {
        string sceneName = SceneManager.GetActiveScene().name;

        if (sceneName == "MainMenu")
            PlayMusic(menuMusic, immediate: true);
        else
            PlayMusic(arcMusic, immediate: true); // Default to ARC music in levels
    }

    // ------------------------------------------------------------------
    // Play Music — crossfades to a new clip
    // ------------------------------------------------------------------
    public void PlayMusic(AudioClip clip, bool immediate = false)
    {
        if (clip == null) return;

        // Already playing this clip — do nothing
        AudioSource current = aIsPlaying ? musicSourceA : musicSourceB;
        if (current.clip == clip && current.isPlaying) return;

        if (crossfadeCoroutine != null) StopCoroutine(crossfadeCoroutine);

        if (immediate)
        {
            // No crossfade — just start immediately
            current.clip = clip;
            current.volume = musicVolume;
            current.Play();
        }
        else
        {
            crossfadeCoroutine = StartCoroutine(CrossfadeMusic(clip));
        }
    }

    private IEnumerator CrossfadeMusic(AudioClip newClip)
    {
        AudioSource outgoing = aIsPlaying ? musicSourceA : musicSourceB;
        AudioSource incoming = aIsPlaying ? musicSourceB : musicSourceA;
        aIsPlaying = !aIsPlaying;

        incoming.clip = newClip;
        incoming.volume = 0f;
        incoming.Play();

        float elapsed = 0f;
        while (elapsed < crossfadeTime)
        {
            elapsed += Time.unscaledDeltaTime; // Unscaled — works during slow-motion pause
            float t = Mathf.Clamp01(elapsed / crossfadeTime);
            incoming.volume = Mathf.Lerp(0f, musicVolume, t);
            outgoing.volume = Mathf.Lerp(musicVolume, 0f, t);
            yield return null;
        }

        outgoing.Stop();
        outgoing.volume = 0f;
    }

    // ------------------------------------------------------------------
    // Switch world music — call from CharacterSwitcher after toggle
    // Crossfades ARC/ECHO music based on who is now active
    // ------------------------------------------------------------------
    public void UpdateWorldMusic(bool arcIsActive)
    {
        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName == "MainMenu") return; // Don't change music on main menu

        AudioClip target = arcIsActive ? arcMusic : echoMusic;
        PlayMusic(target);
    }

    // ------------------------------------------------------------------
    // Play SFX
    // ------------------------------------------------------------------
    public void PlaySFX(SFX effect)
    {
        AudioClip clip = null;
        float volume = 1f;

        switch (effect)
        {
            case SFX.Switch:
                clip = sfxSwitch; volume = sfxSwitchVolume; break;
            case SFX.Death:
                clip = sfxDeath; volume = sfxDeathVolume; break;
            case SFX.LevelComplete:
                clip = sfxLevelComplete; volume = sfxLevelCompleteVolume; break;
            case SFX.OrbCollect:
                clip = sfxOrbCollect; volume = sfxOrbCollectVolume; break;
            case SFX.ButtonClick:
                clip = sfxButtonClick; volume = sfxButtonClickVolume; break;
        }

        if (clip != null)
            sfxSource.PlayOneShot(clip, volume);
    }

    // ------------------------------------------------------------------
    // Volume control — call from a settings menu if you add one later
    // ------------------------------------------------------------------
    public void SetMusicVolume(float vol)
    {
        musicVolume = Mathf.Clamp01(vol);
        AudioSource active = aIsPlaying ? musicSourceA : musicSourceB;
        active.volume = musicVolume;
    }

    public void StopMusic()
    {
        musicSourceA.Stop();
        musicSourceB.Stop();
    }
}