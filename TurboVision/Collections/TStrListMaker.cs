namespace TurboVision.Collections;

using System.Text;
using TurboVision.Views;

/// <summary>
/// String list builder for creating string resources.
/// Creates indexed string lists that can be read by TStringList.
/// </summary>
/// <remarks>
/// Matches upstream TStrListMaker from resource.h/tstrlist.cpp.
/// </remarks>
public class TStrListMaker : TObject
{
    /// <summary>
    /// Maximum number of consecutive keys per index block.
    /// Matches upstream MAXKEYS constant.
    /// </summary>
    public const int MaxKeys = 16;

    private int _strPos;
    private int _strSize;
    private byte[] _strings;
    private int _indexPos;
    private int _indexSize;
    private TStrIndexRec[] _index;
    private TStrIndexRec _cur;

    /// <summary>
    /// Creates a new string list maker with specified capacities.
    /// Matches upstream constructor.
    /// </summary>
    /// <param name="aStrSize">Maximum size for string data in bytes.</param>
    /// <param name="aIndexSize">Maximum number of index entries.</param>
    public TStrListMaker(int aStrSize, int aIndexSize)
    {
        _strPos = 0;
        _strSize = aStrSize;
        _strings = new byte[aStrSize];
        _indexPos = 0;
        _indexSize = aIndexSize;
        _index = new TStrIndexRec[aIndexSize];
        _cur = new TStrIndexRec();
    }

    /// <summary>
    /// Creates an empty string list maker.
    /// Used for streaming initialization.
    /// </summary>
    protected TStrListMaker()
    {
        _strPos = 0;
        _strSize = 0;
        _strings = [];
        _indexPos = 0;
        _indexSize = 0;
        _index = [];
        _cur = new TStrIndexRec();
    }

    /// <summary>
    /// Gets the streamable type name (same as TStringList for compatibility).
    /// </summary>
    public string StreamableName
    {
        get { return TStringList.TypeName; }
    }

    /// <summary>
    /// Adds a string with the specified key.
    /// Strings should be added in key order for optimal performance.
    /// Matches upstream put().
    /// </summary>
    /// <param name="key">The string key.</param>
    /// <param name="str">The string value.</param>
    public void Put(ushort key, string str)
    {
        // Check if we need to start a new index block
        if (_cur.Count == MaxKeys || key != _cur.Key + _cur.Count)
        {
            CloseCurrent();
        }

        // Start new block if needed
        if (_cur.Count == 0)
        {
            _cur.Key = key;
            _cur.Offset = (ushort)_strPos;
        }

        // Store the string: length byte followed by string data
        byte[] bytes = Encoding.UTF8.GetBytes(str);
        if (bytes.Length > 255)
        {
            throw new ArgumentException("String too long (max 255 bytes)", nameof(str));
        }
        if (_strPos + bytes.Length + 1 > _strSize)
        {
            throw new InvalidOperationException("String buffer overflow");
        }

        _strings[_strPos++] = (byte)bytes.Length;
        Array.Copy(bytes, 0, _strings, _strPos, bytes.Length);
        _strPos += bytes.Length;
        _cur.Count++;
    }

    /// <summary>
    /// Closes the current index block.
    /// Matches upstream closeCurrent().
    /// </summary>
    private void CloseCurrent()
    {
        if (_cur.Count != 0)
        {
            if (_indexPos >= _indexSize)
            {
                throw new InvalidOperationException("Index buffer overflow");
            }
            _index[_indexPos++] = _cur;
            _cur = new TStrIndexRec();
        }
    }

    /// <summary>
    /// Writes the string list to a binary stream.
    /// Matches upstream write().
    /// </summary>
    /// <param name="writer">The binary writer.</param>
    public void Write(BinaryWriter writer)
    {
        // Close any pending index block
        CloseCurrent();

        // Write string data size and data
        writer.Write((ushort)_strPos);
        writer.Write(_strings, 0, _strPos);

        // Write index
        writer.Write((ushort)_indexPos);
        for (int i = 0; i < _indexPos; i++)
        {
            _index[i].Write(writer);
        }
    }

    /// <summary>
    /// Gets the current string data size in bytes.
    /// </summary>
    public int StringDataSize
    {
        get { return _strPos; }
    }

    /// <summary>
    /// Gets the current number of index entries.
    /// </summary>
    public int IndexCount
    {
        get { return _indexPos + (_cur.Count > 0 ? 1 : 0); }
    }

    /// <summary>
    /// Shuts down the string list maker.
    /// </summary>
    public override void ShutDown()
    {
        _strings = [];
        _index = [];
        base.ShutDown();
    }
}
