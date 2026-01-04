using TurboVision.Core;

namespace TurboVision.Dialogs;

/// <summary>
/// Character filter validator.
/// Only allows characters that are in the valid character set.
/// </summary>
public class TFilterValidator : TValidator
{
    private static readonly string ErrorMsg = "Invalid character in input";

    /// <summary>
    /// The set of valid characters.
    /// </summary>
    protected string ValidChars { get; set; }

    /// <summary>
    /// Creates a filter validator with the specified valid characters.
    /// </summary>
    /// <param name="validChars">String containing all valid characters.</param>
    public TFilterValidator(string validChars) : base()
    {
        ValidChars = validChars ?? "";
    }

    public override void Error()
    {
        MsgBox.MessageBox(ErrorMsg, (ushort)(MessageBoxFlags.mfError | MessageBoxFlags.mfOKButton));
    }

    public override bool IsValid(string s)
    {
        return AllCharsValid(s);
    }

    public override bool IsValidInput(ref string s, bool suppressFill)
    {
        return AllCharsValid(s);
    }

    /// <summary>
    /// Checks if all characters in the string are in the valid character set.
    /// </summary>
    private bool AllCharsValid(string s)
    {
        foreach (char c in s)
        {
            if (!ValidChars.Contains(c))
            {
                return false;
            }
        }
        return true;
    }
}
