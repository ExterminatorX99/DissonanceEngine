using System;
using System.Linq;
using GameEngine.Graphics;
using GameEngine.Utils;
using OpenTK.Graphics.OpenGL;
using PrimitiveTypeGL = OpenTK.Graphics.OpenGL.PrimitiveType;

#pragma warning disable 0649

namespace GameEngine
{
	public struct BoneWeight
	{
		public int boneIndex0;
		public int boneIndex1;
		public int boneIndex2;
		public int boneIndex3;
		public float weight0;
		public float weight1;
		public float weight2;
		public float weight3;
	}

	public class Mesh : Asset<Mesh>
	{
		//TODO: Implement OnDispose

		public string name;
		public int[] triangles;
		public Vector3[] vertices;
		public Vector3[] normals;
		public Vector2[] uv;
		public Vector4[] colors;
		public Vector4[] tangents;
		public BoneWeight[] boneWeights;
		public Vector3 boundsCenter;
		public Vector3 boundsExtent;

		internal int vertexBufferId = -1;
		internal int indexBufferId = -1;
		internal int vertexSize;
		internal bool isReady;

		public virtual void DrawMesh(bool skipAttributes = false)
		{
			int vertexDataOffset = 0;

			void TrySubmitAttribute(bool active,int attributeId,int size,VertexAttribPointerType pointerType,bool normalized,int stride)
			{
				if(active) {
					GL.EnableVertexAttribArray(attributeId);
					GL.VertexAttribPointer(attributeId,size,pointerType,normalized,stride,(IntPtr)vertexDataOffset);

					switch(pointerType) {
						case VertexAttribPointerType.Byte:
						case VertexAttribPointerType.UnsignedByte:
							vertexDataOffset += size;
							break;
						case VertexAttribPointerType.Short:
						case VertexAttribPointerType.HalfFloat:
						case VertexAttribPointerType.UnsignedShort:
							vertexDataOffset += size*sizeof(short);
							break;
						case VertexAttribPointerType.Int:
						case VertexAttribPointerType.Float:
						case VertexAttribPointerType.UnsignedInt:
						case VertexAttribPointerType.UnsignedInt10F11F11FRev:
						case VertexAttribPointerType.UnsignedInt2101010Rev:
							vertexDataOffset += size*sizeof(int);
							break;
						case VertexAttribPointerType.Double:
							vertexDataOffset += size*sizeof(long);
							break;
					}
				} else {
					GL.DisableVertexAttribArray(attributeId);
				}
			}

			//Vertices
			GL.BindBuffer(BufferTarget.ArrayBuffer,vertexBufferId);
			GL.VertexAttribPointer((int)AttributeId.Vertex,3,VertexAttribPointerType.Float,false,vertexSize,(IntPtr)vertexDataOffset);

			vertexDataOffset += sizeof(float)*3;

			if(!skipAttributes) {
				//Normals
				TrySubmitAttribute(normals!=null,(int)AttributeId.Normal,3,VertexAttribPointerType.Float,true,vertexSize);

				//Tangents
				TrySubmitAttribute(tangents!=null,(int)AttributeId.Tangent,4,VertexAttribPointerType.Float,false,vertexSize);

				//Colors
				TrySubmitAttribute(colors!=null,(int)AttributeId.Color,4,VertexAttribPointerType.Float,false,vertexSize);

				//BoneWeights
				bool hasWeights = boneWeights!=null;
				TrySubmitAttribute(hasWeights,(int)AttributeId.BoneIndices,4,VertexAttribPointerType.Int,false,vertexSize);
				TrySubmitAttribute(hasWeights,(int)AttributeId.BoneWeights,4,VertexAttribPointerType.Float,false,vertexSize);

				//UVs
				TrySubmitAttribute(uv!=null,(int)AttributeId.Uv0,2,VertexAttribPointerType.Float,false,vertexSize);
			}

			//Triangles
			GL.BindBuffer(BufferTarget.ElementArrayBuffer,indexBufferId);
			GL.DrawElements(PrimitiveTypeGL.Triangles,triangles.Length,DrawElementsType.UnsignedInt,0);
		}

		public void Apply()
		{
			//Debug.StartStopwatch("meshApply");
			if(vertices==null || vertices.Length==0) {
				throw new ArgumentException("Mesh's vertice array cannot be null or empty");
			}
			/*if(triangles==null || triangles.Length==0) {
				throw new ArgumentException("Mesh's triangle array cannot be null or empty");
			}*/
			if(normals!=null && normals.Length!=vertices.Length) {
				throw new ArgumentException("Mesh's normals array is supposed to be the same length as vertices or be null");
			}
			if(tangents!=null && tangents.Length!=vertices.Length) {
				throw new ArgumentException("Mesh's tangents array is supposed to be the same length as vertices or be null");
			}
			if(colors!=null && colors.Length!=vertices.Length) {
				throw new ArgumentException("Mesh's colors array is supposed to be the same length as vertices or be null");
			}
			if(uv!=null && uv.Length!=vertices.Length) {
				throw new ArgumentException("Mesh's uv array is supposed to be the same length as vertices or be null");
			}

			float xMin = 0f; float xMax = 0f;
			float yMin = 0f; float yMax = 0f;
			float zMin = 0f; float zMax = 0f;

			vertexSize = 3+(uv!=null ? 2 : 0)+(normals!=null ? 3 : 0)+(colors!=null ? 4 : 0)+(tangents!=null ? 4 : 0)+(boneWeights!=null ? 8 : 0);

			var vertexData = new float[vertexSize*vertices.Length];

			vertexSize *= sizeof(float);

			int j = 0;
			for(int i = 0;i<vertices.Length;i++) {
				var v = vertices[i];

				//Bounding box stuff
				if(v.x>xMax) {
					xMax = v.x;
				}else if(v.x<xMin) {
					xMin = v.x;
				}

				if(v.y>yMax) {
					yMax = v.y;
				}else if(v.y<yMin) {
					yMin = v.y;
				}

				if(v.z>zMax) {
					zMax = v.z;
				}else if(v.z<zMin) {
					zMin = v.z;
				}

				//Map all data to 1D array
				vertexData[j++] = v.x;
				vertexData[j++] = v.y;
				vertexData[j++] = v.z;

				if(normals!=null) {
					var n = normals[i];

					vertexData[j++] = n.x;
					vertexData[j++] = n.y;
					vertexData[j++] = n.z;
				}

				if(tangents!=null) {
					var t = tangents[i];

					vertexData[j++] = t.x;
					vertexData[j++] = t.y;
					vertexData[j++] = t.z;
					vertexData[j++] = t.w;
				}

				if(colors!=null) {
					var c = colors[i];

					vertexData[j++] = c.x;
					vertexData[j++] = c.y;
					vertexData[j++] = c.z;
					vertexData[j++] = c.w;
				}

				if(boneWeights!=null) {
					var w = boneWeights[i];

					vertexData[j++] = w.boneIndex0;
					vertexData[j++] = w.boneIndex1;
					vertexData[j++] = w.boneIndex2;
					vertexData[j++] = w.boneIndex3;

					vertexData[j++] = w.weight0;
					vertexData[j++] = w.weight1;
					vertexData[j++] = w.weight2;
					vertexData[j++] = w.weight3;
				}

				if(uv!=null) {
					var vec = uv[i];

					vertexData[j++] = vec.x;
					vertexData[j++] = 1f-vec.y;
				}
			}

			boundsCenter = new Vector3((xMin+xMax)*0.5f,(yMin+yMax)*0.5f,(zMin+zMax)*0.5f);

			boundsExtent = new Vector3(
				Mathf.Max(Mathf.Abs(xMin),Mathf.Abs(xMax))-boundsCenter.x,
				Mathf.Max(Mathf.Abs(yMin),Mathf.Abs(yMax))-boundsCenter.y,
				Mathf.Max(Mathf.Abs(zMin),Mathf.Abs(zMax))-boundsCenter.z
			);

			static void PushData<T>(BufferTarget bufferTarget,ref int bufferId,int size,T[] data,BufferUsageHint usageHint) where T : struct
			{
				if(bufferId==-1) {
					bufferId = GL.GenBuffer();
				}

				GL.BindBuffer(BufferTarget.ArrayBuffer,bufferId);
				GL.BufferData(BufferTarget.ArrayBuffer,(IntPtr)size,data,usageHint);
			}

			//Push Vertices
			PushData(BufferTarget.ArrayBuffer,ref vertexBufferId,sizeof(float)*vertexData.Length,vertexData,BufferUsageHint.StaticDraw);

			//Push Triangles
			PushData(BufferTarget.ElementArrayBuffer,ref indexBufferId,sizeof(int)*triangles.Length,triangles,BufferUsageHint.StaticDraw);

			isReady = true;
		}
		public void RecalculateNormals()
		{
			var newNormals = new Vector3[vertices.Length];

			for(int i = 0;i<triangles.Length;i+=3) {
				int i1 = triangles[i];
				int i2 = triangles[i+1];
				int i3 = triangles[i+2];

				var v1 = vertices[i1];
				var v2 = vertices[i2];
				var v3 = vertices[i3];

				var normal = Vector3.Cross(v2-v1,v3-v1).Normalized;

				newNormals[i1] += normal;
				newNormals[i2] += normal;
				newNormals[i3] += normal;
			}

			var zero = Vector3.Zero;
			for(int i = 0;i<vertices.Length;i++) {
				if(newNormals[i]!=zero) {
					newNormals[i].Normalize();
				}
			}
			
			normals = newNormals;
		}
		public void RecalculateTangents()
		{
			if(uv==null) {
				throw new Exception("RecalculateTangents() requires a working UV array");
			}
			if(normals==null) {
				throw new Exception("RecalculateTangents() requires a working array of normals, call RecalculateNormals() first");
			}

			int verticeCount = vertices.Length;

			var tan1 = new Vector3[verticeCount];
			var tan2 = new Vector3[verticeCount];
			tangents = new Vector4[verticeCount];

			for(int i = 0;i<triangles.Length;i+=3) {
				int i1 = triangles[i];
				int i2 = triangles[i+1];
				int i3 = triangles[i+2];

				var v1 = vertices[i1];
				var v2 = vertices[i2];
				var v3 = vertices[i3];

				var w1 = uv[i1];
				var w2 = uv[i2];
				var w3 = uv[i3];

				float x1 = v2.x-v1.x;
				float x2 = v3.x-v1.x;
				float y1 = v2.y-v1.y;
				float y2 = v3.y-v1.y;
				float z1 = v2.z-v1.z;
				float z2 = v3.z-v1.z;

				float s1 = w2.x-w1.x;
				float s2 = w3.x-w1.x;
				float t1 = w2.y-w1.y;
				float t2 = w3.y-w1.y;

				float r = 1f/(s1*t2-s2*t1);
				var sdir = new Vector3((t2*x1-t1*x2)*r,(t2*y1-t1*y2)*r,(t2*z1-t1*z2)*r);
				var tdir = new Vector3((s1*x2-s2*x1)*r,(s1*y2-s2*y1)*r,(s1*z2-s2*z1)*r);

				tan1[i1] += sdir;
				tan1[i2] += sdir;
				tan1[i3] += sdir;

				tan2[i1] += tdir;
				tan2[i2] += tdir;
				tan2[i3] += tdir;
			}

			for(int i=0;i<verticeCount;i++)  {
				var n = normals[i];
				var t = tan1[i];

				//Gram-Schmidt orthogonalization
				float w = Vector3.Dot(Vector3.Cross(n,t),tan2[i])<0f ? -1f : 1f;
				tangents[i] = new Vector4((t-n*Vector3.Dot(n,t)).Normalized,w);
			}
		}

		internal void BufferData()
		{

		}

		public static Mesh CombineMeshes(params Mesh[] meshes)
		{
			int newVertexCount = meshes.Sum(m => m.vertices.Length);
			int newTriangleCount = meshes.Sum(m => m.triangles.Length);

			Mesh newMesh = new Mesh {
				triangles = new int[newTriangleCount],
				vertices = new Vector3[newVertexCount],
				normals = new Vector3[newVertexCount],
				uv = new Vector2[newVertexCount],
			};

			int vertex = 0;
			int triangleIndex = 0;

			for(int i = 0;i<meshes.Length;i++) {
				var mesh = meshes[i];

				//Vertices
				int vertexCount = mesh.vertices.Length;

				Array.Copy(mesh.vertices,0,newMesh.vertices,vertex,vertexCount);
				Array.Copy(mesh.normals,0,newMesh.normals,vertex,vertexCount);
				Array.Copy(mesh.uv,0,newMesh.uv,vertex,vertexCount);

				//Triangles
				int triangleIndexCount = mesh.triangles.Length;

				if(vertex==0) {
					Array.Copy(mesh.triangles,newMesh.triangles,triangleIndexCount);
				} else {
					for(int k = 0;k<triangleIndexCount;k++) {
						newMesh.triangles[triangleIndex+k] = mesh.triangles[k]+vertex;
					}
				}

				vertex += vertexCount;
				triangleIndex += triangleIndexCount;
			}

			newMesh.Apply();

			return newMesh;
		}
	}
}