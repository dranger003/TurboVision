namespace TurboVision.Streaming;

/// <summary>
/// Contract for serializing TurboVision objects to streams.
/// Allows different serialization formats (JSON, Binary) to be used interchangeably.
/// </summary>
public interface IStreamSerializer
{
    /// <summary>
    /// Writes a streamable object to a stream.
    /// </summary>
    /// <param name="stream">The output stream.</param>
    /// <param name="obj">The object to serialize.</param>
    void Write(Stream stream, IStreamable obj);

    /// <summary>
    /// Reads a streamable object from a stream.
    /// </summary>
    /// <typeparam name="T">The expected type of the root object.</typeparam>
    /// <param name="stream">The input stream.</param>
    /// <returns>The deserialized object, or null if the stream is empty.</returns>
    T? Read<T>(Stream stream) where T : class, IStreamable;

    /// <summary>
    /// Registers a type for polymorphic serialization.
    /// </summary>
    /// <typeparam name="T">The type to register.</typeparam>
    /// <param name="typeId">The type identifier string.</param>
    void Register<T>(string typeId) where T : class, IStreamable, new();
}
