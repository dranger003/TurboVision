namespace TurboVision.Core;

/// <summary>
/// A bitset representing enabled/disabled commands.
/// Commands are identified by numbers 0-255.
/// </summary>
public class TCommandSet : IEquatable<TCommandSet>
{
    private const int MaxCommands = 256;
    private readonly byte[] _commands;

    public TCommandSet()
    {
        _commands = new byte[MaxCommands / 8];
    }

    public TCommandSet(TCommandSet other)
    {
        _commands = new byte[MaxCommands / 8];
        Array.Copy(other._commands, _commands, _commands.Length);
    }

    /// <summary>
    /// Checks if a command is enabled.
    /// </summary>
    public bool Has(int cmd)
    {
        if (cmd < 0 || cmd >= MaxCommands)
        {
            return false;
        }
        return (_commands[cmd / 8] & (1 << (cmd % 8))) != 0;
    }

    /// <summary>
    /// Enables a command.
    /// </summary>
    public void EnableCmd(int cmd)
    {
        if (cmd >= 0 && cmd < MaxCommands)
        {
            _commands[cmd / 8] |= (byte)(1 << (cmd % 8));
        }
    }

    /// <summary>
    /// Disables a command.
    /// </summary>
    public void DisableCmd(int cmd)
    {
        if (cmd >= 0 && cmd < MaxCommands)
        {
            _commands[cmd / 8] &= (byte)~(1 << (cmd % 8));
        }
    }

    /// <summary>
    /// Enables all commands in another set.
    /// </summary>
    public void EnableCmd(TCommandSet other)
    {
        for (int i = 0; i < _commands.Length; i++)
        {
            _commands[i] |= other._commands[i];
        }
    }

    /// <summary>
    /// Disables all commands in another set.
    /// </summary>
    public void DisableCmd(TCommandSet other)
    {
        for (int i = 0; i < _commands.Length; i++)
        {
            _commands[i] &= (byte)~other._commands[i];
        }
    }

    /// <summary>
    /// Checks if all commands are disabled.
    /// </summary>
    public bool IsEmpty
    {
        get
        {
            for (int i = 0; i < _commands.Length; i++)
            {
                if (_commands[i] != 0)
                {
                    return false;
                }
            }
            return true;
        }
    }

    public static TCommandSet operator &(TCommandSet left, TCommandSet right)
    {
        TCommandSet result = new();
        for (int i = 0; i < result._commands.Length; i++)
        {
            result._commands[i] = (byte)(left._commands[i] & right._commands[i]);
        }
        return result;
    }

    public static TCommandSet operator |(TCommandSet left, TCommandSet right)
    {
        TCommandSet result = new();
        for (int i = 0; i < result._commands.Length; i++)
        {
            result._commands[i] = (byte)(left._commands[i] | right._commands[i]);
        }
        return result;
    }

    public bool Equals(TCommandSet? other)
    {
        if (other is null)
        {
            return false;
        }
        return _commands.AsSpan().SequenceEqual(other._commands);
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as TCommandSet);
    }

    public override int GetHashCode()
    {
        HashCode hash = new();
        hash.AddBytes(_commands);
        return hash.ToHashCode();
    }

    public static bool operator ==(TCommandSet? left, TCommandSet? right)
    {
        if (left is null)
        {
            return right is null;
        }
        return left.Equals(right);
    }

    public static bool operator !=(TCommandSet? left, TCommandSet? right)
    {
        return !(left == right);
    }
}
