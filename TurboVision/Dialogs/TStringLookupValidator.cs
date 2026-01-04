using TurboVision.Core;

namespace TurboVision.Dialogs;

/// <summary>
/// String list lookup validator.
/// Validates input by checking if it exists in a list of valid strings.
/// </summary>
public class TStringLookupValidator : TLookupValidator
{
    private static readonly string ErrorMsg = "Input is not in list of valid strings";

    /// <summary>
    /// The collection of valid strings.
    /// </summary>
    protected IList<string>? Strings { get; set; }

    /// <summary>
    /// Creates a string lookup validator with the specified list of valid strings.
    /// </summary>
    /// <param name="strings">Collection of valid strings.</param>
    public TStringLookupValidator(IList<string>? strings) : base()
    {
        Strings = strings;
    }

    public override void Error()
    {
        MsgBox.MessageBox(ErrorMsg, (ushort)(MessageBoxFlags.mfError | MessageBoxFlags.mfOKButton));
    }

    public override bool Lookup(string s)
    {
        if (Strings == null)
        {
            return true;
        }

        foreach (var str in Strings)
        {
            if (string.Equals(str, s, StringComparison.Ordinal))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Replaces the current list of valid strings with a new list.
    /// </summary>
    /// <param name="strings">The new list of valid strings.</param>
    public void NewStringList(IList<string>? strings)
    {
        Strings = strings;
    }

    public override void Dispose()
    {
        Strings = null;
        base.Dispose();
    }
}
