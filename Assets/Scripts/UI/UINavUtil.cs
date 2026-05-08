using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Utility to disable keyboard navigation on UI Selectables.
/// Prevents arrow keys / A/D from moving focus between gameplay UI elements.
/// </summary>
public static class UINavUtil
{
    /// <summary>Set navigation=None on all Selectable components under root (inclusive).</summary>
    public static void DisableAll(Transform root)
    {
        if (root == null) return;

        // Include the root itself
        var rootSel = root.GetComponent<Selectable>();
        if (rootSel != null) SetNone(rootSel);

        // And all children
        var selectables = root.GetComponentsInChildren<Selectable>(true);
        for (int i = 0; i < selectables.Length; i++)
            SetNone(selectables[i]);
    }

    private static void SetNone(Selectable sel)
    {
        var nav = sel.navigation;
        nav.mode = Navigation.Mode.None;
        sel.navigation = nav;
    }
}
