using System.Text.Json.Serialization;
using TurboVision.Core;
using TurboVision.Views;

namespace TurboVision.Editors;

/// <summary>
/// In-memory text editor control for dialogs.
/// Supports data exchange via GetData/SetData for dialog integration.
/// </summary>
public class TMemo : TEditor
{
    /// <summary>
    /// Type name for streaming identification.
    /// </summary>
    public new const string TypeName = "TMemo";

    /// <inheritdoc/>
    [JsonIgnore]
    public override string StreamableName => TypeName;

    private static readonly byte[] DefaultPalette = [0x1A, 0x1B];

    public TMemo(TRect bounds, TScrollBar? hScrollBar, TScrollBar? vScrollBar,
                 TIndicator? indicator, ushort bufSize)
        : base(bounds, hScrollBar, vScrollBar, indicator, bufSize)
    {
    }

    /// <summary>
    /// Returns the size of the data buffer required for GetData/SetData.
    /// </summary>
    public override int DataSize()
    {
        return (int)BufSize + sizeof(ushort);
    }

    /// <summary>
    /// Copies the memo content to the provided buffer.
    /// </summary>
    public override void GetData(Span<byte> rec)
    {
        if (rec.Length < DataSize())
            return;

        // Write length (2 bytes)
        rec[0] = (byte)(BufLen & 0xFF);
        rec[1] = (byte)((BufLen >> 8) & 0xFF);

        // Copy buffer content (concatenating the two parts around the gap)
        int offset = 2;
        for (uint i = 0; i < CurPtr && offset < rec.Length; i++, offset++)
        {
            rec[offset] = (byte)Buffer![i];
        }
        for (uint i = CurPtr + GapLen; i < BufSize && offset < rec.Length; i++, offset++)
        {
            rec[offset] = (byte)Buffer![i];
        }

        // Zero-fill remaining space
        while (offset < 2 + BufSize)
        {
            rec[offset++] = 0;
        }
    }

    /// <summary>
    /// Sets the memo content from the provided buffer.
    /// </summary>
    public override void SetData(ReadOnlySpan<byte> rec)
    {
        if (rec.Length < 2)
            return;

        ushort length = (ushort)(rec[0] | (rec[1] << 8));
        if (length > BufSize)
            length = (ushort)BufSize;

        if (SetBufSize(length))
        {
            // Copy data to the end of buffer (before gap)
            for (int i = 0; i < length && i + 2 < rec.Length; i++)
            {
                Buffer![BufSize - length + i] = (char)rec[2 + i];
            }
            SetBufLen(length);
        }
    }

    public override TPalette? GetPalette()
    {
        return new TPalette(DefaultPalette);
    }

    public override void HandleEvent(ref TEvent ev)
    {
        // Skip Tab key - let it navigate between controls
        if (ev.What == EventConstants.evKeyDown && ev.KeyDown.KeyCode == KeyConstants.kbTab)
        {
            return;
        }
        base.HandleEvent(ref ev);
    }
}
