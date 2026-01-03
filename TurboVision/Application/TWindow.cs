using TurboVision.Core;
using TurboVision.Views;

namespace TurboVision.Application;

/// <summary>
/// Movable, resizable window with frame.
/// </summary>
public class TWindow : TGroup
{
    private static readonly byte[] DefaultPalette =
    [
        0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F
    ];

    public byte Flags { get; set; } = WindowFlags.wfMove | WindowFlags.wfGrow | WindowFlags.wfClose | WindowFlags.wfZoom;
    public TRect ZoomRect { get; set; }
    public short Number { get; set; }
    public short Palette { get; set; } = WindowPalettes.wpBlueWindow;
    public TFrame? Frame { get; set; }
    public string? Title { get; set; }

    public TWindow(TRect bounds, string? title, short number) : base(bounds)
    {
        Title = title;
        Number = number;
        Options |= OptionFlags.ofSelectable | OptionFlags.ofTopSelect;
        State |= StateFlags.sfShadow;
        GrowMode = GrowFlags.gfGrowAll | GrowFlags.gfGrowRel;
        ZoomRect = bounds;

        Frame = InitFrame(new TRect(0, 0, bounds.B.X - bounds.A.X, bounds.B.Y - bounds.A.Y));
        if (Frame != null)
        {
            Insert(Frame);
        }
    }

    public static TFrame? InitFrame(TRect r)
    {
        return new TFrame(r);
    }

    public virtual void Close()
    {
        if (Valid(CommandConstants.cmClose))
        {
            Owner?.Remove(this);
            Dispose();
        }
    }

    public override TPalette? GetPalette()
    {
        return Palette switch
        {
            WindowPalettes.wpCyanWindow => new TPalette((ReadOnlySpan<byte>)[0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17]),
            WindowPalettes.wpGrayWindow => new TPalette((ReadOnlySpan<byte>)[0x18, 0x19, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F]),
            _ => new TPalette(DefaultPalette)
        };
    }

    public virtual string? GetTitle(short maxSize)
    {
        return Title;
    }

    public override void HandleEvent(ref TEvent ev)
    {
        base.HandleEvent(ref ev);

        if (ev.What == EventConstants.evCommand)
        {
            switch (ev.Message.Command)
            {
                case CommandConstants.cmClose:
                    if ((Flags & WindowFlags.wfClose) != 0)
                    {
                        ClearEvent(ref ev);
                        Close();
                    }
                    break;
                case CommandConstants.cmZoom:
                    if ((Flags & WindowFlags.wfZoom) != 0)
                    {
                        ClearEvent(ref ev);
                        Zoom();
                    }
                    break;
                case CommandConstants.cmResize:
                    if ((Flags & WindowFlags.wfGrow) != 0)
                    {
                        // TODO: Implement resize
                        ClearEvent(ref ev);
                    }
                    break;
            }
        }
        else if (ev.What == EventConstants.evKeyDown)
        {
            // TODO: Handle keyboard shortcuts
        }
    }

    public override void SetState(ushort aState, bool enable)
    {
        base.SetState(aState, enable);

        if ((aState & StateFlags.sfSelected) != 0)
        {
            SetState(StateFlags.sfActive, enable);
            Frame?.SetState(StateFlags.sfActive, enable);
        }
    }

    public override void SizeLimits(out TPoint min, out TPoint max)
    {
        base.SizeLimits(out min, out max);
        min = new TPoint(16, 6);
    }

    public TScrollBar? StandardScrollBar(ushort options)
    {
        TRect r;
        if ((options & ScrollBarParts.sbVertical) != 0)
        {
            r = new TRect(Size.X - 1, 1, Size.X, Size.Y - 1);
        }
        else
        {
            r = new TRect(2, Size.Y - 1, Size.X - 2, Size.Y);
        }

        var sb = new TScrollBar(r);

        if ((options & ScrollBarParts.sbHandleKeyboard) != 0)
        {
            sb.Options |= OptionFlags.ofPostProcess;
        }

        Insert(sb);
        return sb;
    }

    public virtual void Zoom()
    {
        var r = ZoomRect;
        if (Owner != null)
        {
            if (r.Equals(GetBounds()))
            {
                r = new TRect(0, 0, Owner.Size.X, Owner.Size.Y);
            }
            else
            {
                ZoomRect = GetBounds();
            }
            Locate(ref r);
        }
    }

    public override void ShutDown()
    {
        Frame = null;
        Title = null;
        base.ShutDown();
    }
}
