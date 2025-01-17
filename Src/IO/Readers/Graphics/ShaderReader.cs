using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Dissonance.Engine.Graphics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Dissonance.Engine.IO
{
	public partial class ShaderReader : IAssetReader<Asset<Shader>[]>
	{
		public string[] Extensions { get; } = { ".program" };

		public bool AutoloadAssets => true;

		public async ValueTask<Asset<Shader>[]> ReadFromStream(Stream stream, string assetPath, MainThreadCreationContext switchToMainThread)
		{
			using var reader = new StreamReader(stream);
			
			string jsonText = reader.ReadToEnd();
			var shaders = new List<Asset<Shader>>();

			var jsonShaders = Assets.Get<JObject>(assetPath, AssetRequestMode.ImmediateLoad).Value.ToObject<Dictionary<string, JsonShaderProgram>>();

			await switchToMainThread;

			foreach (var pair in jsonShaders) {
				string name = pair.Key;
				var jsonShader = pair.Value;

				string vertexCode = Assets.Get<string>(jsonShader.VertexShader, AssetRequestMode.ImmediateLoad).Value;
				string fragmentCode = Assets.Get<string>(jsonShader.FragmentShader, AssetRequestMode.ImmediateLoad).Value;
				string geometryCode = !string.IsNullOrWhiteSpace(jsonShader.GeometryShader) ? Assets.Get<string>(jsonShader.GeometryShader, AssetRequestMode.ImmediateLoad).Value : null;

				var shader = Shader.FromCode(name, vertexCode, fragmentCode, geometryCode, jsonShader.ShaderDefines);

				shader.Priority = jsonShader.Queue;
				shader.CullMode = jsonShader.CullMode;
				shader.PolygonMode = jsonShader.PolygonMode;
				shader.BlendFactorSrc = jsonShader.BlendFactorSrc;
				shader.BlendFactorDst = jsonShader.BlendFactorDst;

				var shaderAsset = Assets.CreateLoaded(name, shader);

				shaders.Add(shaderAsset);
			}

			return shaders.ToArray();
		}
	}
}
