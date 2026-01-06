using System.Text.Json.Serialization;
using TurboVision.Streaming;

namespace TurboVision.Collections;

/// <summary>
/// Streamable dynamic array collection.
/// Extends TNSCollection with serialization capabilities.
/// </summary>
/// <remarks>
/// Matches upstream TCollection from objects.h/tcollect.cpp.
/// Implements IStreamable for JSON serialization support.
/// </remarks>
/// <typeparam name="T">The type of items in the collection.</typeparam>
public abstract class TCollection<T> : TNSCollection<T>, IStreamable where T : class
{
    /// <summary>
    /// Type name for streaming identification.
    /// Matches upstream name constant.
    /// </summary>
    public const string TypeName = "TCollection";

    /// <summary>
    /// Creates a new collection with specified initial limit and growth delta.
    /// </summary>
    /// <param name="aLimit">Initial capacity.</param>
    /// <param name="aDelta">Growth increment when capacity is exceeded.</param>
    public TCollection(int aLimit, int aDelta) : base(aLimit, aDelta)
    {
    }

    /// <summary>
    /// Creates an empty collection.
    /// Used for streaming initialization.
    /// </summary>
    protected TCollection() : base()
    {
    }

    /// <summary>
    /// Gets the streamable type name.
    /// Matches upstream streamableName().
    /// </summary>
    [JsonIgnore]
    public virtual string StreamableName => TypeName;

    /// <summary>
    /// Writes the collection to a binary stream.
    /// Matches upstream write().
    /// </summary>
    /// <param name="writer">The binary writer.</param>
    public virtual void Write(BinaryWriter writer)
    {
        writer.Write(Count);
        writer.Write(Limit);
        writer.Write(Delta);
        for (int i = 0; i < Count; i++)
        {
            WriteItem(Items[i], writer);
        }
    }

    /// <summary>
    /// Reads the collection from a binary stream.
    /// Matches upstream read().
    /// </summary>
    /// <param name="reader">The binary reader.</param>
    public virtual void Read(BinaryReader reader)
    {
        int count = reader.ReadInt32();
        int savedLimit = reader.ReadInt32();
        int delta = reader.ReadInt32();

        Delta = delta;
        SetLimit(savedLimit);

        for (int i = 0; i < count; i++)
        {
            Items[i] = ReadItem(reader);
        }
        SetCount(count);
    }

    /// <summary>
    /// Writes a single item to the stream.
    /// Matches upstream writeItem().
    /// Must be implemented by derived classes.
    /// </summary>
    /// <param name="item">The item to write.</param>
    /// <param name="writer">The binary writer.</param>
    protected abstract void WriteItem(T? item, BinaryWriter writer);

    /// <summary>
    /// Reads a single item from the stream.
    /// Matches upstream readItem().
    /// Must be implemented by derived classes.
    /// </summary>
    /// <param name="reader">The binary reader.</param>
    /// <returns>The item read.</returns>
    protected abstract T? ReadItem(BinaryReader reader);
}
