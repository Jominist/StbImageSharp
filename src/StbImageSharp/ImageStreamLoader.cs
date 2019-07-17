﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace StbImageSharp
{
	public unsafe class ImageStreamLoader
	{
		private Stream _stream;
		private byte[] _buffer = new byte[1024];

		private readonly StbImage.stbi_io_callbacks _callbacks;

		public ImageStreamLoader()
		{
			_callbacks = new StbImage.stbi_io_callbacks
			{
				read = ReadCallback,
				skip = SkipCallback,
				eof = Eof
			};
		}

		private int SkipCallback(void* user, int i)
		{
			return (int) _stream.Seek(i, SeekOrigin.Current);
		}

		private int Eof(void* user)
		{
			return _stream.CanRead ? 1 : 0;
		}

		private int ReadCallback(void* user, sbyte* data, int size)
		{
			if (size > _buffer.Length)
			{
				_buffer = new byte[size*2];
			}

			var res = _stream.Read(_buffer, 0, size);
			Marshal.Copy(_buffer, 0, new IntPtr(data), size);
			return res;
		}

		public Image Read(Stream stream, ColorComponents req_comp = ColorComponents.Default)
		{
			byte* result = null;

			_stream = stream;

			try
			{
				int x, y, comp;
				result = StbImage.stbi_load_from_callbacks(_callbacks, null, &x, &y, &comp, (int)req_comp);

				return Image.FromResult(result, x, y, (ColorComponents)comp, req_comp);
			}
			finally
			{
				if (result != null)
				{
					CRuntime.free(result);
				}
				_stream = null;
			}
		}

		public AnimatedFrame[] ReadAnimatedGif(Stream stream, ColorComponents req_comp = ColorComponents.Default)
		{
			try
			{
				_stream = stream;

				var res = new List<AnimatedFrame>();

				var context = new StbImage.stbi__context();
				StbImage.stbi__start_callbacks(context, _callbacks, null);

				if (StbImage.stbi__gif_test(context) == 0)
				{
					throw new Exception("Input stream is not GIF file.");
				}

				var g = new StbImage.stbi__gif();

				do
				{
					int comp;
					var result = StbImage.stbi__gif_load_next(context, g, &comp, (int)req_comp, null);
					if (result == null)
					{
						break;
					}

					var frame = new AnimatedFrame
					{
						Width = g.w,
						Height = g.h,
						SourceComp = (ColorComponents)comp,
						Comp = req_comp == ColorComponents.Default ? (ColorComponents)comp : req_comp,
						Delay = g.delay
					};

					frame.Data = new byte[g.w * g.h * (int)frame.Comp];
					Marshal.Copy(new IntPtr(result), frame.Data, 0, frame.Data.Length);

					CRuntime.free(result);
					res.Add(frame);
				} while (true);

				CRuntime.free(g._out_);

				return res.ToArray();
			}
			finally
			{
				_stream = null;
			}
		}
	}
}