namespace TurboVision.Streaming;

/// <summary>
/// Marker interface for streamable objects.
/// All serializable TurboVision objects implement this interface.
/// Matches upstream TStreamable pattern.
/// </summary>
public interface IStreamable
{
    /// <summary>
    /// Gets the type identifier for streaming.
    /// Used as the $type discriminator in JSON serialization.
    /// Matches upstream streamableName().
    /// </summary>
    string StreamableName { get; }
}
