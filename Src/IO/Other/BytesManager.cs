using System.IO;

namespace Dissonance.Engine.IO
{
	public class BytesManager : AssetManager<byte[]>
	{
		public override string[] Extensions { get; } = new[] { ".bytes" };

		public override byte[] Import(Stream stream, string filePath)
		{
			byte[] bytes = new byte[stream.Length];

			stream.Read(bytes, 0, bytes.Length);

			return bytes;
		}

		public override void Export(byte[] bytes, Stream stream)
		{
			stream.Write(bytes, 0, bytes.Length);
		}
	}
}
