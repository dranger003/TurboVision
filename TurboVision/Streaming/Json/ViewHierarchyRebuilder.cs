using TurboVision.Dialogs;
using TurboVision.Views;

namespace TurboVision.Streaming.Json;

/// <summary>
/// Reconstructs view hierarchy relationships after JSON deserialization.
/// Rebuilds Owner/Next pointers and linked view references that are
/// excluded from serialization to avoid circular references.
/// </summary>
public static class ViewHierarchyRebuilder
{
    /// <summary>
    /// Rebuilds the view hierarchy starting from the given root object.
    /// </summary>
    /// <param name="root">The root object to rebuild from.</param>
    public static void Rebuild(IStreamable root)
    {
        if (root is TGroup group)
        {
            RebuildGroup(group);
        }
    }

    /// <summary>
    /// Rebuilds a TGroup's child view relationships.
    /// </summary>
    /// <param name="group">The group to rebuild.</param>
    private static void RebuildGroup(TGroup group)
    {
        // Get the pending subviews from deserialization
        var subViews = group.GetPendingSubViews();
        if (subViews == null || subViews.Count == 0)
        {
            group.ClearPendingData();
            return;
        }

        // Filter out null entries
        var validViews = new List<TView>();
        foreach (var view in subViews)
        {
            if (view != null)
            {
                validViews.Add(view);
            }
        }

        if (validViews.Count == 0)
        {
            group.ClearPendingData();
            return;
        }

        // Build circular linked list from SubViews array
        for (int i = 0; i < validViews.Count; i++)
        {
            var view = validViews[i];

            // Set owner reference
            view.Owner = group;

            // Set next pointer to create circular list
            // Each view points to the next, last points to first
            view.Next = validViews[(i + 1) % validViews.Count];
        }

        // Set Last to point to the last view in the list
        group.Last = validViews[^1];

        // Restore Current from CurrentIndex if valid
        var currentIndex = group.GetPendingCurrentIndex();
        if (currentIndex >= 0 && currentIndex < validViews.Count)
        {
            group.Current = validViews[currentIndex];
        }
        else
        {
            group.Current = null;
        }

        // Clear pending data now that we've processed it
        group.ClearPendingData();

        // Recursively rebuild any child groups
        foreach (var view in validViews)
        {
            if (view is TGroup childGroup)
            {
                RebuildGroup(childGroup);
            }
        }

        // Resolve linked view references (TLabel.Link, THistory.Link, etc.)
        ResolveLinkedReferences(group, validViews);
    }

    /// <summary>
    /// Resolves linked view references within a group.
    /// Views like TLabel and THistory have Link properties that reference
    /// other views by index during serialization.
    /// </summary>
    private static void ResolveLinkedReferences(TGroup group, IReadOnlyList<TView> subViews)
    {
        foreach (var view in subViews)
        {
            switch (view)
            {
                case TLabel label when label.LinkIndex >= 0 && label.LinkIndex < subViews.Count:
                    label.Link = subViews[label.LinkIndex];
                    break;

                case THistory history when history.LinkIndex >= 0 && history.LinkIndex < subViews.Count:
                    history.Link = subViews[history.LinkIndex] as TInputLine;
                    break;
            }
        }
    }
}
