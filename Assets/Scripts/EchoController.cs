// EchoController.cs
// Attach to: ECHO GameObject
// Requires: Rigidbody2D, BoxCollider2D on the same GameObject
//
// ECHO now inherits from PlayerController with NO movement changes.
// Since characters are controlled one at a time, ECHO controls
// normally in his world — no mirrored input needed anymore.
//
// The PlayerInput component on ARC is set to "Broadcast Messages"
// so ECHO receives input callbacks automatically without needing
// its own PlayerInput component.
//
// NOTE: Rename MirroredController.cs to EchoController.cs
// OR delete MirroredController.cs and use this file instead.

using UnityEngine;

public class EchoController : PlayerController
{
    // No overrides needed — ECHO behaves identically to ARC now.
    // All active/inactive logic is inherited from PlayerController.
    //
    // The only reason this class exists separately is so you can
    // add ECHO-specific behaviour later (e.g. a unique ability)
    // without touching ARC's code.
}