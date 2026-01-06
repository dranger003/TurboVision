namespace TurboVision.Streaming;

/// <summary>
/// Abstract reader interface for deserialization.
/// Allows different implementations (JSON, Binary) to share
/// the same deserialization logic in streamable objects.
/// Matches upstream ipstream pattern.
/// </summary>
public interface IStreamReader
{
    /// <summary>
    /// Reads an unsigned byte value.
    /// </summary>
    byte ReadByte();

    /// <summary>
    /// Reads a signed 16-bit integer.
    /// </summary>
    short ReadInt16();

    /// <summary>
    /// Reads a signed 32-bit integer.
    /// </summary>
    int ReadInt32();

    /// <summary>
    /// Reads an unsigned 16-bit integer.
    /// </summary>
    ushort ReadUInt16();

    /// <summary>
    /// Reads a string value.
    /// </summary>
    string? ReadString();

    /// <summary>
    /// Reads a boolean value.
    /// </summary>
    bool ReadBoolean();

    /// <summary>
    /// Reads a streamable object.
    /// </summary>
    /// <typeparam name="T">The expected type of object.</typeparam>
    /// <returns>The object read, or null.</returns>
    T? ReadObject<T>() where T : class, IStreamable;

    /// <summary>
    /// Reads an array of streamable objects.
    /// </summary>
    /// <typeparam name="T">The type of objects in the array.</typeparam>
    /// <returns>The list of objects read.</returns>
    List<T> ReadArray<T>() where T : class, IStreamable;
}
