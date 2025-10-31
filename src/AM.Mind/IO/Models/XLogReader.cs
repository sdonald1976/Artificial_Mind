using AM.Mind.IO.Enums;
using AM.Mind.IO.Interfaces;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AM.Mind.IO.Models;

// --- Reader: iterate, read-at-offset, and map index ---
public sealed class XLogReader : IDisposable
{
    private readonly ICodec _codec;
    private FileStream _xlog;
    private readonly string _xlogPath;
    private readonly XLogFlags _flags;
    private bool _disposed;

    public XLogReader(string xlogPath, ICodec? codec = null)
    {
        _codec = codec ?? new PassThroughCodec();
        _xlogPath = xlogPath;
        _xlog = new FileStream(_xlogPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 1 << 16, FileOptions.SequentialScan);

        // Validate header
        Span<byte> magic = stackalloc byte[4];
        _xlog.ReadExactly(magic);
        if (!magic.SequenceEqual(Constants.Magic)) throw new InvalidDataException("Bad XLOG magic");

        Span<byte> head = stackalloc byte[2 + 2 + 8];
        _xlog.ReadExactly(head);
        ushort ver = BinaryPrimitives.ReadUInt16LittleEndian(head.Slice(0, 2));
        if (ver != Constants.Version) throw new InvalidDataException($"Unsupported XLOG version {ver}");
        _flags = (XLogFlags)BinaryPrimitives.ReadUInt16LittleEndian(head.Slice(2, 2));
        // reserved unused
    }

    public IEnumerable<(long FileOffset, ExperienceEnvelope Envelope)> ReadAll()
    {
        EnsureNotDisposed();
        while (true)
        {
            long offset = _xlog.Position;

            // Clean EOF → stop
            if (!Bin.TryReadVarUInt(_xlog, out var lenU))
                yield break;

            int len = checked((int)lenU);
            var buf = new byte[len];
            _xlog.ReadExactly(buf);
            var raw = _codec.Decode(buf);
            var env = EnvelopeCodec.Deserialize(raw);

            yield return (offset, env);
        }
    }

    public ExperienceEnvelope ReadAt(long fileOffset)
    {
        EnsureNotDisposed();
        _xlog.Position = fileOffset;
        var (lenU, _) = Bin.ReadVarUInt(_xlog);
        int len = checked((int)lenU);
        var buf = new byte[len];
        _xlog.ReadExactly(buf);
        var raw = _codec.Decode(buf);
        return EnvelopeCodec.Deserialize(raw);
    }

    private void EnsureNotDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(XLogReader));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _xlog?.Dispose();
    }
}