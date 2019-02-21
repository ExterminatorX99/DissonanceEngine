﻿using OpenTK.Graphics.OpenGL;
using PrimitiveTypeGL = OpenTK.Graphics.OpenGL.PrimitiveType;

namespace GameEngine.Graphics
{
	public class PostProcessPass : RenderPass
	{
		public PostProcessPass(string name) : base(name) {}

		public override void Render() //TODO: Make this apply uniforms properly
		{
			Framebuffer.BindWithDrawBuffers(framebuffer);

			GL.Viewport(0,0,Screen.Width,Screen.Height);

			Shader.SetShader(passShader);
			OpenTK.Vector3 ambientCol = Rendering.ambientColor;
			GL.Uniform3(GL.GetUniformLocation(passShader.program,"ambientColor"),ref ambientCol); //code to replace
			if(passShader.hasDefaultUniform[DefaultShaderUniforms.ScreenWidth]) {
				GL.Uniform1(passShader.defaultUniformIndex[DefaultShaderUniforms.ScreenWidth],Screen.Width);
			}
			if(passShader.hasDefaultUniform[DefaultShaderUniforms.ScreenHeight]) {
				GL.Uniform1(passShader.defaultUniformIndex[DefaultShaderUniforms.ScreenHeight],Screen.Height);
			}
			
			if(passedTextures!=null) {
				for(int i=0;i<passedTextures.Length;i++) {
					var texture = passedTextures[i];
					GL.ActiveTexture((TextureUnit)((int)TextureUnit.Texture0+i));
					GL.BindTexture(TextureTarget.Texture2D,texture.Id);
					if(passShader!=null) {
						int location = GL.GetUniformLocation(passShader.program,texture.name);
						GL.Uniform1(location,i);
					}
				}
			}

			GL.Begin(PrimitiveTypeGL.Quads);
				GL.Vertex2(	-1.0f,1.0f);
				GL.TexCoord2(0.0f,1.0f);
				GL.Vertex2(	-1.0f,-1.0f);
				GL.TexCoord2(0.0f,0.0f);
				GL.Vertex2(	1.0f,-1.0f);
				GL.TexCoord2(1.0f,0.0f);
				GL.Vertex2(	1.0f,1.0f);
				GL.TexCoord2(1.0f,1.0f);
			GL.End();
		}
	}
}