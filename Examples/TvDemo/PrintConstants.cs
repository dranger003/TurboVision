using System.Text;
using TurboVision.Core;

namespace TvDemo;

/// <summary>
/// Provides functionality to print TurboVision constants (key codes, event codes,
/// control key states, mouse buttons, etc.) as symbolic names.
/// Port of prntcnst.cpp from upstream tvision.
/// </summary>
public static class PrintConstants
{
    public record struct TConstant(ushort Value, string Name);

    private static TConstant NM(ushort value, string name) => new(value, name);

    // Key codes table - only constants that exist in KeyConstants
    private static readonly TConstant[] KeyCodes =
    [
        NM(KeyConstants.kbCtrlA, nameof(KeyConstants.kbCtrlA)),
        NM(KeyConstants.kbCtrlB, nameof(KeyConstants.kbCtrlB)),
        NM(KeyConstants.kbCtrlC, nameof(KeyConstants.kbCtrlC)),
        NM(KeyConstants.kbCtrlD, nameof(KeyConstants.kbCtrlD)),
        NM(KeyConstants.kbCtrlE, nameof(KeyConstants.kbCtrlE)),
        NM(KeyConstants.kbCtrlF, nameof(KeyConstants.kbCtrlF)),
        NM(KeyConstants.kbCtrlG, nameof(KeyConstants.kbCtrlG)),
        NM(KeyConstants.kbCtrlH, nameof(KeyConstants.kbCtrlH)),
        NM(KeyConstants.kbCtrlI, nameof(KeyConstants.kbCtrlI)),
        NM(KeyConstants.kbCtrlJ, nameof(KeyConstants.kbCtrlJ)),
        NM(KeyConstants.kbCtrlK, nameof(KeyConstants.kbCtrlK)),
        NM(KeyConstants.kbCtrlL, nameof(KeyConstants.kbCtrlL)),
        NM(KeyConstants.kbCtrlM, nameof(KeyConstants.kbCtrlM)),
        NM(KeyConstants.kbCtrlN, nameof(KeyConstants.kbCtrlN)),
        NM(KeyConstants.kbCtrlO, nameof(KeyConstants.kbCtrlO)),
        NM(KeyConstants.kbCtrlP, nameof(KeyConstants.kbCtrlP)),
        NM(KeyConstants.kbCtrlQ, nameof(KeyConstants.kbCtrlQ)),
        NM(KeyConstants.kbCtrlR, nameof(KeyConstants.kbCtrlR)),
        NM(KeyConstants.kbCtrlS, nameof(KeyConstants.kbCtrlS)),
        NM(KeyConstants.kbCtrlT, nameof(KeyConstants.kbCtrlT)),
        NM(KeyConstants.kbCtrlU, nameof(KeyConstants.kbCtrlU)),
        NM(KeyConstants.kbCtrlV, nameof(KeyConstants.kbCtrlV)),
        NM(KeyConstants.kbCtrlW, nameof(KeyConstants.kbCtrlW)),
        NM(KeyConstants.kbCtrlX, nameof(KeyConstants.kbCtrlX)),
        NM(KeyConstants.kbCtrlY, nameof(KeyConstants.kbCtrlY)),
        NM(KeyConstants.kbCtrlZ, nameof(KeyConstants.kbCtrlZ)),
        NM(KeyConstants.kbEsc, nameof(KeyConstants.kbEsc)),
        NM(KeyConstants.kbCtrlIns, nameof(KeyConstants.kbCtrlIns)),
        NM(KeyConstants.kbShiftIns, nameof(KeyConstants.kbShiftIns)),
        NM(KeyConstants.kbCtrlDel, nameof(KeyConstants.kbCtrlDel)),
        NM(KeyConstants.kbShiftDel, nameof(KeyConstants.kbShiftDel)),
        NM(KeyConstants.kbBack, nameof(KeyConstants.kbBack)),
        NM(KeyConstants.kbCtrlBack, nameof(KeyConstants.kbCtrlBack)),
        NM(KeyConstants.kbShiftTab, nameof(KeyConstants.kbShiftTab)),
        NM(KeyConstants.kbTab, nameof(KeyConstants.kbTab)),
        NM(KeyConstants.kbAltQ, nameof(KeyConstants.kbAltQ)),
        NM(KeyConstants.kbAltW, nameof(KeyConstants.kbAltW)),
        NM(KeyConstants.kbAltE, nameof(KeyConstants.kbAltE)),
        NM(KeyConstants.kbAltR, nameof(KeyConstants.kbAltR)),
        NM(KeyConstants.kbAltT, nameof(KeyConstants.kbAltT)),
        NM(KeyConstants.kbAltY, nameof(KeyConstants.kbAltY)),
        NM(KeyConstants.kbAltU, nameof(KeyConstants.kbAltU)),
        NM(KeyConstants.kbAltI, nameof(KeyConstants.kbAltI)),
        NM(KeyConstants.kbAltO, nameof(KeyConstants.kbAltO)),
        NM(KeyConstants.kbAltP, nameof(KeyConstants.kbAltP)),
        NM(KeyConstants.kbEnter, nameof(KeyConstants.kbEnter)),
        NM(KeyConstants.kbAltA, nameof(KeyConstants.kbAltA)),
        NM(KeyConstants.kbAltS, nameof(KeyConstants.kbAltS)),
        NM(KeyConstants.kbAltD, nameof(KeyConstants.kbAltD)),
        NM(KeyConstants.kbAltF, nameof(KeyConstants.kbAltF)),
        NM(KeyConstants.kbAltG, nameof(KeyConstants.kbAltG)),
        NM(KeyConstants.kbAltH, nameof(KeyConstants.kbAltH)),
        NM(KeyConstants.kbAltJ, nameof(KeyConstants.kbAltJ)),
        NM(KeyConstants.kbAltK, nameof(KeyConstants.kbAltK)),
        NM(KeyConstants.kbAltL, nameof(KeyConstants.kbAltL)),
        NM(KeyConstants.kbAltZ, nameof(KeyConstants.kbAltZ)),
        NM(KeyConstants.kbAltX, nameof(KeyConstants.kbAltX)),
        NM(KeyConstants.kbAltC, nameof(KeyConstants.kbAltC)),
        NM(KeyConstants.kbAltV, nameof(KeyConstants.kbAltV)),
        NM(KeyConstants.kbAltB, nameof(KeyConstants.kbAltB)),
        NM(KeyConstants.kbAltN, nameof(KeyConstants.kbAltN)),
        NM(KeyConstants.kbAltM, nameof(KeyConstants.kbAltM)),
        NM(KeyConstants.kbF1, nameof(KeyConstants.kbF1)),
        NM(KeyConstants.kbF2, nameof(KeyConstants.kbF2)),
        NM(KeyConstants.kbF3, nameof(KeyConstants.kbF3)),
        NM(KeyConstants.kbF4, nameof(KeyConstants.kbF4)),
        NM(KeyConstants.kbF5, nameof(KeyConstants.kbF5)),
        NM(KeyConstants.kbF6, nameof(KeyConstants.kbF6)),
        NM(KeyConstants.kbF7, nameof(KeyConstants.kbF7)),
        NM(KeyConstants.kbF8, nameof(KeyConstants.kbF8)),
        NM(KeyConstants.kbF9, nameof(KeyConstants.kbF9)),
        NM(KeyConstants.kbF10, nameof(KeyConstants.kbF10)),
        NM(KeyConstants.kbHome, nameof(KeyConstants.kbHome)),
        NM(KeyConstants.kbUp, nameof(KeyConstants.kbUp)),
        NM(KeyConstants.kbPgUp, nameof(KeyConstants.kbPgUp)),
        NM(KeyConstants.kbLeft, nameof(KeyConstants.kbLeft)),
        NM(KeyConstants.kbRight, nameof(KeyConstants.kbRight)),
        NM(KeyConstants.kbEnd, nameof(KeyConstants.kbEnd)),
        NM(KeyConstants.kbDown, nameof(KeyConstants.kbDown)),
        NM(KeyConstants.kbPgDn, nameof(KeyConstants.kbPgDn)),
        NM(KeyConstants.kbIns, nameof(KeyConstants.kbIns)),
        NM(KeyConstants.kbDel, nameof(KeyConstants.kbDel)),
        NM(KeyConstants.kbCtrlF1, nameof(KeyConstants.kbCtrlF1)),
        NM(KeyConstants.kbCtrlF2, nameof(KeyConstants.kbCtrlF2)),
        NM(KeyConstants.kbCtrlF3, nameof(KeyConstants.kbCtrlF3)),
        NM(KeyConstants.kbCtrlF4, nameof(KeyConstants.kbCtrlF4)),
        NM(KeyConstants.kbCtrlF5, nameof(KeyConstants.kbCtrlF5)),
        NM(KeyConstants.kbCtrlF6, nameof(KeyConstants.kbCtrlF6)),
        NM(KeyConstants.kbCtrlF7, nameof(KeyConstants.kbCtrlF7)),
        NM(KeyConstants.kbCtrlF8, nameof(KeyConstants.kbCtrlF8)),
        NM(KeyConstants.kbCtrlF9, nameof(KeyConstants.kbCtrlF9)),
        NM(KeyConstants.kbCtrlF10, nameof(KeyConstants.kbCtrlF10)),
        NM(KeyConstants.kbAltF1, nameof(KeyConstants.kbAltF1)),
        NM(KeyConstants.kbAltF2, nameof(KeyConstants.kbAltF2)),
        NM(KeyConstants.kbAltF3, nameof(KeyConstants.kbAltF3)),
        NM(KeyConstants.kbAltF4, nameof(KeyConstants.kbAltF4)),
        NM(KeyConstants.kbAltF5, nameof(KeyConstants.kbAltF5)),
        NM(KeyConstants.kbAltF6, nameof(KeyConstants.kbAltF6)),
        NM(KeyConstants.kbAltF7, nameof(KeyConstants.kbAltF7)),
        NM(KeyConstants.kbAltF8, nameof(KeyConstants.kbAltF8)),
        NM(KeyConstants.kbAltF9, nameof(KeyConstants.kbAltF9)),
        NM(KeyConstants.kbAltF10, nameof(KeyConstants.kbAltF10)),
        NM(KeyConstants.kbCtrlLeft, nameof(KeyConstants.kbCtrlLeft)),
        NM(KeyConstants.kbCtrlRight, nameof(KeyConstants.kbCtrlRight)),
        NM(KeyConstants.kbCtrlEnd, nameof(KeyConstants.kbCtrlEnd)),
        NM(KeyConstants.kbCtrlPgDn, nameof(KeyConstants.kbCtrlPgDn)),
        NM(KeyConstants.kbCtrlHome, nameof(KeyConstants.kbCtrlHome)),
        NM(KeyConstants.kbAlt1, nameof(KeyConstants.kbAlt1)),
        NM(KeyConstants.kbAlt2, nameof(KeyConstants.kbAlt2)),
        NM(KeyConstants.kbAlt3, nameof(KeyConstants.kbAlt3)),
        NM(KeyConstants.kbAlt4, nameof(KeyConstants.kbAlt4)),
        NM(KeyConstants.kbAlt5, nameof(KeyConstants.kbAlt5)),
        NM(KeyConstants.kbAlt6, nameof(KeyConstants.kbAlt6)),
        NM(KeyConstants.kbAlt7, nameof(KeyConstants.kbAlt7)),
        NM(KeyConstants.kbAlt8, nameof(KeyConstants.kbAlt8)),
        NM(KeyConstants.kbAlt9, nameof(KeyConstants.kbAlt9)),
        NM(KeyConstants.kbAlt0, nameof(KeyConstants.kbAlt0)),
        NM(KeyConstants.kbCtrlPgUp, nameof(KeyConstants.kbCtrlPgUp)),
        NM(KeyConstants.kbNoKey, nameof(KeyConstants.kbNoKey)),
        NM(KeyConstants.kbAltBack, nameof(KeyConstants.kbAltBack)),
        NM(KeyConstants.kbF11, nameof(KeyConstants.kbF11)),
        NM(KeyConstants.kbF12, nameof(KeyConstants.kbF12)),
        NM(KeyConstants.kbCtrlUp, nameof(KeyConstants.kbCtrlUp)),
        NM(KeyConstants.kbCtrlDown, nameof(KeyConstants.kbCtrlDown)),
    ];

    // Control key state flags table
    private static readonly TConstant[] ControlKeyStateFlags =
    [
        NM(KeyConstants.kbShift, nameof(KeyConstants.kbShift)),
        NM(KeyConstants.kbScrollState, nameof(KeyConstants.kbScrollState)),
        NM(KeyConstants.kbLeftCtrl, nameof(KeyConstants.kbLeftCtrl)),
        NM(KeyConstants.kbRightCtrl, nameof(KeyConstants.kbRightCtrl)),
        NM(KeyConstants.kbLeftAlt, nameof(KeyConstants.kbLeftAlt)),
        NM(KeyConstants.kbRightAlt, nameof(KeyConstants.kbRightAlt)),
        NM(KeyConstants.kbNumState, nameof(KeyConstants.kbNumState)),
        NM(KeyConstants.kbCapsState, nameof(KeyConstants.kbCapsState)),
        NM(KeyConstants.kbInsState, nameof(KeyConstants.kbInsState)),
        NM(KeyConstants.kbEnhanced, nameof(KeyConstants.kbEnhanced)),
        NM(KeyConstants.kbPaste, nameof(KeyConstants.kbPaste)),
    ];

    // Event codes table
    private static readonly TConstant[] EventCodes =
    [
        NM(EventConstants.evNothing, nameof(EventConstants.evNothing)),
        NM(EventConstants.evMouseDown, nameof(EventConstants.evMouseDown)),
        NM(EventConstants.evMouseUp, nameof(EventConstants.evMouseUp)),
        NM(EventConstants.evMouseMove, nameof(EventConstants.evMouseMove)),
        NM(EventConstants.evMouseAuto, nameof(EventConstants.evMouseAuto)),
        NM(EventConstants.evMouseWheel, nameof(EventConstants.evMouseWheel)),
        NM(EventConstants.evKeyDown, nameof(EventConstants.evKeyDown)),
        NM(EventConstants.evCommand, nameof(EventConstants.evCommand)),
        NM(EventConstants.evBroadcast, nameof(EventConstants.evBroadcast)),
        NM(EventConstants.evMouse, nameof(EventConstants.evMouse)),
        NM(EventConstants.evKeyboard, nameof(EventConstants.evKeyboard)),
        NM(EventConstants.evMessage, nameof(EventConstants.evMessage)),
    ];

    // Mouse button flags table (from EventConstants, not MouseConstants)
    private static readonly TConstant[] MouseButtonFlags =
    [
        NM(EventConstants.mbLeftButton, nameof(EventConstants.mbLeftButton)),
        NM(EventConstants.mbRightButton, nameof(EventConstants.mbRightButton)),
        NM(EventConstants.mbMiddleButton, nameof(EventConstants.mbMiddleButton)),
    ];

    // Mouse wheel flags table (from EventConstants, not MouseConstants)
    private static readonly TConstant[] MouseWheelFlags =
    [
        NM(EventConstants.mwUp, nameof(EventConstants.mwUp)),
        NM(EventConstants.mwDown, nameof(EventConstants.mwDown)),
        NM(EventConstants.mwLeft, nameof(EventConstants.mwLeft)),
        NM(EventConstants.mwRight, nameof(EventConstants.mwRight)),
    ];

    // Mouse event flags table (from EventConstants, not MouseConstants)
    private static readonly TConstant[] MouseEventFlags =
    [
        NM(EventConstants.meMouseMoved, nameof(EventConstants.meMouseMoved)),
        NM(EventConstants.meDoubleClick, nameof(EventConstants.meDoubleClick)),
        NM(EventConstants.meTripleClick, nameof(EventConstants.meTripleClick)),
    ];

    /// <summary>
    /// Prints bitflags as a combination of symbolic names separated by " | ".
    /// </summary>
    private static string PrintFlags(ushort flags, TConstant[] constants)
    {
        var sb = new StringBuilder();
        ushort foundFlags = 0;

        foreach (var constant in constants)
        {
            if ((flags & constant.Value) != 0)
            {
                if (foundFlags != 0)
                    sb.Append(" | ");
                sb.Append(constant.Name);
                foundFlags |= (ushort)(flags & constant.Value);
            }
        }

        if (foundFlags == 0 || foundFlags != flags)
        {
            if (foundFlags != 0)
                sb.Append(" | ");
            sb.Append($"0x{(flags & ~foundFlags):X4}");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Prints a single code value as a symbolic name or hex fallback.
    /// </summary>
    private static string PrintCode(ushort code, TConstant[] constants)
    {
        foreach (var constant in constants)
        {
            if (code == constant.Value)
                return constant.Name;
        }

        return $"0x{code:X4}";
    }

    public static string PrintKeyCode(ushort keyCode)
    {
        return PrintCode(keyCode, KeyCodes);
    }

    public static string PrintControlKeyState(ushort controlKeyState)
    {
        return PrintFlags(controlKeyState, ControlKeyStateFlags);
    }

    public static string PrintEventCode(ushort eventCode)
    {
        return PrintCode(eventCode, EventCodes);
    }

    public static string PrintMouseButtonState(ushort buttonState)
    {
        return PrintFlags(buttonState, MouseButtonFlags);
    }

    public static string PrintMouseWheelState(ushort wheelState)
    {
        return PrintFlags(wheelState, MouseWheelFlags);
    }

    public static string PrintMouseEventFlags(ushort eventFlags)
    {
        return PrintFlags(eventFlags, MouseEventFlags);
    }
}
