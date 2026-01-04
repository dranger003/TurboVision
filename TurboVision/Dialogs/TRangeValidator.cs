using System.Globalization;
using TurboVision.Core;

namespace TurboVision.Dialogs;

/// <summary>
/// Numeric range validator.
/// Validates that input is a number within a specified range.
/// </summary>
public class TRangeValidator : TFilterValidator
{
    private const string ValidUnsignedChars = "+0123456789";
    private const string ValidSignedChars = "+-0123456789";
    private static readonly string ErrorMsg = "Value not in the range {0} to {1}";

    /// <summary>
    /// Minimum allowed value.
    /// </summary>
    protected long Min { get; set; }

    /// <summary>
    /// Maximum allowed value.
    /// </summary>
    protected long Max { get; set; }

    /// <summary>
    /// Creates a range validator for the specified numeric range.
    /// </summary>
    /// <param name="min">Minimum allowed value.</param>
    /// <param name="max">Maximum allowed value.</param>
    public TRangeValidator(long min, long max)
        : base(min >= 0 ? ValidUnsignedChars : ValidSignedChars)
    {
        Min = min;
        Max = max;
    }

    public override void Error()
    {
        MsgBox.MessageBox(
            MessageBoxFlags.mfError | MessageBoxFlags.mfOKButton,
            ErrorMsg,
            Min, Max);
    }

    public override bool IsValid(string s)
    {
        if (!base.IsValid(s))
        {
            return false;
        }

        if (long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out long value))
        {
            return value >= Min && value <= Max;
        }

        return false;
    }

    public override int Transfer(ref string s, Span<byte> buffer, TVTransfer flag)
    {
        if ((Options & ValidatorOptions.voTransfer) != 0)
        {
            switch (flag)
            {
                case TVTransfer.vtDataSize:
                    return sizeof(long);

                case TVTransfer.vtGetData:
                    if (buffer.Length >= sizeof(long) && long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out long getValue))
                    {
                        BitConverter.TryWriteBytes(buffer, getValue);
                    }
                    return sizeof(long);

                case TVTransfer.vtSetData:
                    if (buffer.Length >= sizeof(long))
                    {
                        long setValue = BitConverter.ToInt64(buffer);
                        s = setValue.ToString(CultureInfo.InvariantCulture);
                    }
                    return sizeof(long);
            }
            return sizeof(long);
        }
        return 0;
    }
}
