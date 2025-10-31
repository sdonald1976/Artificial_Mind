using AM.Mind.IO.Enums;
using AM.Mind.IO.Interfaces;
using AM.Mind.IO.Structs;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AM.Mind.IO.Models;

// --- Writer: manages .xlog chunks + .fidx index ---
public sealed class XLogWriter : IDisposable
{
    private readonly string _dir;
    private readonly string _prefix;
    private readonly long _rotateBytes;
    private readonly ICodec _codec;
    private FileStream _xlog;
    private FileStream _fidx;
    private byte _fileId;
    private long _nextId;
    private XLogFlags _flags;
    private bool _disposed;

    public string CurrentXLogPath { get; private set; }
    public string CurrentFidxPath { get; private set; }

    public XLogWriter(string directory, string prefix = "exp", long rotateBytes = 2L * 1024 * 1024 * 1024, long startId = 0, ICodec? codec = null)
    {
        Directory.CreateDirectory(directory);
        _dir = directory;
        _prefix = prefix;
        _rotateBytes = rotateBytes;
        _codec = codec ?? new PassThroughCodec();
        _flags = _codec.Flags;

        // Start with fileId = highest existing + 1
        _fileId = GetNextFileId(directory, prefix);
        _nextId = startId;

        OpenNewFiles();
    }

    public long Append(ExperienceEnvelope env)
    {
        EnsureNotDisposed();
        // Assign id if zero (or always assign from counter)
        env.Id = _nextId++;

        long offsetBefore = _xlog.Position;

        // Serialize + (optionally) compress
        var raw = EnvelopeCodec.Serialize(env);
        var enc = _codec.Encode(raw);

        // Write [RecLen varint] + [Payload]
        Bin.WriteVarUInt(_xlog, (ulong)enc.Length);
        _xlog.Write(enc, 0, enc.Length);

        // Index row
        var idx = new IndexEntry
        {
            Id = env.Id,
            FileOffset = offsetBefore,
            TicksUtc = env.TicksUtc,
            Reward = env.Reward,
            Flags = (byte)(env.Terminal ? 0x1 : 0x0),
            FileId = _fileId,
            Episode = (ushort)Math.Clamp(env.Episode, 0, ushort.MaxValue)
        };
        WriteIndexEntry(idx);

        // Rotate if needed
        if (_xlog.Position >= _rotateBytes) Rotate();

        return env.Id;
    }

    public void Flush()
    {
        _xlog.Flush(true);
        _fidx.Flush(true);
    }

    private void OpenNewFiles()
    {
        CurrentXLogPath = Path.Combine(_dir, $"{_prefix}-{_fileId:D5}.xlog");
        CurrentFidxPath = Path.Combine(_dir, $"{_prefix}-{_fileId:D5}.fidx");

        _xlog = new FileStream(CurrentXLogPath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.Read, 1 << 16, FileOptions.SequentialScan | FileOptions.WriteThrough);
        _fidx = new FileStream(CurrentFidxPath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.Read, 1 << 16, FileOptions.WriteThrough);

        // Write xlog header
        _xlog.Write(Constants.Magic);
        Span<byte> head = stackalloc byte[2 + 2 + 8];
        BinaryPrimitives.WriteUInt16LittleEndian(head.Slice(0, 2), Constants.Version);
        BinaryPrimitives.WriteUInt16LittleEndian(head.Slice(2, 2), (ushort)_flags);
        head.Slice(4, 8).Clear(); // reserved
        _xlog.Write(head);
    }

    private void Rotate()
    {
        _xlog.Dispose();
        _fidx.Dispose();
        _fileId++;
        OpenNewFiles();
    }

    private void WriteIndexEntry(IndexEntry e)
    {
        Span<byte> buf = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref e, 1));
        _fidx.Write(buf);
    }

    private static byte GetNextFileId(string dir, string prefix)
    {
        byte next = 0;
        foreach (var path in Directory.EnumerateFiles(dir, $"{prefix}-*.xlog"))
        {
            var name = Path.GetFileNameWithoutExtension(path);
            var dash = name.LastIndexOf('-');
            if (dash < 0) continue;
            if (byte.TryParse(name.AsSpan(dash + 1), out var id))
                if (id >= next) next = (byte)(id + 1);
        }
        return next;
    }

    private void EnsureNotDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(XLogWriter));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _xlog?.Dispose();
        _fidx?.Dispose();
    }
}