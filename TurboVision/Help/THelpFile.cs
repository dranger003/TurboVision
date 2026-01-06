using System.Text.Json;

namespace TurboVision.Help;

/// <summary>
/// Manages help file I/O, supporting both binary and JSON formats.
/// </summary>
public class THelpFile : IDisposable
{
    private Stream? _stream;
    private bool _ownsStream;
    private bool _modified;
    private long _indexPos;

    /// <summary>
    /// The topic index for this help file.
    /// </summary>
    public THelpIndex Index { get; private set; }

    /// <summary>
    /// Opens an existing help file from a stream.
    /// </summary>
    public THelpFile(Stream stream, bool ownsStream = true)
    {
        _stream = stream;
        _ownsStream = ownsStream;
        Index = new THelpIndex();

        if (stream.Length >= 4)
        {
            using var reader = new BinaryReader(stream, System.Text.Encoding.UTF8, leaveOpen: true);
            int magic = reader.ReadInt32();

            if (magic == HelpConstants.MagicHeader && stream.Length >= 12)
            {
                // Binary format
                stream.Seek(8, SeekOrigin.Begin);
                _indexPos = reader.ReadInt32();
                stream.Seek(_indexPos, SeekOrigin.Begin);
                Index.Read(reader);
                _modified = false;
            }
            else
            {
                // Not a valid binary file, try JSON or create new
                _indexPos = 12;
                _modified = true;
            }
        }
        else
        {
            _indexPos = 12;
            _modified = true;
        }
    }

    /// <summary>
    /// Creates a new help file.
    /// </summary>
    public THelpFile()
    {
        Index = new THelpIndex();
        _indexPos = 12;
        _modified = true;
    }

    /// <summary>
    /// Gets a topic by context number.
    /// </summary>
    /// <param name="context">The topic context number.</param>
    /// <returns>The help topic, or an invalid topic if not found.</returns>
    public THelpTopic GetTopic(int context)
    {
        long pos = Index.Position(context);

        if (pos > 0 && _stream != null)
        {
            _stream.Seek(pos, SeekOrigin.Begin);
            return ReadTopic(_stream);
        }

        return InvalidTopic();
    }

    /// <summary>
    /// Creates an "invalid topic" placeholder.
    /// </summary>
    public static THelpTopic InvalidTopic()
    {
        var topic = new THelpTopic();
        var para = new TParagraph(HelpConstants.InvalidContext, false);
        topic.AddParagraph(para);
        return topic;
    }

    /// <summary>
    /// Records the current position in the index for a topic.
    /// </summary>
    public void RecordPositionInIndex(int context)
    {
        Index.Add(context, _indexPos);
        _modified = true;
    }

    /// <summary>
    /// Writes a topic to the help file.
    /// </summary>
    public void PutTopic(THelpTopic topic)
    {
        if (_stream == null) return;

        _stream.Seek(_indexPos, SeekOrigin.Begin);
        WriteTopic(_stream, topic);
        _indexPos = _stream.Position;
        _modified = true;
    }

    /// <summary>
    /// Loads a help file from JSON format.
    /// </summary>
    public static THelpFile LoadFromJson(string json)
    {
        var helpFile = new THelpFile();
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.TryGetProperty("topics", out var topicsArray))
        {
            foreach (var topicElement in topicsArray.EnumerateArray())
            {
                int context = topicElement.GetProperty("context").GetInt32();
                var topic = ReadTopicFromJson(topicElement);

                // Store topic data (in-memory for JSON-loaded files)
                helpFile.Index.Add(context, context); // Use context as pseudo-position
            }
        }

        return helpFile;
    }

    /// <summary>
    /// Loads a help file from a JSON file.
    /// </summary>
    public static THelpFile LoadFromJsonFile(string path)
    {
        string json = File.ReadAllText(path);
        return LoadFromJson(json);
    }

    private static THelpTopic ReadTopic(Stream stream)
    {
        using var reader = new BinaryReader(stream, System.Text.Encoding.UTF8, leaveOpen: true);
        var topic = new THelpTopic();

        // Read paragraphs
        int paragraphCount = reader.ReadInt32();
        for (int i = 0; i < paragraphCount; i++)
        {
            ushort size = reader.ReadUInt16();
            bool wrap = reader.ReadInt32() != 0;
            byte[] textBytes = reader.ReadBytes(size);
            string text = System.Text.Encoding.UTF8.GetString(textBytes);

            topic.AddParagraph(new TParagraph(text, wrap));
        }

        // Read cross-references
        int refCount = reader.ReadInt32();
        for (int i = 0; i < refCount; i++)
        {
            int refTopic = reader.ReadInt32();
            int offset = reader.ReadInt32();
            byte length = reader.ReadByte();

            topic.AddCrossRef(new TCrossRef(refTopic, offset, length));
        }

        return topic;
    }

    private static void WriteTopic(Stream stream, THelpTopic topic)
    {
        using var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true);

        // Count paragraphs
        int paragraphCount = 0;
        var p = topic.Paragraphs;
        while (p != null)
        {
            paragraphCount++;
            p = p.Next;
        }

        // Write paragraphs
        writer.Write(paragraphCount);
        p = topic.Paragraphs;
        while (p != null)
        {
            byte[] textBytes = System.Text.Encoding.UTF8.GetBytes(p.Text);
            writer.Write((ushort)textBytes.Length);
            writer.Write(p.Wrap ? 1 : 0);
            writer.Write(textBytes);
            p = p.Next;
        }

        // Write cross-references
        writer.Write(topic.CrossRefs.Count);
        foreach (var crossRef in topic.CrossRefs)
        {
            writer.Write(crossRef.Ref);
            writer.Write(crossRef.Offset);
            writer.Write(crossRef.Length);
        }
    }

    private static THelpTopic ReadTopicFromJson(JsonElement element)
    {
        var topic = new THelpTopic();

        if (element.TryGetProperty("paragraphs", out var paragraphs))
        {
            foreach (var para in paragraphs.EnumerateArray())
            {
                string text = para.GetProperty("text").GetString() ?? string.Empty;
                bool wrap = para.TryGetProperty("wrap", out var wrapProp) && wrapProp.GetBoolean();
                topic.AddParagraph(new TParagraph(text, wrap));
            }
        }

        if (element.TryGetProperty("crossRefs", out var crossRefs))
        {
            foreach (var cr in crossRefs.EnumerateArray())
            {
                int target = cr.GetProperty("target").GetInt32();
                int offset = cr.GetProperty("offset").GetInt32();
                byte length = (byte)cr.GetProperty("length").GetInt32();
                topic.AddCrossRef(new TCrossRef(target, offset, length));
            }
        }

        return topic;
    }

    public void Dispose()
    {
        if (_modified && _stream != null && _stream.CanWrite)
        {
            // Write index at current position
            _stream.Seek(_indexPos, SeekOrigin.Begin);
            using var writer = new BinaryWriter(_stream, System.Text.Encoding.UTF8, leaveOpen: true);
            Index.Write(writer);

            // Write header
            long size = _stream.Position - 8;
            _stream.Seek(0, SeekOrigin.Begin);
            writer.Write(HelpConstants.MagicHeader);
            writer.Write((int)size);
            writer.Write((int)_indexPos);
        }

        if (_ownsStream)
        {
            _stream?.Dispose();
        }
        _stream = null;
    }
}
