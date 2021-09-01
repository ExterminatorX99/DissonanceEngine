using System;
using System.IO;
using Dissonance.Engine.Audio;

namespace Dissonance.Engine.IO
{
	public class WavManager : AssetManager<AudioClip>
	{
		public override string[] Extensions { get; } = new[] { ".wav", ".wave" };

		public override AudioClip Import(Stream stream, string filePath)
		{
			using var reader = new BinaryReader(stream);

			// RIFF header
			string signature = new(reader.ReadChars(4));

			if (signature != "RIFF") {
				throw new NotSupportedException("Specified stream is not a wave file.");
			}

			int riffChunckSize = reader.ReadInt32();
			string format = new(reader.ReadChars(4));

			if (format != "WAVE") {
				throw new NotSupportedException("Specified stream is not a wave file.");
			}

			// WAVE header
			string formatSignature = new(reader.ReadChars(4));

			if (formatSignature != "fmt ") {
				throw new NotSupportedException("Specified wave file is not supported.");
			}

			int formatChunkSize = reader.ReadInt32();
			int audioFormat = reader.ReadInt16();
			int channelsNum = reader.ReadInt16();
			int sampleRate = reader.ReadInt32();
			int byteRate = reader.ReadInt32();
			int blockAlign = reader.ReadInt16();
			int bitsPerSample = reader.ReadInt16();

			string dataSignature = new(reader.ReadChars(4));

			if (dataSignature != "data") {
				throw new NotSupportedException("Specified wave file is not supported.");
			}

			int dataChunkSize = reader.ReadInt32();

			byte[] data = reader.ReadBytes(dataChunkSize);
			var clip = new AudioClip();

			clip.SetData(data, channelsNum, bitsPerSample / 8, sampleRate);

			return clip;
		}
	}
}
