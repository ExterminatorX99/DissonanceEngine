using System;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using DSU = GameEngine.Graphics.DefaultShaderUniforms;
using GLShaderType = OpenTK.Graphics.OpenGL.ShaderType;

namespace GameEngine.Graphics
{
	public partial class Shader : Asset<Shader>
	{
		//TODO: Implement OnDispose
		internal static Shader activeShader;

		//TODO: Initialize these after Graphics.Init();
		internal static Dictionary<string,Shader> shadersByName = new Dictionary<string,Shader>(StringComparer.OrdinalIgnoreCase);
		internal static List<Shader> shaders = new List<Shader>();
		internal Dictionary<string,ShaderUniform> uniforms;
		internal bool[] hasDefaultUniform = new bool[DSU.Count];
		internal int[] defaultUniformIndex = new int[DSU.Count];
		internal List<Material> materialAttachments = new List<Material>();

		public readonly string Name;

		public int program;
		public int vertexShader;
		public int fragmentShader;
		public int geometryShader;
		public string[] defines;
		public BlendingFactor blendFactorSrc;
		public BlendingFactor blendFactorDst;
		public CullMode cullMode = CullMode.Front;
		public PolygonMode polygonMode = PolygonMode.Fill;
		
		private static Shader errorShader;
		public static Shader ErrorShader => errorShader ??= Resources.Find<Shader>("Error");

		public override string GetAssetName() => Name;

		private Shader(string name)
		{
			this.Name = name;
		}

		public override string ToString() => Name;

		private void Init()
		{
			if(shadersByName.TryGetValue(Name,out var oldShader) && oldShader!=null) {
				oldShader.Dispose();
				shaders.Remove(oldShader);
			}

			shadersByName[Name] = this;
			shaders.Add(this);

			//Set uniform locations
			uniforms = new Dictionary<string,ShaderUniform>();
			GL.GetProgram(program,GetProgramParameterName.ActiveUniforms,out int uniformCount);

			const int MaxUniformNameLength = 32;

			for(int location = 0;location<uniformCount;location++) {
				GL.GetActiveUniform(program,location,MaxUniformNameLength,out int length,out int size,out ActiveUniformType uniformType,out string uniformName);

				uniforms.Add(uniformName,new ShaderUniform(uniformName,uniformType,location));
				
				//Optimization for engine's uniforms
				int indexOf = Array.IndexOf(DSU.names,uniformName);
				if(indexOf>=0) {
					hasDefaultUniform[indexOf] = true;
					defaultUniformIndex[indexOf] = location;
				}
			}

			Rendering.CheckGLErrors();
		}

		internal void SetupCommonUniforms()
		{
			if(hasDefaultUniform[DSU.ScreenWidth]) {
				GL.Uniform1(defaultUniformIndex[DSU.ScreenWidth],Screen.Width);
			}
			if(hasDefaultUniform[DSU.ScreenHeight]) {
				GL.Uniform1(defaultUniformIndex[DSU.ScreenHeight],Screen.Height);
			}
			if(hasDefaultUniform[DSU.ScreenResolution]) {
				GL.Uniform2(defaultUniformIndex[DSU.ScreenResolution],Screen.sizeFloat);
			}
			if(hasDefaultUniform[DSU.Time]) {
				GL.Uniform1(defaultUniformIndex[DSU.Time],Time.renderTime);
			}
		}
		internal void SetupCameraUniforms(Camera camera,Vector3 cameraPos)
		{
			if(hasDefaultUniform[DSU.NearClip]) {
				GL.Uniform1(defaultUniformIndex[DSU.NearClip],camera.nearClip);
			}
			if(hasDefaultUniform[DSU.FarClip]) {
				GL.Uniform1(defaultUniformIndex[DSU.FarClip],camera.farClip);
			}
			if(hasDefaultUniform[DSU.CameraPosition]) {
				GL.Uniform3(defaultUniformIndex[DSU.CameraPosition],cameraPos);
			}
			if(hasDefaultUniform[DSU.CameraDirection]) {
				GL.Uniform3(defaultUniformIndex[DSU.CameraDirection],camera.Transform.Forward);
			}
		}
		internal void SetupMatrixUniformsCached(Camera camera,Transform transform,bool[] uniformComputed,
			ref Matrix4x4 world,			ref Matrix4x4 worldInverse,
			ref Matrix4x4 worldView,		ref Matrix4x4 worldViewInverse,
			ref Matrix4x4 worldViewProj,	ref Matrix4x4 worldViewProjInverse,
			ref Matrix4x4 view,				ref Matrix4x4 viewInverse,
			ref Matrix4x4 proj,				ref Matrix4x4 projInverse
		){
			//Heavily optimized shitcode below, forgive me future me
			//no

			#region World
			//bool needsWorld;
			if(hasDefaultUniform[DSU.World] || hasDefaultUniform[DSU.WorldInverse] || hasDefaultUniform[DSU.WorldView] || hasDefaultUniform[DSU.WorldViewInverse] || hasDefaultUniform[DSU.WorldViewProj] || hasDefaultUniform[DSU.WorldViewProjInverse]) {
				//Check
				if(!uniformComputed[DSU.World]) { world = transform.WorldMatrix; uniformComputed[DSU.World] = true; }

				if(hasDefaultUniform[DSU.World]) {
					//Assign
					UniformMatrix4(defaultUniformIndex[DSU.World],ref world);
				}
				if(hasDefaultUniform[DSU.WorldInverse]) {
					//Check, Assign
					if(!uniformComputed[DSU.WorldInverse]) { worldInverse = world.Inverted; uniformComputed[DSU.WorldInverse] = true; }
					UniformMatrix4(defaultUniformIndex[DSU.WorldInverse],ref worldInverse);
				}

				#region WorldView
				if(hasDefaultUniform[DSU.WorldView] || hasDefaultUniform[DSU.WorldViewInverse] || hasDefaultUniform[DSU.WorldViewProj] || hasDefaultUniform[DSU.WorldViewProjInverse]) {
					//Check
					if(!uniformComputed[DSU.WorldView]) { worldView = world*camera.matrix_view; uniformComputed[DSU.WorldView] = true; }

					if(hasDefaultUniform[DSU.WorldView]) {
						//Assign
						UniformMatrix4(defaultUniformIndex[DSU.WorldView],ref worldView);
					}
					if(hasDefaultUniform[DSU.WorldViewInverse]) {
						//Check, Assign
						if(!uniformComputed[DSU.WorldViewInverse]) { worldViewInverse = worldView.Inverted; uniformComputed[DSU.WorldViewInverse] = true; }
						UniformMatrix4(defaultUniformIndex[DSU.WorldViewInverse],ref worldViewInverse);
					}

					#region WorldViewProj
					if(hasDefaultUniform[DSU.WorldViewProj] || hasDefaultUniform[DSU.WorldViewProjInverse]) {
						//Check
						if(!uniformComputed[DSU.WorldViewProj]) { worldViewProj = worldView*camera.matrix_proj; uniformComputed[DSU.WorldViewProj] = true; }

						if(hasDefaultUniform[DSU.WorldViewProj]) {
							//Assign
							UniformMatrix4(defaultUniformIndex[DSU.WorldViewProj],ref worldViewProj);
						}
						if(hasDefaultUniform[DSU.WorldViewProjInverse]) {
							//Check, Assign
							if(!uniformComputed[DSU.WorldViewProjInverse]) { worldViewProjInverse = worldViewProj.Inverted; uniformComputed[DSU.WorldViewProjInverse] = true; }
							UniformMatrix4(defaultUniformIndex[DSU.WorldViewProjInverse],ref worldViewProjInverse);
						}
					}
					#endregion
				}
				#endregion
			}
			#endregion
			#region View
			if(hasDefaultUniform[DSU.View])			{ UniformMatrix4(defaultUniformIndex[DSU.View],			ref view); }
			if(hasDefaultUniform[DSU.ViewInverse])	{ UniformMatrix4(defaultUniformIndex[DSU.ViewInverse],	ref viewInverse); }
			#endregion
			#region Proj
			if(hasDefaultUniform[DSU.Proj])			{ UniformMatrix4(defaultUniformIndex[DSU.Proj],			ref proj); }
			if(hasDefaultUniform[DSU.ProjInverse])	{ UniformMatrix4(defaultUniformIndex[DSU.ProjInverse],	ref projInverse); }
			#endregion
		}
		internal void SetupMatrixUniforms(ref Camera camera,ref Vector3 cameraPos,Transform transform,
			ref Matrix4x4 world,			ref Matrix4x4 worldInverse,
			ref Matrix4x4 worldView,		ref Matrix4x4 worldViewInverse,
			ref Matrix4x4 worldViewProj,	ref Matrix4x4 worldViewProjInverse,
			ref Matrix4x4 view,				ref Matrix4x4 viewInverse,
			ref Matrix4x4 proj,				ref Matrix4x4 projInverse,
			bool dontCalculateWorld = false
		){
			#region World
			if (hasDefaultUniform[DSU.World] || hasDefaultUniform[DSU.WorldInverse] || hasDefaultUniform[DSU.WorldView] || hasDefaultUniform[DSU.WorldViewInverse] || hasDefaultUniform[DSU.WorldViewProj] || hasDefaultUniform[DSU.WorldViewProjInverse]) {
				if(!dontCalculateWorld) {
					world = transform.WorldMatrix;
				}

				if(hasDefaultUniform[DSU.World]) {
					UniformMatrix4(defaultUniformIndex[DSU.World],ref world);
				}
				if(hasDefaultUniform[DSU.WorldInverse]) {
					worldInverse = world.Inverted;
					UniformMatrix4(defaultUniformIndex[DSU.WorldInverse],ref worldInverse);
				}

				#region WorldView
				if(hasDefaultUniform[DSU.WorldView] || hasDefaultUniform[DSU.WorldViewInverse] || hasDefaultUniform[DSU.WorldViewProj] || hasDefaultUniform[DSU.WorldViewProjInverse]) {
					worldView = world*camera.matrix_view;

					if(hasDefaultUniform[DSU.WorldView]) {
						UniformMatrix4(defaultUniformIndex[DSU.WorldView],ref worldView);
					}
					if(hasDefaultUniform[DSU.WorldViewInverse]) {
						worldViewInverse = worldView.Inverted;
						UniformMatrix4(defaultUniformIndex[DSU.WorldViewInverse],ref worldViewInverse);
					}

					#region WorldViewProj
					if(hasDefaultUniform[DSU.WorldViewProj] || hasDefaultUniform[DSU.WorldViewProjInverse]) {
						worldViewProj = worldView*camera.matrix_proj;

						if(hasDefaultUniform[DSU.WorldViewProj]) {
							UniformMatrix4(defaultUniformIndex[DSU.WorldViewProj],ref worldViewProj);
						}
						if(hasDefaultUniform[DSU.WorldViewProjInverse]) {
							worldViewProjInverse = worldViewProj.Inverted;
							UniformMatrix4(defaultUniformIndex[DSU.WorldViewProjInverse],ref worldViewProjInverse);
						}
					}
					#endregion
				}
				#endregion
			}
			#endregion

			#region View
			if(hasDefaultUniform[DSU.View]) {
				UniformMatrix4(defaultUniformIndex[DSU.View],ref view);
			}
			if(hasDefaultUniform[DSU.ViewInverse]) {
				UniformMatrix4(defaultUniformIndex[DSU.ViewInverse],ref viewInverse);
			}
			#endregion

			#region Proj
			if(hasDefaultUniform[DSU.Proj]) {
				UniformMatrix4(defaultUniformIndex[DSU.Proj],ref proj);
			}
			if(hasDefaultUniform[DSU.ProjInverse]) {
				UniformMatrix4(defaultUniformIndex[DSU.ProjInverse],ref projInverse);
			}
			#endregion
		}

		public int GetUniformLocation(string uniformName) => uniforms[uniformName].location;
		public bool TryGetUniformLocation(string uniformName,out int location)
		{
			if(uniforms.TryGetValue(uniformName,out var uniform)) {
				location = uniform.location;
				return true;
			}

			location = -1;
			return false;
		}

		internal void MaterialDetach(Material material) => materialAttachments.Remove(material);
		internal void MaterialAttach(Material material) => materialAttachments.Add(material);
		internal void CompileShader(ShaderType type,string code,string shaderName = "")
		{
			code = code.Trim();

			//Some broken Nvidia drivers don't support 'f' suffix, even though it was added in GLSL 1.2 decades ago. Zoinks.
			code = RegexCache.shaderFSuffixB.Replace(code,@"$1$2");
			code = RegexCache.shaderFSuffixA.Replace(code,@"$1$2.0");
			
			int shader = GL.CreateShader((GLShaderType)type);
			GL.ShaderSource(shader,code);
			GL.CompileShader(shader);

			string info = GL.GetShaderInfoLog(shader);

			if(!string.IsNullOrEmpty(info)) {
				Debug.Log($"Error compilling shader:\r\n{info}\r\n\r\n{code}");

				GL.DeleteShader(shader);

				if(type==ShaderType.Vertex) {
					GL.AttachShader(program,ErrorShader.vertexShader);
				}else if(type==ShaderType.Fragment) {
					GL.AttachShader(program,ErrorShader.fragmentShader);
				}
			}else{
				GL.AttachShader(program,shader);
			}

			if(Rendering.CheckGLErrors(false)) {
				throw new GraphicsException($"Unable to compile '{type}' shader '{shaderName}'");
			}
		}

		internal static void SetShader(Shader shader)
		{
			if(shader!=null) {
				if(shader!=activeShader) {
					GL.UseProgram(shader.program);
					activeShader = shader;

					Rendering.SetBlendFunc(shader.blendFactorSrc,shader.blendFactorDst);
				}
			}else if(activeShader!=null) {
				GL.UseProgram(0);
				activeShader = null;
			}
		}

		internal static Shader FromCode(string name,string vertexCode,string fragmentCode = "",string geometryCode = "",string[] defines = null)
		{
			//Debug.Log("Compiling shader "+name);
			
			if(defines!=null && defines.Length==0) {
				defines = null;
			}

			if(defines!=null) {
				string defString = "";

				void PrepareCode(ref string code)
				{
					if(string.IsNullOrEmpty(code)) {
						return;
					}

					int index = code.IndexOf("version",StringComparison.Ordinal);
					if(index>=0) {
						index = code.IndexOf("\n",index,StringComparison.Ordinal)+1;
						code = code.Insert(index,defString);
					}
				}

				for(int i=0;i<defines.Length;i++) {
					defString += "#define "+defines[i]+" \n";
				}

				PrepareCode(ref vertexCode);
				PrepareCode(ref fragmentCode);
				PrepareCode(ref geometryCode);
			}

			var shader = new Shader(name) {
				defines = defines,
				program = GL.CreateProgram()
			};

			void TryCompileCode(Shader s,ShaderType shaderType,string code)
			{
				if(!string.IsNullOrEmpty(code)) {
					s.CompileShader(shaderType,code,name);
				}
			}

			TryCompileCode(shader,ShaderType.Vertex,vertexCode);
			TryCompileCode(shader,ShaderType.Fragment,fragmentCode);
			TryCompileCode(shader,ShaderType.Geometry,geometryCode);

			GL.BindAttribLocation(shader.program,(int)AttributeId.Vertex,"vertex");
			GL.BindAttribLocation(shader.program,(int)AttributeId.Normal,"normal");
			GL.BindAttribLocation(shader.program,(int)AttributeId.Tangent,"tangent");
			GL.BindAttribLocation(shader.program,(int)AttributeId.Color,"color");
			GL.BindAttribLocation(shader.program,(int)AttributeId.BoneIndices,"boneIndices");
			GL.BindAttribLocation(shader.program,(int)AttributeId.BoneWeights,"boneWeights");
			GL.BindAttribLocation(shader.program,(int)AttributeId.Uv0,"uv0");
			
			GL.LinkProgram(shader.program);

			shader.Init();

			return shader;
		}
		internal static unsafe void UniformMatrix4(int location,ref Matrix4x4 matrix,bool transpose = false)
		{
			fixed(float* matrix_ptr = &matrix.m00) {
				GL.UniformMatrix4(location,1,transpose,matrix_ptr);
			}
		}
	}
}