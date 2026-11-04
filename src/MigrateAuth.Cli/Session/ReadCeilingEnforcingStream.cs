namespace MigrateAuth.Cli.Session;

/// <summary>
/// Thrown when a file the product reads holds more than its ceiling. It deliberately is not an
/// <see cref="IOException"/>: a caller that treats every I/O failure as "unreadable" must not
/// swallow the one failure that means "too large".
/// </summary>
public sealed class ReadCeilingExceededException : Exception
{
	public ReadCeilingExceededException(long ceilingInBytes)
		: base($"The file holds more than {ceilingInBytes} bytes.")
	{
	}
}

/// <summary>
/// THE SIZE BOUND OF AN UNTRUSTED FILE, APPLIED TO WHAT IS ACTUALLY READ.
///
/// A length taken from the directory entry before the file is opened bounds nothing: the file can
/// grow between that check and the read. This wrapper is therefore built over the OPENED stream
/// and checks its length once more, then counts every byte handed to the reader and refuses the
/// one that passes the ceiling, so a file that grows while it is being read still cannot deliver
/// more than the ceiling to the deserializer. The inner stream is left to its owner to dispose.
/// </summary>
public sealed class ReadCeilingEnforcingStream : Stream
{
	private readonly Stream _inner;
	private readonly long _ceilingInBytes;

	private long _bytesRead;

	private ReadCeilingEnforcingStream(Stream inner, long ceilingInBytes)
	{
		_inner = inner;
		_ceilingInBytes = ceilingInBytes;
	}

	public override bool CanRead => true;

	public override bool CanSeek => false;

	public override bool CanWrite => false;

	public override long Length => throw new NotSupportedException();

	public override long Position
	{
		get => _bytesRead;
		set => throw new NotSupportedException();
	}

	/// <summary>
	/// Wraps an opened stream, refusing it at once when its length already passes the ceiling.
	/// </summary>
	/// <exception cref="ReadCeilingExceededException">The opened file is longer than the ceiling.</exception>
	public static ReadCeilingEnforcingStream Wrap(Stream opened, long ceilingInBytes)
	{
		ArgumentNullException.ThrowIfNull(opened);

		if (opened.Length > ceilingInBytes)
		{
			throw new ReadCeilingExceededException(ceilingInBytes);
		}

		return new ReadCeilingEnforcingStream(opened, ceilingInBytes);
	}

	public override int Read(Span<byte> buffer)
	{
		int count = _inner.Read(buffer);

		_bytesRead += count;

		if (_bytesRead > _ceilingInBytes)
		{
			throw new ReadCeilingExceededException(_ceilingInBytes);
		}

		return count;
	}

	public override int Read(byte[] buffer, int offset, int count)
	{
		return Read(buffer.AsSpan(offset, count));
	}

	public override void Flush()
	{
	}

	public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

	public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

	public override void SetLength(long value) => throw new NotSupportedException();
}
