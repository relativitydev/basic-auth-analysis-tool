using MigrateAuth.Cli.Session;

using Xunit;

namespace MigrateAuth.Cli.Tests.Session;

/// <summary>
/// The size bound on an untrusted file applies to the opened stream and to the bytes it delivers,
/// not to a length read from the directory entry before the open.
/// </summary>
public sealed class ReadCeilingEnforcingStreamTests
{
	private const int _CEILING_IN_BYTES = 8;

	[Fact]
	public void AnOpenedStreamWithinTheCeilingIsReadInFull()
	{
		using MemoryStream opened = new(new byte[_CEILING_IN_BYTES]);
		using ReadCeilingEnforcingStream bounded = ReadCeilingEnforcingStream.Wrap(opened, _CEILING_IN_BYTES);
		using MemoryStream copy = new();

		bounded.CopyTo(copy);

		Assert.Equal(_CEILING_IN_BYTES, copy.Length);
	}

	[Fact]
	public void AnOpenedStreamLongerThanTheCeilingIsRefusedBeforeAnyByteIsRead()
	{
		using MemoryStream opened = new(new byte[_CEILING_IN_BYTES + 1]);

		Assert.Throws<ReadCeilingExceededException>(() => ReadCeilingEnforcingStream.Wrap(opened, _CEILING_IN_BYTES));
		Assert.Equal(0, opened.Position);
	}

	/// <summary>
	/// A file that passed the length check and then grew: the stream reports a length within the
	/// ceiling and delivers more, which is exactly what a check made before the read cannot see.
	/// </summary>
	[Fact]
	public void AStreamThatDeliversMoreThanItsReportedLengthIsRefusedAtTheCeiling()
	{
		using UnderreportingStream opened = new(new byte[_CEILING_IN_BYTES * 4]);
		using ReadCeilingEnforcingStream bounded = ReadCeilingEnforcingStream.Wrap(opened, _CEILING_IN_BYTES);
		using MemoryStream copy = new();

		Assert.Throws<ReadCeilingExceededException>(() => bounded.CopyTo(copy));
		Assert.True(copy.Length <= _CEILING_IN_BYTES * 2);
	}

	private sealed class UnderreportingStream(byte[] content) : MemoryStream(content)
	{
		public override long Length => 1;
	}
}
