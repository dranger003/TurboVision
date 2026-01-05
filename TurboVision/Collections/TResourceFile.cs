namespace TurboVision.Collections;

using System.Text;
using TurboVision.Views;

/// <summary>
/// Persistent resource file manager.
/// Provides read/write access to a file-based resource collection.
/// </summary>
/// <remarks>
/// Matches upstream TResourceFile from resource.h/tresfile.cpp.
/// Resources are stored with a header, data section, and index.
/// </remarks>
public class TResourceFile : TObject
{
    /// <summary>
    /// Magic number identifying a resource file ('FBPR' = 0x52504246).
    /// Matches upstream rStreamMagic.
    /// </summary>
    public const int StreamMagic = 0x52504246;

    /// <summary>
    /// Signature for count-type header (0x4246 = 'FB').
    /// </summary>
    private const ushort SignatureFB = 0x4246;

    /// <summary>
    /// Info type indicating resource data (0x5250 = 'PR').
    /// </summary>
    private const ushort InfoTypePR = 0x5250;

    /// <summary>
    /// MZ executable signature.
    /// </summary>
    private const ushort SignatureMZ = 0x5A4D;

    private Stream _stream;
    private BinaryReader _reader;
    private BinaryWriter _writer;
    private bool _modified;
    private long _basePos;
    private int _indexPos;
    private TResourceCollection _index;
    private bool _ownsStream;

    /// <summary>
    /// Creates a resource file from an existing stream.
    /// Matches upstream constructor.
    /// </summary>
    /// <param name="stream">The stream to use (must be readable and seekable).</param>
    /// <param name="ownsStream">Whether this object owns and should dispose the stream.</param>
    public TResourceFile(Stream stream, bool ownsStream = true)
    {
        _stream = stream;
        _reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
        _writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
        _modified = false;
        _ownsStream = ownsStream;
        _index = new TResourceCollection(0, 8);

        _basePos = stream.Position;
        stream.Seek(0, SeekOrigin.End);
        long streamSize = stream.Position;

        bool found = false;
        bool repeat;

        do
        {
            repeat = false;
            if (_basePos <= streamSize - 8)
            {
                stream.Seek(_basePos, SeekOrigin.Begin);
                ushort signature = _reader.ReadUInt16();

                if (signature == SignatureMZ)
                {
                    // Skip MZ executable header
                    ushort lastCount = _reader.ReadUInt16();
                    ushort pageCount = _reader.ReadUInt16();
                    _basePos += (pageCount * 512L) - ((-lastCount) & 511);
                    repeat = true;
                }
                else if (signature == SignatureFB)
                {
                    ushort infoType = _reader.ReadUInt16();
                    if (infoType == InfoTypePR)
                    {
                        found = true;
                    }
                    else
                    {
                        int infoSize = _reader.ReadInt32();
                        _basePos += infoSize + 16 - (infoSize % 16);
                        repeat = true;
                    }
                }
            }
        } while (repeat);

        if (found)
        {
            // Read existing resource index
            stream.Seek(_basePos + sizeof(int) * 2, SeekOrigin.Begin);
            _indexPos = _reader.ReadInt32();
            stream.Seek(_basePos + _indexPos, SeekOrigin.Begin);
            _index.Read(_reader);
        }
        else
        {
            // Initialize new resource file
            _indexPos = sizeof(int) * 3;
            _index = new TResourceCollection(0, 8);
        }
    }

    /// <summary>
    /// Opens a resource file by path.
    /// </summary>
    /// <param name="path">Path to the resource file.</param>
    /// <param name="mode">File mode.</param>
    public TResourceFile(string path, FileMode mode = FileMode.OpenOrCreate)
        : this(new FileStream(path, mode, FileAccess.ReadWrite, FileShare.Read), true)
    {
    }

    /// <summary>
    /// Gets the number of resources in the file.
    /// Matches upstream count().
    /// </summary>
    public int Count
    {
        get { return _index.Count; }
    }

    /// <summary>
    /// Gets whether the file has been modified.
    /// </summary>
    public bool IsModified
    {
        get { return _modified; }
    }

    /// <summary>
    /// Removes a resource by key.
    /// Matches upstream remove().
    /// </summary>
    /// <param name="key">The resource key.</param>
    public void Remove(string key)
    {
        if (_index.Search(key, out int i))
        {
            _index.AtFree(i);
            _modified = true;
        }
    }

    /// <summary>
    /// Flushes any modifications to the stream.
    /// Matches upstream flush().
    /// </summary>
    public void Flush()
    {
        if (_modified)
        {
            // Write index at current index position
            _stream.Seek(_basePos + _indexPos, SeekOrigin.Begin);
            _index.Write(_writer);

            // Calculate total length
            int length = (int)(_stream.Position - _basePos - sizeof(int) * 2);

            // Write header
            _stream.Seek(_basePos, SeekOrigin.Begin);
            _writer.Write(StreamMagic);
            _writer.Write(length);
            _writer.Write(_indexPos);

            _writer.Flush();
            _modified = false;
        }
    }

    /// <summary>
    /// Gets the raw data for a resource.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <returns>The resource data, or null if not found.</returns>
    public byte[]? GetBytes(string key)
    {
        if (!_index.Search(key, out int i))
        {
            return null;
        }

        var item = _index.At(i);
        if (item == null)
        {
            return null;
        }

        _stream.Seek(_basePos + item.Position, SeekOrigin.Begin);
        return _reader.ReadBytes(item.Size);
    }

    /// <summary>
    /// Gets the reader positioned at a resource's data.
    /// Allows streaming large resources.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <param name="size">Output: the size of the resource data.</param>
    /// <returns>The BinaryReader, or null if not found.</returns>
    public BinaryReader? GetReader(string key, out int size)
    {
        size = 0;
        if (!_index.Search(key, out int i))
        {
            return null;
        }

        var item = _index.At(i);
        if (item == null)
        {
            return null;
        }

        size = item.Size;
        _stream.Seek(_basePos + item.Position, SeekOrigin.Begin);
        return _reader;
    }

    /// <summary>
    /// Gets the key at the specified index.
    /// Matches upstream keyAt().
    /// </summary>
    /// <param name="i">Zero-based index.</param>
    /// <returns>The key.</returns>
    public string? KeyAt(int i)
    {
        return _index.At(i)?.Key;
    }

    /// <summary>
    /// Stores raw data as a resource.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <param name="data">The data to store.</param>
    public void Put(string key, byte[] data)
    {
        TResourceItem item;
        if (_index.Search(key, out int i))
        {
            item = _index.At(i)!;
        }
        else
        {
            item = new TResourceItem { Key = key };
            _index.AtInsert(i, item);
        }

        // Write data at current index position
        item.Position = _indexPos;
        _stream.Seek(_basePos + _indexPos, SeekOrigin.Begin);
        _writer.Write(data);

        _indexPos = (int)(_stream.Position - _basePos);
        item.Size = _indexPos - item.Position;

        _modified = true;
    }

    /// <summary>
    /// Stores data from a writer action as a resource.
    /// Allows streaming large resources.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <param name="writeAction">Action that writes data to the BinaryWriter.</param>
    public void Put(string key, Action<BinaryWriter> writeAction)
    {
        TResourceItem item;
        if (_index.Search(key, out int i))
        {
            item = _index.At(i)!;
        }
        else
        {
            item = new TResourceItem { Key = key };
            _index.AtInsert(i, item);
        }

        // Write data at current index position
        item.Position = _indexPos;
        _stream.Seek(_basePos + _indexPos, SeekOrigin.Begin);
        writeAction(_writer);

        _indexPos = (int)(_stream.Position - _basePos);
        item.Size = _indexPos - item.Position;

        _modified = true;
    }

    /// <summary>
    /// Switches to a new stream, optionally packing data.
    /// Matches upstream switchTo().
    /// </summary>
    /// <param name="newStream">The new stream.</param>
    /// <param name="pack">Whether to pack (remove unused space).</param>
    /// <returns>The old stream.</returns>
    public Stream SwitchTo(Stream newStream, bool pack)
    {
        var newWriter = new BinaryWriter(newStream, Encoding.UTF8, leaveOpen: true);
        long newBasePos = newStream.Position;

        if (pack)
        {
            // Pack: copy only the resources that are in the index
            newStream.Seek(newBasePos + sizeof(int) * 3, SeekOrigin.Begin);

            for (int i = 0; i < _index.Count; i++)
            {
                var item = _index.At(i);
                if (item == null) continue;

                _stream.Seek(_basePos + item.Position, SeekOrigin.Begin);
                item.Position = (int)(newStream.Position - newBasePos);

                // Copy data
                byte[] buffer = new byte[Math.Min(item.Size, 4096)];
                int remaining = item.Size;
                while (remaining > 0)
                {
                    int toRead = Math.Min(remaining, buffer.Length);
                    int read = _stream.Read(buffer, 0, toRead);
                    if (read == 0) break;
                    newStream.Write(buffer, 0, read);
                    remaining -= read;
                }
            }

            _indexPos = (int)(newStream.Position - newBasePos);
        }
        else
        {
            // Copy everything up to the index
            _stream.Seek(_basePos, SeekOrigin.Begin);
            byte[] buffer = new byte[4096];
            int remaining = _indexPos;
            while (remaining > 0)
            {
                int toRead = Math.Min(remaining, buffer.Length);
                int read = _stream.Read(buffer, 0, toRead);
                if (read == 0) break;
                newStream.Write(buffer, 0, read);
                remaining -= read;
            }
        }

        _modified = true;
        _basePos = newBasePos;

        var oldStream = _stream;
        _stream = newStream;
        _reader = new BinaryReader(newStream, Encoding.UTF8, leaveOpen: true);
        _writer = newWriter;

        return oldStream;
    }

    /// <summary>
    /// Shuts down the resource file.
    /// </summary>
    public override void ShutDown()
    {
        Flush();
        _index.Dispose();
        _reader.Dispose();
        _writer.Dispose();
        if (_ownsStream)
        {
            _stream.Dispose();
        }
        base.ShutDown();
    }
}
