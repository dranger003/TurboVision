namespace TurboVision.Streaming;

/// <summary>
/// Abstract writer interface for serialization.
/// Allows different implementations (JSON, Binary) to share
/// the same serialization logic in streamable objects.
/// Matches upstream opstream pattern.
/// </summary>
public interface IStreamWriter
{
    /// <summary>
    /// Writes an unsigned byte value.
    /// </summary>
    void WriteByte(byte value);

    /// <summary>
    /// Writes a signed 16-bit integer.
    /// </summary>
    void WriteInt16(short value);

    /// <summary>
    /// Writes a signed 32-bit integer.
    /// </summary>
    void WriteInt32(int value);

    /// <summary>
    /// Writes an unsigned 16-bit integer.
    /// </summary>
    void WriteUInt16(ushort value);

    /// <summary>
    /// Writes a string value.
    /// </summary>
    void WriteString(string? value);

    /// <summary>
    /// Writes a boolean value.
    /// </summary>
    void WriteBoolean(bool value);

    /// <summary>
    /// Writes a streamable object.
    /// </summary>
    /// <typeparam name="T">The type of object to write.</typeparam>
    /// <param name="obj">The object to write, or null.</param>
    void WriteObject<T>(T? obj) where T : class, IStreamable;

    /// <summary>
    /// Writes an array of streamable objects.
    /// </summary>
    /// <typeparam name="T">The type of objects in the array.</typeparam>
    /// <param name="items">The items to write.</param>
    void WriteArray<T>(IReadOnlyList<T>? items) where T : class, IStreamable;
}
