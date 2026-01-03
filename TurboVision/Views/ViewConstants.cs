namespace TurboVision.Views;

/// <summary>
/// View state flags.
/// </summary>
public static class StateFlags
{
    public const ushort sfVisible = 0x001;
    public const ushort sfCursorVis = 0x002;
    public const ushort sfCursorIns = 0x004;
    public const ushort sfShadow = 0x008;
    public const ushort sfActive = 0x010;
    public const ushort sfSelected = 0x020;
    public const ushort sfFocused = 0x040;
    public const ushort sfDragging = 0x080;
    public const ushort sfDisabled = 0x100;
    public const ushort sfModal = 0x200;
    public const ushort sfDefault = 0x400;
    public const ushort sfExposed = 0x800;
}

/// <summary>
/// View option flags.
/// </summary>
public static class OptionFlags
{
    public const ushort ofSelectable = 0x001;
    public const ushort ofTopSelect = 0x002;
    public const ushort ofFirstClick = 0x004;
    public const ushort ofFramed = 0x008;
    public const ushort ofPreProcess = 0x010;
    public const ushort ofPostProcess = 0x020;
    public const ushort ofBuffered = 0x040;
    public const ushort ofTileable = 0x080;
    public const ushort ofCenterX = 0x100;
    public const ushort ofCenterY = 0x200;
    public const ushort ofCentered = 0x300;
    public const ushort ofValidate = 0x400;
}

/// <summary>
/// View grow mode flags.
/// </summary>
public static class GrowFlags
{
    public const byte gfGrowLoX = 0x01;
    public const byte gfGrowLoY = 0x02;
    public const byte gfGrowHiX = 0x04;
    public const byte gfGrowHiY = 0x08;
    public const byte gfGrowAll = 0x0F;
    public const byte gfGrowRel = 0x10;
    public const byte gfFixed = 0x20;
}

/// <summary>
/// View drag mode flags.
/// </summary>
public static class DragFlags
{
    public const byte dmDragMove = 0x01;
    public const byte dmDragGrow = 0x02;
    public const byte dmDragGrowLeft = 0x04;
    public const byte dmLimitLoX = 0x10;
    public const byte dmLimitLoY = 0x20;
    public const byte dmLimitHiX = 0x40;
    public const byte dmLimitHiY = 0x80;
    public const byte dmLimitAll = dmLimitLoX | dmLimitLoY | dmLimitHiX | dmLimitHiY;
}

/// <summary>
/// Window flags.
/// </summary>
public static class WindowFlags
{
    public const byte wfMove = 0x01;
    public const byte wfGrow = 0x02;
    public const byte wfClose = 0x04;
    public const byte wfZoom = 0x08;
}

/// <summary>
/// Help context codes.
/// </summary>
public static class HelpContexts
{
    public const ushort hcNoContext = 0;
    public const ushort hcDragging = 1;
}

/// <summary>
/// ScrollBar part codes.
/// </summary>
public static class ScrollBarParts
{
    public const byte sbLeftArrow = 0;
    public const byte sbRightArrow = 1;
    public const byte sbPageLeft = 2;
    public const byte sbPageRight = 3;
    public const byte sbUpArrow = 4;
    public const byte sbDownArrow = 5;
    public const byte sbPageUp = 6;
    public const byte sbPageDown = 7;
    public const byte sbIndicator = 8;

    // ScrollBar options
    public const ushort sbHorizontal = 0x000;
    public const ushort sbVertical = 0x001;
    public const ushort sbHandleKeyboard = 0x002;
}

/// <summary>
/// Window palette entries.
/// </summary>
public static class WindowPalettes
{
    public const byte wpBlueWindow = 0;
    public const byte wpCyanWindow = 1;
    public const byte wpGrayWindow = 2;
}
