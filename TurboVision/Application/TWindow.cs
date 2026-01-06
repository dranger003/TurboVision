using System.Text.Json.Serialization;
using TurboVision.Core;
using TurboVision.Views;

namespace TurboVision.Application;

/// <summary>
/// Movable, resizable window with frame.
/// </summary>
public class TWindow : TGroup
{
    /// <summary>
    /// Type name for streaming identification.
    /// </summary>
    public new const string TypeName = "TWindow";

    /// <inheritdoc/>
    [JsonIgnore]
    public override string StreamableName => TypeName;

    private static readonly byte[] DefaultPalette =
    [
        0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F
    ];

    public byte Flags { get; set; } = WindowFlags.wfMove | WindowFlags.wfGrow | WindowFlags.wfClose | WindowFlags.wfZoom;
    public TRect ZoomRect { get; set; }
    public short Number { get; set; }
    public short Palette { get; set; } = WindowPalettes.wpBlueWindow;

    /// <summary>
    /// Reference to the frame view. Not serialized directly - the frame is included
    /// in SubViews and this reference is reconstructed after deserialization.
    /// </summary>
    [JsonIgnore]
    public TFrame? Frame { get; set; }

    public string? Title { get; set; }

    public TWindow(TRect bounds, string? title, short number) : base(bounds)
    {
        Title = title;
        Number = number;
        Options |= OptionFlags.ofSelectable | OptionFlags.ofTopSelect;
        // Buffering is now supported via hierarchical WriteBuf (TVWrite)
        // Child views write to this window's buffer, which propagates up to the screen
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
            Frame = null; // so we don't try to use the frame after it's been deleted
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
                case CommandConstants.cmResize:
                    if ((Flags & (WindowFlags.wfMove | WindowFlags.wfGrow)) != 0)
                    {
                        if (Owner != null)
                        {
                            var limits = Owner.GetExtent();
                            SizeLimits(out var min, out var max);
                            DragView(ev, (byte)(DragMode | (Flags & (WindowFlags.wfMove | WindowFlags.wfGrow))),
                                limits, min, max);
                        }
                        ClearEvent(ref ev);
                    }
                    break;
                case CommandConstants.cmClose:
                    if ((Flags & WindowFlags.wfClose) != 0 &&
                        (ev.Message.InfoPtr == null || ReferenceEquals(ev.Message.InfoPtr, this)))
                    {
                        ClearEvent(ref ev);
                        if ((State & StateFlags.sfModal) == 0)
                        {
                            Close();
                        }
                        else
                        {
                            ev.What = EventConstants.evCommand;
                            ev.Message.Command = CommandConstants.cmCancel;
                            PutEvent(ev);
                            ClearEvent(ref ev);
                        }
                    }
                    break;
                case CommandConstants.cmZoom:
                    if ((Flags & WindowFlags.wfZoom) != 0 &&
                        (ev.Message.InfoPtr == null || ReferenceEquals(ev.Message.InfoPtr, this)))
                    {
                        Zoom();
                        ClearEvent(ref ev);
                    }
                    break;
            }
        }
        else if (ev.What == EventConstants.evKeyDown)
        {
            switch (ev.KeyDown.KeyCode)
            {
                case KeyConstants.kbTab:
                    FocusNext(false);
                    ClearEvent(ref ev);
                    break;
                case KeyConstants.kbShiftTab:
                    FocusNext(true);
                    ClearEvent(ref ev);
                    break;
            }
        }
        else if (ev.What == EventConstants.evBroadcast)
        {
            if (ev.Message.Command == CommandConstants.cmSelectWindowNum &&
                ev.Message.InfoInt == Number &&
                (Options & OptionFlags.ofSelectable) != 0)
            {
                Select();
                ClearEvent(ref ev);
            }
        }
    }

    public override void SetState(ushort aState, bool enable)
    {
        base.SetState(aState, enable);

        if ((aState & StateFlags.sfSelected) != 0)
        {
            SetState(StateFlags.sfActive, enable);
            Frame?.SetState(StateFlags.sfActive, enable);

            var windowCommands = new TCommandSet();
            windowCommands.EnableCmd(CommandConstants.cmNext);
            windowCommands.EnableCmd(CommandConstants.cmPrev);

            if ((Flags & (WindowFlags.wfGrow | WindowFlags.wfMove)) != 0)
            {
                windowCommands.EnableCmd(CommandConstants.cmResize);
            }
            if ((Flags & WindowFlags.wfClose) != 0)
            {
                windowCommands.EnableCmd(CommandConstants.cmClose);
            }
            if ((Flags & WindowFlags.wfZoom) != 0)
            {
                windowCommands.EnableCmd(CommandConstants.cmZoom);
            }

            if (enable)
            {
                EnableCommands(windowCommands);
            }
            else
            {
                DisableCommands(windowCommands);
            }
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
        SizeLimits(out _, out var maxSize);
        if (Size.X != maxSize.X || Size.Y != maxSize.Y)
        {
            ZoomRect = GetBounds();
            var r = new TRect(0, 0, maxSize.X, maxSize.Y);
            Locate(ref r);
        }
        else
        {
            var r = ZoomRect;
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
