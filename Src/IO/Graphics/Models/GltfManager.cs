using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Dissonance.Engine.Graphics;
using Newtonsoft.Json;

namespace Dissonance.Engine.IO.Graphics.Models
{
	public partial class GltfManager : AssetManager<AssetPack>
	{
		public const uint FormatHeader = 0x46546C67;

		private const uint ChunkJson = 0x4E4F534A;
		private const uint ChunkBinary = 0x004E4942;

		public static readonly Dictionary<ComponentType,uint> ComponentTypeSizes = new Dictionary<ComponentType,uint> {
			{ ComponentType.SByte,  1 },
			{ ComponentType.Byte,   1 },
			{ ComponentType.Short,  2 },
			{ ComponentType.UShort, 2 },
			{ ComponentType.UInt,   4 },
			{ ComponentType.Float,	4 },
		};
		public static readonly Dictionary<string,uint> AccessorTypeSizes = new Dictionary<string,uint>(StringComparer.InvariantCultureIgnoreCase) {
			{ "SCALAR",	1 },
			{ "VEC2",	2 },
			{ "VEC3",	3 },
			{ "VEC4",	4 },
			{ "MAT2",	4 },
			{ "MAT3",	9 },
			{ "MAT4",	16 },
		};
		public static readonly Dictionary<string,Type> AttributeToType = new Dictionary<string,Type>(StringComparer.InvariantCultureIgnoreCase) {
			{ "POSITION",		typeof(VertexAttribute) },
			{ "NORMAL",			typeof(NormalAttribute) },
			{ "TANGENT",		typeof(TangentAttribute) },
			{ "TEXCOORD_0",		typeof(Uv0Attribute) },
			//{ "TEXCOORD_1",	typeof(Uv1Attribute) },
			{ "COLOR_0",		typeof(ColorAttribute) },
			//{ "JOINTS_0",		typeof(BoneIndicesAttribute) },
			//{ "WEIGHTS_0",	typeof(BoneWeightsAttribute) },
		};

		public override string[] Extensions => new[] { ".gltf",".glb" };

		public override AssetPack Import(Stream stream,string fileName)
		{
			GltfJson json = null;
			Stream blobStream = null;

			if(fileName.EndsWith(".gltf")) {
				byte[] textBytes = new byte[stream.Length-stream.Position];

				stream.Read(textBytes,0,textBytes.Length);

				HandleGltf(textBytes,ref json);
			} else {
				HandleGlb(stream,ref json,ref blobStream);
			}

			//TODO: Add extension support.
			if(json.extensionsRequired!=null) {
				foreach(var requiredExtension in json.extensionsRequired) {
					throw new FileLoadException($"glTF Error: Required extension '{requiredExtension}' is not supported.");
				}
			}

			var assets = new AssetPack();

			LoadMeshes(json,assets,blobStream);

			return assets;
		}

		protected static void HandleGltf(byte[] textBytes,ref GltfJson json)
		{
			json = JsonConvert.DeserializeObject<GltfJson>(Encoding.UTF8.GetString(textBytes));

			File.WriteAllText("Gltf.json",JsonConvert.SerializeObject(json,Formatting.Indented,new JsonSerializerSettings() { DefaultValueHandling = DefaultValueHandling.Ignore }));
		}
		protected static void HandleGlb(Stream stream,ref GltfJson json,ref Stream blobStream)
		{
			using var reader = new BinaryReader(stream);

			ReadHeader(reader,out _,out uint length);

			int chunkId = 0;

			while(reader.BaseStream.Position<length) {
				//Read Chunk
				uint chunkLength = reader.ReadUInt32();
				var chunkType = reader.ReadUInt32();
				var chunkData = reader.ReadBytes((int)chunkLength);

				if(chunkId==0 && chunkType!=ChunkJson) {
					static string ToAscii(uint value) => Encoding.ASCII.GetString(BitConverter.GetBytes(value));

					throw new FileLoadException($"glTF Error: First chunk was expected to be '{ToAscii(ChunkJson)}', but is '{ToAscii(chunkType)}'.");
				}

				switch(chunkType) {
					case ChunkJson:
						HandleGltf(chunkData,ref json);
						break;
					case ChunkBinary:
						blobStream = new MemoryStream(chunkData);
						break;
				}

				chunkId++;
			}
		}

		protected static void ReadHeader(BinaryReader reader,out uint version,out uint length)
		{
			uint magic = reader.ReadUInt32();

			if(magic!=FormatHeader) {
				throw new FileLoadException("glTF Error: File is not of 'Binary glTF' format.");
			}

			version = reader.ReadUInt32();
			length = reader.ReadUInt32();
		}
		protected static byte[] GetAccessorData(GltfJson json,GltfJson.Accessor accessor,Stream blobStream)
		{
			var bufferView = accessor.bufferView.HasValue ? json.bufferViews[accessor.bufferView.Value] : null;

			int elementSize = (int)(AccessorTypeSizes[accessor.type]*ComponentTypeSizes[accessor.componentType]);
			int fullSize = (int)(bufferView?.byteLength ?? accessor.count*elementSize);

			byte[] data = new byte[fullSize];

			if(bufferView!=null) {
				uint bufferId = bufferView.buffer;
				var buffer = json.buffers[bufferId];

				Stream stream;

				if(buffer.uri==null) {
					//Read from blob.

					if(bufferId!=0) {
						throw new FileLoadException($"glTF Error: Buffer {bufferId} is missing 'uri' property. Only the first buffer in a .glb file is allowed to not have one.");
					}

					stream = blobStream;
				} else {
					//Read from uri.

					stream = File.OpenRead(buffer.uri);
				}

				if(!stream.CanSeek) {
					throw new FileLoadException("glTF Error: Stream is not seekable. This shouldn't happen.");
				}

				stream.Seek(bufferView.byteOffset+accessor.byteOffset,SeekOrigin.Begin);

				if(bufferView.byteStride==0) {
					stream.Read(data,0,(int)bufferView.byteLength);
				} else {
					int bytesRead = 0;

					while(bytesRead<bufferView.byteLength) {
						stream.Read(data,bytesRead,elementSize);

						bytesRead += elementSize;

						stream.Seek(bufferView.byteStride,SeekOrigin.Current);
					}
				}
			}

			return data;
		}

		private static void LoadMeshes(GltfJson json,AssetPack assets,Stream blobStream)
		{
			if(json.meshes==null || json.meshes.Length==0) {
				return;
			}

			foreach(var jsonMesh in json.meshes) {
				var meshes = new List<Mesh>();

				foreach(var jsonPrimitive in jsonMesh.primitives) {
					var mesh = new Mesh();

					if(jsonPrimitive.mode.HasValue) {
						mesh.PrimitiveType = jsonPrimitive.mode.Value;
					}

					if(jsonPrimitive.indices.HasValue) {
						var jsonAccessor = json.accessors[jsonPrimitive.indices.Value];

						var data = GetAccessorData(json,jsonAccessor,blobStream);

						mesh.IndexBuffer.SetData<ushort>(data,value => value);
					}

					foreach(var pair in jsonPrimitive.attributes) {
						var attributeName = pair.Key;
						var jsonAccessor = json.accessors[pair.Value];

						if(!AttributeToType.TryGetValue(attributeName,out var attributeType)) {
							continue;
						}

						var attribute = CustomVertexAttribute.GetInstance(attributeType);
						var buffer = mesh.GetBuffer(attribute.BufferType);

						var data = GetAccessorData(json,jsonAccessor,blobStream);

						buffer.SetData(data);
					}

					mesh.Apply();

					meshes.Add(mesh);
				}

				var model = new Model() {
					meshes = meshes.ToArray()
				};

				assets.Add(model,jsonMesh.name);
			}
		}
	}
}
