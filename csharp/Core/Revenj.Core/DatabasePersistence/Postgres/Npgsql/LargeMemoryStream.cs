﻿using System;
using System.Collections.Generic;
using System.IO;

namespace Revenj.DatabasePersistence.Postgres.Npgsql
{
	internal class LargeMemoryStream : Stream
	{
		private const int BlockSize = 65536;
		private readonly List<byte[]> Blocks = new List<byte[]>();
		private int CurrentPosition;
		private int BlockRemaining;
		private int TotalSize;

		public LargeMemoryStream(Stream another, int size)
		{
			Position = 0;
			var buf = new byte[BlockSize];
			int read;
			while (size > 0)
			{
				read = another.Read(buf, 0, BlockSize < size ? BlockSize : size);
				Write(buf, 0, read);
				size -= read;
			}
			Position = 0;
		}

		public override bool CanRead { get { return true; } }
		public override bool CanSeek { get { return false; } }
		public override bool CanWrite { get { return true; } }
		public override void Flush() { }
		public override long Length { get { return TotalSize; } }

		public override long Position
		{
			get { return CurrentPosition; }
			set { CurrentPosition = (int)value; }
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			var off = CurrentPosition % BlockSize;
			var pos = CurrentPosition / BlockSize;
			var min = Math.Min(BlockSize - off, Math.Min(TotalSize - CurrentPosition, count));
			Buffer.BlockCopy(Blocks[pos], off, buffer, offset, min);
			CurrentPosition += min;
			return min;
		}

		public override long Seek(long offset, SeekOrigin origin) { return 0; }
		public override void SetLength(long value) { }

		public override void Write(byte[] buffer, int offset, int count)
		{
			int cur = count;
			while (cur > 0)
			{
				if (BlockRemaining == 0)
				{
					Blocks.Add(new byte[BlockSize]);
					BlockRemaining = BlockSize;
				}
				var min = cur < BlockRemaining ? cur : BlockRemaining;
				Buffer.BlockCopy(buffer, offset + count - cur, Blocks[Blocks.Count - 1], BlockSize - BlockRemaining, min);
				cur -= min;
				BlockRemaining -= min;
				CurrentPosition += min;
				TotalSize += min;
			}
		}
	}
}
