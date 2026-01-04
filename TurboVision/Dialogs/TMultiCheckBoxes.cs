using TurboVision.Core;

namespace TurboVision.Dialogs;

/// <summary>
/// Flag constants for TMultiCheckBoxes specifying bit width per item.
/// High byte = bit shift per item, low byte = bit mask.
/// </summary>
public static class ClusterFlags
{
    /// <summary>1 bit per item (2 states: 0, 1)</summary>
    public const ushort cfOneBit = 0x0101;

    /// <summary>2 bits per item (4 states: 0, 1, 2, 3)</summary>
    public const ushort cfTwoBits = 0x0203;

    /// <summary>4 bits per item (16 states: 0-15)</summary>
    public const ushort cfFourBits = 0x040F;

    /// <summary>8 bits per item (256 states: 0-255)</summary>
    public const ushort cfEightBits = 0x08FF;
}

/// <summary>
/// Multi-state checkbox group where each item can have multiple states.
/// Each checkbox cycles through states (defined by selRange) when pressed.
/// The bit width per item is controlled by the flags parameter.
/// </summary>
public class TMultiCheckBoxes : TCluster
{
    private const string Button = " [ ] ";

    private readonly byte _selRange;
    private readonly ushort _flags;
    private readonly string _states;

    /// <summary>
    /// Creates a new TMultiCheckBoxes control.
    /// </summary>
    /// <param name="bounds">The bounding rectangle.</param>
    /// <param name="strings">The list of item strings (linked list).</param>
    /// <param name="selRange">Number of states per item (e.g., 2 for binary, 3 for tri-state).</param>
    /// <param name="flags">Bit width flags (e.g., cfOneBit, cfTwoBits). High byte = bits per item, low byte = bit mask.</param>
    /// <param name="states">String of characters representing each state (e.g., " X" for unchecked/checked).</param>
    public TMultiCheckBoxes(TRect bounds, TSItem? strings, byte selRange, ushort flags, string states)
        : base(bounds, strings)
    {
        _selRange = selRange;
        _flags = flags;
        _states = states;
    }

    public override void Draw()
    {
        DrawMultiBox(Button, _states);
    }

    public override int DataSize()
    {
        return sizeof(uint);
    }

    public override byte MultiMark(int item)
    {
        int flagLow = _flags & 0xFF;
        int flagHigh = (_flags >> 8) * item;
        return (byte)((Value & ((uint)flagLow << flagHigh)) >> flagHigh);
    }

    public override void GetData(Span<byte> rec)
    {
        if (rec.Length >= sizeof(uint))
        {
            BitConverter.TryWriteBytes(rec, Value);
        }
        DrawView();
    }

    public override void Press(int item)
    {
        int flagLow = _flags & 0xFF;
        int flagHigh = (_flags >> 8) * item;

        int curState = (int)((Value & ((uint)flagLow << flagHigh)) >> flagHigh);

        curState++;

        if (curState >= _selRange)
        {
            curState = 0;
        }

        Value = (uint)((Value & ~((uint)flagLow << flagHigh)) | ((uint)curState << flagHigh));
    }

    public override void SetData(ReadOnlySpan<byte> rec)
    {
        if (rec.Length >= sizeof(uint))
        {
            Value = BitConverter.ToUInt32(rec);
        }
        DrawView();
    }
}
