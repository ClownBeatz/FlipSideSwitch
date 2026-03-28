// HazardTile.cs
// Attach to: Any hazard GameObject (spikes, energy fields, etc.)
// Requires: A Collider2D set to "Is Trigger" on the same GameObject
//
// VISUAL UPGRADE:
//   When a character touches the hazard, the hazard sprite flashes bright red/white
//   for a split second before the level resets — gives visual punch to the death.
//   (The flash happens simultaneously with UIManager's death flash, not after.)
//
//   If the GameObject has no SpriteRenderer, behaviour is unchanged.

using UnityEngine;
using System.Collections;

public class HazardTile : MonoBehaviour
{
    [Header("Death Flash")]
    [SerializeField] private Color flashColour = new Color(1f, 0.15f, 0.15f, 1f);
    [SerializeField] private float flashDuration = 0.12f;  // Seconds before kill triggers

    private SpriteRenderer sr;
    private bool triggered = false;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered) return;

        PlayerController player = other.GetComponent<PlayerController>();
        if (player == null) return;

        triggered = true;
        StartCoroutine(FlashAndKill(player));
    }

    private IEnumerator FlashAndKill(PlayerController player)
    {
        // Flash the hazard sprite to signal the hit
        if (sr != null)
        {
            Color originalColour = sr.color;

            // Quick white → red flash
            sr.color = Color.white;
            yield return null;                          // one frame white
            sr.color = flashColour;
            yield return new WaitForSeconds(flashDuration);
            sr.color = originalColour;
        }

        // Kill — LevelManager handles the screen flash and reset
        player.Die();

        // Reset triggered flag after a frame so the death-reset cycle can continue cleanly
        yield return null;
        triggered = false;
    }
}