using TurboVision.Application;
using TurboVision.Core;
using TurboVision.Dialogs;
using TurboVision.Views;

namespace TvDemo;

/// <summary>
/// A dialog for changing the desktop background pattern.
/// </summary>
public class TChBackground : TDialog
{
    private readonly TBackground? _background;
    private readonly TInputLine _input;

    public TChBackground(TBackground? background)
        : base(new TRect(0, 0, 29, 9), null)
    {
        _background = background;

        // Center the dialog
        var r = GetExtent();
        if (TApplication.DeskTop != null)
        {
            var desk = TApplication.DeskTop.Size;
            r = new TRect(
                (desk.X - r.B.X) / 2,
                (desk.Y - r.B.Y) / 2,
                (desk.X - r.B.X) / 2 + r.B.X,
                (desk.Y - r.B.Y) / 2 + r.B.Y);
            ChangeBounds(r);
        }

        _input = new TInputLine(new TRect(4, 5, 7, 6), 1);
        Insert(_input);

        Insert(new TStaticText(new TRect(2, 2, 27, 3), "Enter background pattern:"));
        Insert(new TButton(new TRect(16, 4, 26, 6), "~A~pply", CommandConstants.cmOK, CommandConstants.bfDefault));
        Insert(new TButton(new TRect(16, 6, 26, 8), "~C~lose", CommandConstants.cmCancel, CommandConstants.bfNormal));

        _input.Focus();
    }

    public override bool Valid(ushort command)
    {
        if (base.Valid(command))
        {
            if (_background != null && command == CommandConstants.cmOK)
            {
                string? data = _input.Data;
                if (!string.IsNullOrEmpty(data))
                {
                    _background.Pattern = data[0];
                    _background.DrawView();
                }
                return false; // Keep dialog open
            }
            return true;
        }
        return false;
    }
}
