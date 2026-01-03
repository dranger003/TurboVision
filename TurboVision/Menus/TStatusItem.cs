using TurboVision.Core;

namespace TurboVision.Menus;

/// <summary>
/// Individual status line item.
/// </summary>
public class TStatusItem
{
    public TStatusItem? Next { get; set; }
    public string? Text { get; set; }
    public TKey KeyCode { get; set; }
    public ushort Command { get; set; }

    public TStatusItem(string? text, TKey keyCode, ushort command, TStatusItem? next = null)
    {
        Text = text;
        KeyCode = keyCode;
        Command = command;
        Next = next;
    }
}
