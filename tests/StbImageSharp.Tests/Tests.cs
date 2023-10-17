﻿using Hebron.Runtime;
using NUnit.Framework;
using StbImageSharp.Tests.Utility;
using System;
using System.IO;
using System.Reflection;

namespace StbImageSharp.Tests
{
	[TestFixture]
	public class Tests
	{
		private static readonly Assembly _assembly = typeof(Tests).Assembly;

		[TestCase("The Public Domain_ Enclosing the Commons of the Mind.pdf")]
		[TestCase("empty")]
		public void LoadUnknownFormat(string filename)
		{
			Assert.Throws<InvalidOperationException>(() =>
			{ 
				using var stream = _assembly.OpenResourceStream(filename);
				using var result = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
			});
		}

		[TestCase("sample_1280×853.hdr", 1280, 853, ColorComponents.RedGreenBlue)]
		[TestCase("DockPanes.jpg", 609, 406, ColorComponents.RedGreenBlue)]
		public unsafe void Load(string filename, int width, int height, ColorComponents colorComponents)
		{
			using var stream = _assembly.OpenResourceStream(filename);
			using var result = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);

			Assert.IsNotNull(result);
			Assert.AreEqual(result.Width, width);
			Assert.AreEqual(result.Height, height);
			Assert.AreEqual(result.Comp, ColorComponents.RedGreenBlueAlpha);
			Assert.AreEqual(result.SourceComp, colorComponents);
			Assert.That(result.DataPtr is not null);
			Assert.AreEqual(result.Data.Length, result.Width * result.Height * 4);
		}

		[TestCase("sample_1280×853.hdr", 1280, 853, ColorComponents.RedGreenBlue)]
		public unsafe void LoadHdr(string filename, int width, int height, ColorComponents colorComponents)
		{
			using var stream = _assembly.OpenResourceStream(filename);
			using var result = ImageResultFloat.FromStream(stream, ColorComponents.RedGreenBlueAlpha);

			Assert.IsNotNull(result);
			Assert.AreEqual(result.Width, width);
			Assert.AreEqual(result.Height, height);
			Assert.AreEqual(result.Comp, ColorComponents.RedGreenBlueAlpha);
			Assert.AreEqual(result.SourceComp, colorComponents);
			Assert.That(result.DataPtr is not null);
			Assert.AreEqual(result.Data.Length, result.Width * result.Height * 4);
		}

		[TestCase("sample_1280×853.hdr", 2000, 1280, 853, ColorComponents.RedGreenBlue, false)]
		[TestCase("DockPanes.jpg", 2000, 609, 406, ColorComponents.RedGreenBlue, false)]
		public void Info(string filename, int headerSize, int width, int height, ColorComponents colorComponents, bool is16bit)
		{
			ImageInfo? result;

			var data = new byte[headerSize];
			using (var stream = _assembly.OpenResourceStream(filename))
			{
				stream.Read(data, 0, data.Length);
			}

			using (var stream = new MemoryStream(data))
			{
				result = ImageInfo.FromStream(stream);
			}

			Assert.IsNotNull(result);

			var info = result.Value;
			Assert.AreEqual(info.Width, width);
			Assert.AreEqual(info.Height, height);
			Assert.AreEqual(info.ColorComponents, colorComponents);
			Assert.AreEqual(info.BitsPerChannel, is16bit ? 16 : 8);
		}

		[TestCase("somersault.gif", 384, 480, ColorComponents.RedGreenBlueAlpha, 43)]
		public unsafe void AnimatedGifFrames(string fileName, int width, int height, ColorComponents colorComponents, int originalFrameCount)
		{
			using (var stream = _assembly.OpenResourceStream(fileName))
			{
				var frameCount = 0;
				foreach(var frame in ImageResult.AnimatedGifFramesFromStream(stream))
				{
					Assert.AreEqual(frame.Width, width);
					Assert.AreEqual(frame.Height, height);
					Assert.AreEqual(frame.Comp, colorComponents);
					Assert.That(frame.DataPtr is not null);
					Assert.AreEqual(frame.Data.Length, frame.Width * frame.Height * (int)frame.Comp);

					++frameCount;
				}

				Assert.AreEqual(frameCount, originalFrameCount);

				stream.Seek(0, SeekOrigin.Begin);
			}

			Assert.AreEqual(StbImage.NativeAllocations, 0);
		}
	}
}
