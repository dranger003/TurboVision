using TurboVision.Core;
using TurboVision.Views;

namespace TurboVision.Application;

/// <summary>
/// Container for windows (window manager).
/// </summary>
public class TDeskTop : TGroup
{
    private static readonly byte[] DefaultPalette = [0x01];
    private const char DefaultBackground = '░';

    public TBackground? Background { get; set; }
    public bool TileColumnsFirst { get; protected set; }

    public TDeskTop(TRect bounds) : base(bounds)
    {
        GrowMode = GrowFlags.gfGrowHiX | GrowFlags.gfGrowHiY;

        // Disable buffering - draw directly to screen
        Options &= unchecked((ushort)~OptionFlags.ofBuffered);

        Background = InitBackground(new TRect(0, 0, bounds.B.X - bounds.A.X, bounds.B.Y - bounds.A.Y));
        if (Background != null)
        {
            Insert(Background);
        }
    }

    public static TBackground? InitBackground(TRect r)
    {
        return new TBackground(r, DefaultBackground);
    }

    public void Cascade(TRect r)
    {
        // TODO: Cascade windows within the given rectangle
        var views = new List<TView>();
        ForEach((view, _) =>
        {
            if (view is TWindow && (view.Options & OptionFlags.ofTileable) != 0)
            {
                views.Add(view);
            }
        }, null);

        if (views.Count == 0)
        {
            return;
        }

        int numWindows = views.Count;
        int step = (r.B.X - r.A.X - 20) / numWindows;
        if (step < 2) step = 2;

        for (int i = 0; i < numWindows; i++)
        {
            var view = views[i];
            var bounds = new TRect(
                r.A.X + i * step,
                r.A.Y + i,
                r.B.X - (numWindows - 1 - i) * step,
                r.B.Y - (numWindows - 1 - i)
            );
            view.Locate(ref bounds);
        }
    }

    public override void HandleEvent(ref TEvent ev)
    {
        base.HandleEvent(ref ev);

        if (ev.What == EventConstants.evCommand)
        {
            switch (ev.Message.Command)
            {
                case CommandConstants.cmNext:
                    ClearEvent(ref ev);
                    SelectNext(false);
                    break;
                case CommandConstants.cmPrev:
                    ClearEvent(ref ev);
                    SelectNext(true);
                    break;
            }
        }
        else if (ev.What == EventConstants.evBroadcast)
        {
            if (ev.Message.Command == CommandConstants.cmSelectWindowNum)
            {
                // TODO: Select window by number
            }
        }
    }

    public void Tile(TRect r)
    {
        // TODO: Tile windows within the given rectangle
        var views = new List<TView>();
        ForEach((view, _) =>
        {
            if (view is TWindow && (view.Options & OptionFlags.ofTileable) != 0)
            {
                views.Add(view);
            }
        }, null);

        if (views.Count == 0)
        {
            return;
        }

        int numWindows = views.Count;
        int cols = (int)Math.Ceiling(Math.Sqrt(numWindows));
        int rows = (numWindows + cols - 1) / cols;

        int width = (r.B.X - r.A.X) / cols;
        int height = (r.B.Y - r.A.Y) / rows;

        for (int i = 0; i < numWindows; i++)
        {
            var view = views[i];
            int col = TileColumnsFirst ? i / rows : i % cols;
            int row = TileColumnsFirst ? i % rows : i / cols;

            var bounds = new TRect(
                r.A.X + col * width,
                r.A.Y + row * height,
                r.A.X + (col + 1) * width,
                r.A.Y + (row + 1) * height
            );
            view.Locate(ref bounds);
        }
    }

    public virtual void TileError()
    {
        // Override to handle tiling errors
    }

    public override void ShutDown()
    {
        Background = null;
        base.ShutDown();
    }
}
