namespace TurboVision.Help;

/// <summary>
/// Index mapping topic context numbers to file positions.
/// </summary>
public class THelpIndex
{
    private List<long> _index = [];

    public THelpIndex()
    {
    }

    /// <summary>
    /// Gets the number of entries in the index.
    /// </summary>
    public int Size
    {
        get { return _index.Count; }
    }

    /// <summary>
    /// Gets the file position for a topic.
    /// </summary>
    /// <param name="context">The topic context number.</param>
    /// <returns>The file position, or -1 if not found.</returns>
    public long Position(int context)
    {
        if (context >= 0 && context < _index.Count)
        {
            return _index[context];
        }
        return -1;
    }

    /// <summary>
    /// Adds or updates a topic position.
    /// </summary>
    /// <param name="context">The topic context number.</param>
    /// <param name="position">The file position.</param>
    public void Add(int context, long position)
    {
        // Expand the index if needed
        while (_index.Count <= context)
        {
            _index.Add(-1);
        }
        _index[context] = position;
    }

    /// <summary>
    /// Reads the index from a binary reader.
    /// </summary>
    public void Read(BinaryReader reader)
    {
        int size = reader.ReadUInt16();
        _index = new List<long>(size);
        for (int i = 0; i < size; i++)
        {
            _index.Add(reader.ReadInt32());
        }
    }

    /// <summary>
    /// Writes the index to a binary writer.
    /// </summary>
    public void Write(BinaryWriter writer)
    {
        writer.Write((ushort)_index.Count);
        foreach (long pos in _index)
        {
            writer.Write((int)pos);
        }
    }
}
