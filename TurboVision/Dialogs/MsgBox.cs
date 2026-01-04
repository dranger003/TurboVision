using TurboVision.Application;
using TurboVision.Core;
using TurboVision.Views;

namespace TurboVision.Dialogs;

/// <summary>
/// Message box class flags.
/// </summary>
[Flags]
public enum MessageBoxFlags : ushort
{
    // Message box classes (stored in lower 2 bits)
    mfWarning = 0x0000,
    mfError = 0x0001,
    mfInformation = 0x0002,
    mfConfirmation = 0x0003,

    // Message box button flags
    mfYesButton = 0x0100,
    mfNoButton = 0x0200,
    mfOKButton = 0x0400,
    mfCancelButton = 0x0800,

    // Standard button combinations
    mfYesNoCancel = mfYesButton | mfNoButton | mfCancelButton,
    mfOKCancel = mfOKButton | mfCancelButton
}

/// <summary>
/// Localizable text strings for message boxes.
/// </summary>
public static class MsgBoxText
{
    public static string YesText { get; set; } = "~Y~es";
    public static string NoText { get; set; } = "~N~o";
    public static string OKText { get; set; } = "O~K~";
    public static string CancelText { get; set; } = "Cancel";
    public static string WarningText { get; set; } = "Warning";
    public static string ErrorText { get; set; } = "Error";
    public static string InformationText { get; set; } = "Information";
    public static string ConfirmText { get; set; } = "Confirm";
}

/// <summary>
/// Standard message box and input box functions.
/// </summary>
public static class MsgBox
{
    private static readonly string[] ButtonNames =
    [
        MsgBoxText.YesText,
        MsgBoxText.NoText,
        MsgBoxText.OKText,
        MsgBoxText.CancelText
    ];

    private static readonly ushort[] Commands =
    [
        CommandConstants.cmYes,
        CommandConstants.cmNo,
        CommandConstants.cmOK,
        CommandConstants.cmCancel
    ];

    private static readonly string[] Titles =
    [
        MsgBoxText.WarningText,
        MsgBoxText.ErrorText,
        MsgBoxText.InformationText,
        MsgBoxText.ConfirmText
    ];

    /// <summary>
    /// Displays a message box with the specified message and options.
    /// </summary>
    /// <param name="msg">The message to display.</param>
    /// <param name="aOptions">Options specifying the message type and buttons.</param>
    /// <returns>The command code of the button pressed.</returns>
    public static ushort MessageBox(string msg, ushort aOptions)
    {
        var r = MakeRect(msg);
        return MessageBoxRect(r, msg, aOptions);
    }

    /// <summary>
    /// Displays a message box with the specified message and options.
    /// </summary>
    /// <param name="msg">The message to display.</param>
    /// <param name="aOptions">Options specifying the message type and buttons.</param>
    /// <returns>The command code of the button pressed.</returns>
    public static ushort MessageBox(string msg, MessageBoxFlags aOptions)
    {
        return MessageBox(msg, (ushort)aOptions);
    }

    /// <summary>
    /// Displays a message box with formatted message.
    /// </summary>
    /// <param name="aOptions">Options specifying the message type and buttons.</param>
    /// <param name="format">Format string.</param>
    /// <param name="args">Format arguments.</param>
    /// <returns>The command code of the button pressed.</returns>
    public static ushort MessageBox(ushort aOptions, string format, params object[] args)
    {
        string msg = string.Format(format, args);
        return MessageBox(msg, aOptions);
    }

    /// <summary>
    /// Displays a message box with formatted message.
    /// </summary>
    /// <param name="aOptions">Options specifying the message type and buttons.</param>
    /// <param name="format">Format string.</param>
    /// <param name="args">Format arguments.</param>
    /// <returns>The command code of the button pressed.</returns>
    public static ushort MessageBox(MessageBoxFlags aOptions, string format, params object[] args)
    {
        return MessageBox((ushort)aOptions, format, args);
    }

    /// <summary>
    /// Displays a message box at a specific location.
    /// </summary>
    /// <param name="r">The bounding rectangle for the dialog.</param>
    /// <param name="msg">The message to display.</param>
    /// <param name="aOptions">Options specifying the message type and buttons.</param>
    /// <returns>The command code of the button pressed.</returns>
    public static ushort MessageBoxRect(TRect r, string msg, ushort aOptions)
    {
        // Get dialog title based on message type (lower 2 bits)
        string title = Titles[aOptions & 0x3];

        var dialog = new TDialog(r, title);

        // Insert static text for the message
        dialog.Insert(new TStaticText(
            new TRect(3, 2, dialog.Size.X - 2, dialog.Size.Y - 3),
            msg));

        // Create buttons based on options
        var buttonList = new List<TButton>();
        int x = -2;

        for (int i = 0; i < 4; i++)
        {
            if ((aOptions & (0x0100 << i)) != 0)
            {
                var button = new TButton(
                    new TRect(0, 0, 10, 2),
                    ButtonNames[i],
                    Commands[i],
                    CommandConstants.bfNormal);
                x += button.Size.X + 2;
                buttonList.Add(button);
            }
        }

        // Center buttons horizontally
        x = (dialog.Size.X - x) / 2;

        foreach (var button in buttonList)
        {
            dialog.Insert(button);
            button.MoveTo(x, dialog.Size.Y - 3);
            x += button.Size.X + 2;
        }

        dialog.SelectNext(false);

        ushort result = TProgram.Application?.ExecView(dialog) ?? CommandConstants.cmCancel;

        dialog.Dispose();

        return result;
    }

    /// <summary>
    /// Displays a message box at a specific location.
    /// </summary>
    /// <param name="r">The bounding rectangle for the dialog.</param>
    /// <param name="msg">The message to display.</param>
    /// <param name="aOptions">Options specifying the message type and buttons.</param>
    /// <returns>The command code of the button pressed.</returns>
    public static ushort MessageBoxRect(TRect r, string msg, MessageBoxFlags aOptions)
    {
        return MessageBoxRect(r, msg, (ushort)aOptions);
    }

    /// <summary>
    /// Displays a message box at a specific location with formatted message.
    /// </summary>
    /// <param name="r">The bounding rectangle for the dialog.</param>
    /// <param name="aOptions">Options specifying the message type and buttons.</param>
    /// <param name="format">Format string.</param>
    /// <param name="args">Format arguments.</param>
    /// <returns>The command code of the button pressed.</returns>
    public static ushort MessageBoxRect(TRect r, ushort aOptions, string format, params object[] args)
    {
        string msg = string.Format(format, args);
        return MessageBoxRect(r, msg, aOptions);
    }

    /// <summary>
    /// Displays an input box dialog.
    /// </summary>
    /// <param name="title">The dialog title.</param>
    /// <param name="label">The label for the input field.</param>
    /// <param name="value">Input/output string value.</param>
    /// <param name="limit">Maximum length of input.</param>
    /// <returns>The command code (cmOK or cmCancel).</returns>
    public static ushort InputBox(string title, string label, ref string value, int limit)
    {
        var r = new TRect(0, 0, 60, 8);
        if (TProgram.DeskTop != null)
        {
            r.Move(
                (TProgram.DeskTop.Size.X - r.B.X) / 2,
                (TProgram.DeskTop.Size.Y - r.B.Y) / 2);
        }
        return InputBoxRect(r, title, label, ref value, limit);
    }

    /// <summary>
    /// Displays an input box dialog at a specific location.
    /// </summary>
    /// <param name="bounds">The bounding rectangle for the dialog.</param>
    /// <param name="title">The dialog title.</param>
    /// <param name="label">The label for the input field.</param>
    /// <param name="value">Input/output string value.</param>
    /// <param name="limit">Maximum length of input.</param>
    /// <returns>The command code (cmOK or cmCancel).</returns>
    public static ushort InputBoxRect(TRect bounds, string title, string label, ref string value, int limit)
    {
        var dialog = new TDialog(bounds, title);

        // Create input line (use StrWidth for proper display positioning with Unicode)
        int labelWidth = TStringUtils.StrWidth(label);
        var inputRect = new TRect(4 + labelWidth, 2, dialog.Size.X - 3, 3);
        var control = new TInputLine(inputRect, limit);
        dialog.Insert(control);

        // Create label
        var labelRect = new TRect(2, 2, 3 + labelWidth, 3);
        dialog.Insert(new TLabel(labelRect, label, control));

        // Create OK button
        var okRect = new TRect(
            dialog.Size.X - 24, dialog.Size.Y - 4,
            dialog.Size.X - 14, dialog.Size.Y - 2);
        dialog.Insert(new TButton(okRect, MsgBoxText.OKText, CommandConstants.cmOK, CommandConstants.bfDefault));

        // Create Cancel button
        var cancelRect = new TRect(
            dialog.Size.X - 12, dialog.Size.Y - 4,
            dialog.Size.X - 2, dialog.Size.Y - 2);
        dialog.Insert(new TButton(cancelRect, MsgBoxText.CancelText, CommandConstants.cmCancel, CommandConstants.bfNormal));

        dialog.SelectNext(false);

        // Set initial data
        control.Data = value;

        ushort result = TProgram.Application?.ExecView(dialog) ?? CommandConstants.cmCancel;

        if (result != CommandConstants.cmCancel)
        {
            value = control.Data;
        }

        dialog.Dispose();

        return result;
    }

    /// <summary>
    /// Calculate appropriate dialog rectangle for message text.
    /// </summary>
    private static TRect MakeRect(string text)
    {
        var r = new TRect(0, 0, 40, 9);

        // Calculate text display width (matches upstream strwidth)
        int width = TStringUtils.StrWidth(text);

        // Expand height if text is longer than fits on one line
        int lineWidth = r.B.X - 7;
        int lineHeight = r.B.Y - 6;
        if (width > lineWidth * lineHeight)
        {
            r = new TRect(0, 0, r.B.X, width / lineWidth + 6 + 1);
        }

        // Center on desktop
        if (TProgram.DeskTop != null)
        {
            r.Move(
                (TProgram.DeskTop.Size.X - r.B.X) / 2,
                (TProgram.DeskTop.Size.Y - r.B.Y) / 2);
        }

        return r;
    }
}
