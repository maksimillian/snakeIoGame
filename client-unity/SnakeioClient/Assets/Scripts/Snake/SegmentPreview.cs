using UnityEngine;

#if UNITY_EDITOR          // only the Editor needs these symbols
using UnityEditor;
#endif

/// <summary>
/// Lets you scrub / drive the Animator int parameter “segment”
/// both in Edit Mode (for live previews) and in a built game.
/// </summary>
[ExecuteAlways]            // keeps Edit-mode preview functionality
[DisallowMultipleComponent]
public sealed class SegmentPreviewer : MonoBehaviour
{
    [Min(0)]
    public int segment = 0;                 // visible in Inspector / scriptable at runtime

    private static readonly int SegmentHash = Animator.StringToHash("segment");
    private Animator anim;
    private int lastApplied = int.MinValue; // avoids redundant writes

    /* ---------- lifetime -------------------------------------------------- */

    private void Awake()  => CacheAnimator();
    private void OnEnable()
    {
        CacheAnimator();

#if UNITY_EDITOR          // tick in Edit Mode without allocating Update calls
        EditorApplication.update += Apply;
#endif
        
        // Force re-apply when enabled to ensure state is correct
        lastApplied = int.MinValue; // Reset cache to force update
        Apply();
    }

    private void OnDisable()
    {
#if UNITY_EDITOR
        EditorApplication.update -= Apply;
#endif
    }

#if UNITY_EDITOR
    // Fires when you tweak the field in the Inspector while *not* in Play Mode
    private void OnValidate() => Apply();
#endif

    // Runs only in Play Mode (Editor or build)
    private void Update()
    {
        if (!Application.isPlaying) return; // Edit-mode previews already handled above
        Apply();
    }

    /* ---------- helpers --------------------------------------------------- */

    private void CacheAnimator()
    {
        if (anim == null) anim = GetComponent<Animator>();
    }

    private void Apply()
    {
        if (!anim) return;

        // Write only when the value really changed (cheap caching)
        if (segment != lastApplied)
        {
            anim.SetInteger(SegmentHash, segment);
            lastApplied = segment;
        }
    }

    /// <summary>Optional public API for buttons, other scripts, etc.</summary>
    public void SetSegment(int value)
    {
        segment = value;
        Apply();
    }
}
