// UIPopIn.cs
// Small entrance animation: scales a panel up from slightly smaller and fades it in
// every time it is enabled. Uses unscaled time so it works while paused.

using System.Collections;
using UnityEngine;

public class UIPopIn : MonoBehaviour
{
    public float duration = 0.32f;
    public float startScale = 0.92f;
    public Vector2 startOffset = new Vector2(0f, -18f);

    private RectTransform rt;
    private CanvasGroup group;
    private Vector2 restPosition;
    private bool hasRest;

    private void Awake()
    {
        rt = transform as RectTransform;
        group = GetComponent<CanvasGroup>();
        if (group == null) group = gameObject.AddComponent<CanvasGroup>();
    }

    private void OnEnable()
    {
        if (rt == null) Awake();
        if (!hasRest) { restPosition = rt.anchoredPosition; hasRest = true; }
        StopAllCoroutines();
        StartCoroutine(Play());
    }

    private void OnDisable()
    {
        if (rt != null && hasRest)
        {
            rt.anchoredPosition = restPosition;
            rt.localScale = Vector3.one;
        }
        if (group != null) group.alpha = 1f;
    }

    private IEnumerator Play()
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = t / duration;
            float s = Mathf.LerpUnclamped(startScale, 1f, FlipsideTheme.EaseOutBack(k));
            rt.localScale = new Vector3(s, s, 1f);
            rt.anchoredPosition = restPosition + startOffset * (1f - FlipsideTheme.EaseOutCubic(k));
            group.alpha = FlipsideTheme.EaseOutCubic(k * 1.6f);
            yield return null;
        }
        rt.localScale = Vector3.one;
        rt.anchoredPosition = restPosition;
        group.alpha = 1f;
    }
}
