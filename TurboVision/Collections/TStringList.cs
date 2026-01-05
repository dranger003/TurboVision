namespace TurboVision.Collections;

using System.Text;
using TurboVision.Views;

/// <summary>
/// Index record for string list entries.
/// Provides efficient lookup of consecutive strings.
/// </summary>
/// <remarks>
/// Matches upstream TStrIndexRec from resource.h/tstrlist.cpp.
/// </remarks>
public struct TStrIndexRec
{
    /// <summary>
    /// Starting key for this index block.
    /// </summary>
    public ushort Key;

    /// <summary>
    /// Number of consecutive strings in this block.
    /// </summary>
    public ushort Count;

    /// <summary>
    /// Byte offset in the string data where this block starts.
    /// </summary>
    public ushort Offset;

    /// <summary>
    /// Creates a new index record.
    /// Matches upstream constructor.
    /// </summary>
    public TStrIndexRec()
    {
        Key = 0;
        Count = 0;
        Offset = 0;
    }

    /// <summary>
    /// Creates a new index record with specified values.
    /// </summary>
    public TStrIndexRec(ushort key, ushort count, ushort offset)
    {
        Key = key;
        Count = count;
        Offset = offset;
    }

    /// <summary>
    /// Reads an index record from a binary stream.
    /// </summary>
    public static TStrIndexRec Read(BinaryReader reader)
    {
        return new TStrIndexRec
        {
            Key = reader.ReadUInt16(),
            Count = reader.ReadUInt16(),
            Offset = reader.ReadUInt16()
        };
    }

    /// <summary>
    /// Writes the index record to a binary stream.
    /// </summary>
    public readonly void Write(BinaryWriter writer)
    {
        writer.Write(Key);
        writer.Write(Count);
        writer.Write(Offset);
    }
}

/// <summary>
/// Read-only string list loaded from a stream.
/// Provides efficient string lookup by numeric key.
/// </summary>
/// <remarks>
/// Matches upstream TStringList from resource.h/tstrlist.cpp.
/// Used for loading pre-built string resources.
/// </remarks>
public class TStringList : TObject
{
    /// <summary>
    /// Type name for streaming identification.
    /// Matches upstream name constant.
    /// </summary>
    public const string TypeName = "TStringList";

    /// <summary>
    /// Maximum number of consecutive keys in one index block.
    /// Matches upstream MAXKEYS constant.
    /// </summary>
    public const int MaxKeys = 16;

    private BinaryReader? _reader;
    private long _basePos;
    private int _indexSize;
    private TStrIndexRec[] _index;

    /// <summary>
    /// Creates an empty string list.
    /// Used for streaming initialization.
    /// </summary>
    protected TStringList()
    {
        _reader = null;
        _basePos = 0;
        _indexSize = 0;
        _index = [];
    }

    /// <summary>
    /// Gets the streamable type name.
    /// </summary>
    public string StreamableName
    {
        get { return TypeName; }
    }

    /// <summary>
    /// Gets a string by its key.
    /// Matches upstream get().
    /// </summary>
    /// <param name="key">The string key.</param>
    /// <returns>The string, or empty string if not found.</returns>
    public string Get(ushort key)
    {
        if (_indexSize == 0 || _reader == null)
        {
            return string.Empty;
        }

        // Find the index block containing this key
        TStrIndexRec? foundRec = null;
        for (int i = 0; i < _indexSize; i++)
        {
            var rec = _index[i];
            if (key >= rec.Key && key < rec.Key + rec.Count)
            {
                foundRec = rec;
                break;
            }
            if (rec.Key > key)
            {
                break;
            }
        }

        if (foundRec == null)
        {
            return string.Empty;
        }

        // Seek to the string data for this block
        var rec2 = foundRec.Value;
        _reader.BaseStream.Seek(_basePos + rec2.Offset, SeekOrigin.Begin);

        // Skip strings until we reach the desired key
        int skip = key - rec2.Key;
        for (int i = 0; i <= skip; i++)
        {
            byte length = _reader.ReadByte();
            if (i == skip)
            {
                byte[] bytes = _reader.ReadBytes(length);
                return Encoding.UTF8.GetString(bytes);
            }
            else
            {
                _reader.BaseStream.Seek(length, SeekOrigin.Current);
            }
        }

        return string.Empty;
    }

    /// <summary>
    /// Reads the string list from a binary stream.
    /// Matches upstream read().
    /// </summary>
    /// <param name="reader">The binary reader.</param>
    public void Read(BinaryReader reader)
    {
        _reader = reader;

        // Read string data size
        ushort strSize = reader.ReadUInt16();

        // Save position of string data
        _basePos = reader.BaseStream.Position;

        // Skip past string data to read index
        reader.BaseStream.Seek(_basePos + strSize, SeekOrigin.Begin);

        // Read index
        _indexSize = reader.ReadUInt16();
        _index = new TStrIndexRec[_indexSize];
        for (int i = 0; i < _indexSize; i++)
        {
            _index[i] = TStrIndexRec.Read(reader);
        }
    }

    /// <summary>
    /// Creates a TStringList from a binary stream.
    /// Factory method for streaming.
    /// </summary>
    public static TStringList Build(BinaryReader reader)
    {
        var list = new TStringList();
        list.Read(reader);
        return list;
    }

    /// <summary>
    /// Shuts down the string list.
    /// </summary>
    public override void ShutDown()
    {
        _reader = null;
        _index = [];
        _indexSize = 0;
        base.ShutDown();
    }
}
