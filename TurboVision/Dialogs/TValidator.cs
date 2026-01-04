using TurboVision.Core;

namespace TurboVision.Dialogs;

/// <summary>
/// Validator status constants.
/// </summary>
public static class ValidatorStatus
{
    public const ushort vsOk = 0;
    public const ushort vsSyntax = 1;
}

/// <summary>
/// Validator option flags.
/// </summary>
[Flags]
public enum ValidatorOptions : ushort
{
    voNone = 0x0000,
    voFill = 0x0001,
    voTransfer = 0x0002,
    voReserved = 0x00FC
}

/// <summary>
/// Transfer operation type for validators.
/// </summary>
public enum TVTransfer
{
    vtDataSize,
    vtSetData,
    vtGetData
}

/// <summary>
/// Abstract base class for input validation.
/// </summary>
public class TValidator : IDisposable
{
    /// <summary>
    /// Status of the validator (vsOk or vsSyntax).
    /// </summary>
    public ushort Status { get; set; }

    /// <summary>
    /// Validator option flags.
    /// </summary>
    public ValidatorOptions Options { get; set; }

    public TValidator()
    {
        Status = ValidatorStatus.vsOk;
        Options = ValidatorOptions.voNone;
    }

    /// <summary>
    /// Displays an error message when validation fails.
    /// Override this method to provide custom error messages.
    /// </summary>
    public virtual void Error()
    {
        // Default implementation does nothing
    }

    /// <summary>
    /// Validates input while the user is typing.
    /// Called for each keystroke to provide real-time validation.
    /// </summary>
    /// <param name="s">The current input string (may be modified if autofill is enabled).</param>
    /// <param name="suppressFill">If true, disables auto-fill behavior.</param>
    /// <returns>True if the input is valid so far, false otherwise.</returns>
    public virtual bool IsValidInput(ref string s, bool suppressFill)
    {
        return true;
    }

    /// <summary>
    /// Validates the final input when the user is done editing.
    /// Called when the input field loses focus or the dialog is closed.
    /// </summary>
    /// <param name="s">The input string to validate.</param>
    /// <returns>True if the input is valid, false otherwise.</returns>
    public virtual bool IsValid(string s)
    {
        return true;
    }

    /// <summary>
    /// Transfers data between the input line and a data record.
    /// Used for validators that change the data format (e.g., numeric validators).
    /// </summary>
    /// <param name="s">The input string.</param>
    /// <param name="buffer">The buffer for data transfer, or null to get size.</param>
    /// <param name="flag">The transfer operation type.</param>
    /// <returns>The size of data transferred, or 0 if not supported.</returns>
    public virtual int Transfer(ref string s, Span<byte> buffer, TVTransfer flag)
    {
        return 0;
    }

    /// <summary>
    /// Validates the input and shows an error message if invalid.
    /// </summary>
    /// <param name="s">The input string to validate.</param>
    /// <returns>True if valid, false otherwise.</returns>
    public bool Validate(string s)
    {
        if (!IsValid(s))
        {
            Error();
            return false;
        }
        return true;
    }

    public virtual void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
