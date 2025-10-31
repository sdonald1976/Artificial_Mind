using AM.Mind.IO.Structs;
using System;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AM.Mind.IO.Models;

// --- Index reader (memory-map friendly) ---
public sealed class IndexReader : IDisposable
{
    private readonly MemoryMappedFile _mmf;
    private readonly MemoryMappedViewAccessor _acc;
    private readonly long _length;
    private bool _disposed;

    public string Path { get; }

    public IndexReader(string fidxPath)
    {
        Path = fidxPath;
        var fi = new FileInfo(fidxPath);
        if (!fi.Exists) throw new FileNotFoundException("Index file not found", fidxPath);
        _length = fi.Length;

        _mmf = MemoryMappedFile.CreateFromFile(fidxPath, FileMode.Open, null, 0, MemoryMappedFileAccess.Read);
        _acc = _mmf.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);
    }

    public long Count => _length / UnsafeSizeOfIndexEntry;

    public IndexEntry ReadAt(long entryIndex)
    {
        EnsureNotDisposed();
        if (entryIndex < 0 || entryIndex >= Count) throw new ArgumentOutOfRangeException(nameof(entryIndex));
        _acc.Read(entryIndex * UnsafeSizeOfIndexEntry, out IndexEntry e);
        return e;
    }

    public IEnumerable<IndexEntry> Range(long start, long count)
    {
        EnsureNotDisposed();
        long end = Math.Min(start + count, Count);
        for (long i = start; i < end; i++)
        {
            _acc.Read(i * UnsafeSizeOfIndexEntry, out IndexEntry e);
            yield return e;
        }
    }

    public IEnumerable<IndexEntry> All()
    {
        EnsureNotDisposed();
        for (long i = 0; i < Count; i++)
        {
            _acc.Read(i * UnsafeSizeOfIndexEntry, out IndexEntry e);
            yield return e;
        }
    }

    private static int UnsafeSizeOfIndexEntry => Marshal.SizeOf<IndexEntry>();

    private void EnsureNotDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(IndexReader));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _acc?.Dispose();
        _mmf?.Dispose();
    }
}