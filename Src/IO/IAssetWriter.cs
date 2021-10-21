using System.IO;

namespace Dissonance.Engine.IO
{
	public interface IAssetWriter<in T> where T : class
	{
		void WriteToStream(T value, Stream stream);
	}
}
