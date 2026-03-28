// LevelTimer.cs
// AI-ASSISTED: Generated with Cursor, reviewed by developer.
// Attach to: any GameObject in your level scene.
//
// Assign your own TMP text objects and frost UI Images in the Inspector.
// The script does not create or position any UI.

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;

public class LevelTimer : MonoBehaviour
{
    public static LevelTimer Instance { get; private set; }

    [Header("Timer Settings")]
    [SerializeField] private float arcStartTime = 60f;
    [SerializeField] private float echoStartTime = 60f;
    [SerializeField] private float inactiveTimerSpeed = 0.25f;

    [Header("Timer Text")]
    [SerializeField] private TextMeshProUGUI arcTimerText;
    [SerializeField] private TextMeshProUGUI echoTimerText;

    [Header("Slow Frost Icons")]
    [SerializeField] private Image arcFrostIcon;
    [SerializeField] private Image echoFrostIcon;

    [Header("Colour Thresholds")]
    [SerializeField] private float warningThreshold = 10f;
    [SerializeField] private float criticalThreshold = 5f;

    [Header("Active Timer Colours")]
    [SerializeField] private Color normalColour = Color.white;
    [SerializeField] private Color warningColour = new Color(1.00f, 0.40f, 0.10f, 1f);
    [SerializeField] private Color criticalColour = new Color(1.00f, 0.10f, 0.10f, 1f);

    [Header("Slow Timer Colours")]
    [SerializeField] private Color slowColourA = new Color(0.50f, 0.85f, 1.00f, 1f);
    [SerializeField] private Color slowColourB = new Color(0.82f, 0.96f, 1.00f, 1f);

    [Header("Slow Timer Wave")]
    [SerializeField] private float waveAmplitude = 2.2f;
    [SerializeField] private float waveFrequency = 1.3f;
    [SerializeField] private float waveSpread = 0.65f;

    [Header("Frost Icon Pulse")]
    [SerializeField] private float frostPulseSpeed = 1.5f;
    [SerializeField] private float frostMinAlpha = 0.45f;
    [SerializeField] private float frostMaxAlpha = 1.0f;

    private float arcTimeRemaining;
    private float echoTimeRemaining;
    private bool timerRunning = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        arcTimeRemaining = arcStartTime;
        echoTimeRemaining = echoStartTime;
        timerRunning = true;

        SetIconVisible(arcFrostIcon, false);
        SetIconVisible(echoFrostIcon, false);

        RefreshTimerText(arcTimerText, arcTimeRemaining);
        RefreshTimerText(echoTimerText, echoTimeRemaining);
    }

    private void Update()
    {
        if (!timerRunning) return;

        bool arcIsActive = CharacterSwitcher.Instance != null
                        && CharacterSwitcher.Instance.ArcIsActive();

        float arcRate = arcIsActive ? 1f : inactiveTimerSpeed;
        float echoRate = !arcIsActive ? 1f : inactiveTimerSpeed;

        arcTimeRemaining = Mathf.Max(0f, arcTimeRemaining - Time.deltaTime * arcRate);
        echoTimeRemaining = Mathf.Max(0f, echoTimeRemaining - Time.deltaTime * echoRate);

        UpdateTimer(arcTimerText, arcFrostIcon, arcTimeRemaining, arcIsActive, 0.0f);
        UpdateTimer(echoTimerText, echoFrostIcon, echoTimeRemaining, !arcIsActive, 1.3f);

        if (arcTimeRemaining <= 0f) { TimerExpired("ARC"); return; }
        if (echoTimeRemaining <= 0f) { TimerExpired("ECHO"); return; }
    }

    private void LateUpdate()
    {
        if (!timerRunning) return;

        bool arcIsActive = CharacterSwitcher.Instance != null
                        && CharacterSwitcher.Instance.ArcIsActive();

        if (!arcIsActive && arcTimerText != null) ApplyVertexWave(arcTimerText, 0.0f);
        if (arcIsActive && echoTimerText != null) ApplyVertexWave(echoTimerText, 1.3f);
    }

    private void UpdateTimer(TextMeshProUGUI label, Image frostIcon,
                             float time, bool isActive, float phaseOffset)
    {
        if (label == null) return;

        if (isActive)
        {
            SetIconVisible(frostIcon, false);
            label.transform.localScale = Vector3.one;
            RefreshTimerText(label, time);

            if (time <= criticalThreshold)
            {
                float strobe = (Mathf.Sin(Time.time * 14f) + 1f) * 0.5f;
                label.color = Color.Lerp(criticalColour, new Color(1f, 0.65f, 0.65f), strobe);
            }
            else if (time <= warningThreshold)
            {
                float t = 1f - (time - criticalThreshold) / (warningThreshold - criticalThreshold);
                label.color = Color.Lerp(normalColour, warningColour, t);
            }
            else
            {
                label.color = normalColour;
            }
        }
        else
        {
            RefreshTimerText(label, time);

            float breath = (Mathf.Sin(Time.time * 1.9f + phaseOffset) + 1f) * 0.5f;
            label.color = Color.Lerp(slowColourA, slowColourB, breath);

            float breathScale = Mathf.Lerp(0.88f, 0.97f, breath);
            label.transform.localScale = new Vector3(breathScale, breathScale, 1f);

            SetIconVisible(frostIcon, true);
            if (frostIcon != null)
            {
                float iconBreath = (Mathf.Sin(Time.time * frostPulseSpeed + phaseOffset) + 1f) * 0.5f;
                Color c = frostIcon.color;
                c.a = Mathf.Lerp(frostMinAlpha, frostMaxAlpha, iconBreath);
                frostIcon.color = c;
            }
        }
    }

    private void ApplyVertexWave(TextMeshProUGUI tmp, float phaseOffset)
    {
        tmp.ForceMeshUpdate();
        TMP_TextInfo textInfo = tmp.textInfo;
        if (textInfo.characterCount == 0) return;

        TMP_MeshInfo[] cachedMeshInfo = textInfo.CopyMeshInfoVertexData();

        for (int m = 0; m < textInfo.meshInfo.Length; m++)
        {
            Vector3[] srcVerts = cachedMeshInfo[m].vertices;
            Vector3[] dstVerts = textInfo.meshInfo[m].vertices;

            if (srcVerts == null || dstVerts == null || srcVerts.Length != dstVerts.Length)
                continue;

            Array.Copy(srcVerts, dstVerts, srcVerts.Length);
        }

        for (int charIdx = 0; charIdx < textInfo.characterCount; charIdx++)
        {
            TMP_CharacterInfo charInfo = textInfo.characterInfo[charIdx];
            if (!charInfo.isVisible) continue;

            int matIdx = charInfo.materialReferenceIndex;
            Vector3[] verts = textInfo.meshInfo[matIdx].vertices;

            float wave = Mathf.Sin(Time.time * waveFrequency
                                   + charIdx * waveSpread + phaseOffset)
                         * waveAmplitude;

            for (int v = 0; v < 4; v++)
                verts[charInfo.vertexIndex + v].y += wave;
        }

        for (int m = 0; m < textInfo.meshInfo.Length; m++)
        {
            textInfo.meshInfo[m].mesh.vertices = textInfo.meshInfo[m].vertices;
            tmp.UpdateGeometry(textInfo.meshInfo[m].mesh, m);
        }
    }

    private void RefreshTimerText(TextMeshProUGUI label, float seconds)
    {
        if (label == null) return;

        int m = Mathf.FloorToInt(seconds / 60f);
        int s = Mathf.FloorToInt(seconds % 60f);
        int cs = Mathf.FloorToInt((seconds * 100f) % 100f);

        label.text = $"{m}:{s:00}.{cs:00}";
    }

    private void SetIconVisible(Image icon, bool visible)
    {
        if (icon != null)
            icon.gameObject.SetActive(visible);
    }

    private void TimerExpired(string character)
    {
        timerRunning = false;
        Debug.Log($"{character}'s timer expired!");

        if (UIManager.Instance != null)
            UIManager.Instance.FlashDeath();

        StartCoroutine(ResetAfterDelay(0.4f));
    }

    private IEnumerator ResetAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (LevelManager.Instance != null)
            LevelManager.Instance.ResetLevel();
    }

    public void StopTimers()
    {
        timerRunning = false;

        Color doneGreen = new Color(0.4f, 1f, 0.4f);

        if (arcTimerText != null)
        {
            arcTimerText.color = doneGreen;
            arcTimerText.transform.localScale = Vector3.one;
        }

        if (echoTimerText != null)
        {
            echoTimerText.color = doneGreen;
            echoTimerText.transform.localScale = Vector3.one;
        }

        SetIconVisible(arcFrostIcon, false);
        SetIconVisible(echoFrostIcon, false);
    }

    public void ResetTimers()
    {
        arcTimeRemaining = arcStartTime;
        echoTimeRemaining = echoStartTime;
        timerRunning = true;

        RefreshTimerText(arcTimerText, arcTimeRemaining);
        RefreshTimerText(echoTimerText, echoTimeRemaining);

        SetIconVisible(arcFrostIcon, false);
        SetIconVisible(echoFrostIcon, false);
    }

    public float GetARCTimeRemaining() => arcTimeRemaining;
    public float GetECHOTimeRemaining() => echoTimeRemaining;
}