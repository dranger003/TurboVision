using TurboVision.Core;
using TurboVision.Views;

namespace TurboVision.Dialogs;

/// <summary>
/// File name input line with broadcast event handling.
/// Updates its content when a file is focused in the file list.
/// </summary>
public class TFileInputLine : TInputLine
{
    public TFileInputLine(TRect bounds, int maxLen)
        : base(bounds, maxLen)
    {
        EventMask |= EventConstants.evBroadcast;
    }

    public override void HandleEvent(ref TEvent ev)
    {
        base.HandleEvent(ref ev);

        if (ev.What == EventConstants.evBroadcast &&
            ev.Message.Command == FileDialogCommands.cmFileFocused &&
            !GetState(StateFlags.sfSelected))
        {
            if (ev.Message.InfoPtr is TSearchRec searchRec)
            {
                // Update the input line with the focused file name
                string newName = searchRec.Name;

                // If it's a directory, append backslash and wildcard
                if (searchRec.IsDirectory)
                {
                    newName += "\\";

                    // Get wildcard from owner TFileDialog
                    if (Owner is TFileDialog fileDialog)
                    {
                        newName += fileDialog.WildCard;
                    }
                }

                Data = newName;
                SelectAll(false);
                DrawView();
            }
        }
    }
}
