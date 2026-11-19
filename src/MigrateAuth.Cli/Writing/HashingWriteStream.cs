using System.Security.Cryptography;

namespace MigrateAuth.Cli.Writing;

/// <summary>
/// A write-only stream that passes every byte through to the stream it wraps and hashes the same bytes
/// with SHA-256 on the way, so the length and hash of a file are known the moment it was written, without
/// reading it back.
///
/// HASHING IS BEST-EFFORT AND NEVER FAILS THE WRITE. The bytes are forwarded first; a failure of the hash
/// (<see cref="CryptographicException"/>, <see cref="PlatformNotSupportedException"/>, and nothing broader)
/// only marks the stream as <see cref="HashingFailed"/>, after which the bytes still pass through. The
/// caller that cares - the report recording and the append - reads the flag.
/// </summary>
public sealed class HashingWriteStream : Stream
{
	private readonly Stream _inner;
	private IncrementalHash? _hash;
	private long _bytesWritten;

	/// <summary>Wraps <paramref name="inner"/>. The wrapped stream is disposed with this one.</summary>
	/// <param name="inner">Where the bytes go.</param>
	/// <param name="hashFactory">Creates the hash; <c>null</c> means SHA-256. A seam for a hasher that throws.</param>
	public HashingWriteStream(Stream inner, Func<IncrementalHash>? hashFactory = null)
	{
		ArgumentNullException.ThrowIfNull(inner);

		_inner = inner;

		try
		{
			_hash = (hashFactory ?? CreateSha256)();
		}
		catch (CryptographicException)
		{
			HashingFailed = true;
		}
		catch (PlatformNotSupportedException)
		{
			HashingFailed = true;
		}
	}

	/// <summary>Whether the hash could not be kept; the bytes were still forwarded.</summary>
	public bool HashingFailed { get; private set; }

	public override bool CanRead => false;

	public override bool CanSeek => false;

	public override bool CanWrite => _inner.CanWrite;

	public override long Length => throw new NotSupportedException();

	public override long Position
	{
		get => throw new NotSupportedException();
		set => throw new NotSupportedException();
	}

	/// <summary>
	/// The length and SHA-256 of everything written so far, or <c>null</c> when hashing failed. The stream
	/// stays usable, so the digest after a prefix and the digest after the whole can both be taken.
	/// </summary>
	public ReportContentDigest? TryGetDigest()
	{
		ReportContentDigest? digest = null;

		if (!HashingFailed && _hash is not null)
		{
			try
			{
				digest = new ReportContentDigest(_bytesWritten, _hash.GetCurrentHash());
			}
			catch (CryptographicException)
			{
				MarkFailed();
			}
			catch (PlatformNotSupportedException)
			{
				MarkFailed();
			}
		}

		return digest;
	}

	public override void Write(byte[] buffer, int offset, int count)
	{
		Write(new ReadOnlySpan<byte>(buffer, offset, count));
	}

	public override void Write(ReadOnlySpan<byte> buffer)
	{
		_inner.Write(buffer);
		_bytesWritten += buffer.Length;

		if (!HashingFailed && _hash is not null)
		{
			try
			{
				_hash.AppendData(buffer);
			}
			catch (CryptographicException)
			{
				MarkFailed();
			}
			catch (PlatformNotSupportedException)
			{
				MarkFailed();
			}
		}
	}

	public override void Flush()
	{
		_inner.Flush();
	}

	public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

	public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

	public override void SetLength(long value) => throw new NotSupportedException();

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			_hash?.Dispose();
			_hash = null;
			_inner.Dispose();
		}

		base.Dispose(disposing);
	}

	private static IncrementalHash CreateSha256() => IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

	private void MarkFailed()
	{
		HashingFailed = true;
		_hash?.Dispose();
		_hash = null;
	}
}
