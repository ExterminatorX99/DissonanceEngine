﻿using System.Collections.Generic;
using Dissonance.Framework.Graphics;

namespace Dissonance.Engine.Graphics
{
	public struct LightingPassData : IRenderComponent
	{
		public struct LightData
		{
			public Light.LightType Type;
			public Matrix4x4 Matrix;
			public float Intensity;
			public float? Range;
			public Vector3 Color;
			public Vector3? Position;
			public Vector3? Direction;
		}

		public List<LightData> Lights { get; private set; }

		public void Reset()
		{
			if (Lights == null) {
				Lights = new List<LightData>();
			} else {
				Lights.Clear();
			}
		}
	}

	public class DeferredLightingPass : RenderPass
	{
		public readonly Shader[] ShadersByLightType = new Shader[2];

		public override void Render()
		{
			Framebuffer.BindWithDrawBuffers(Framebuffer);

			GL.Enable(EnableCap.Blend);
			GL.Enable(EnableCap.CullFace);
			GL.CullFace(CullFaceMode.Back);
			GL.DepthMask(false);

			// test if it equals 1
			// GL.StencilFunc(StencilFunction.Notequal, 0x01, 0x01);
			// GL.StencilMask(0);

			Matrix4x4 worldMatrix, inverseWorldMatrix = default,
			worldViewMatrix = default, inverseWorldViewMatrix = default,
			worldViewProjMatrix = default, inverseWorldViewProjMatrix = default;

			var renderViewData = GlobalGet<RenderViewData>();
			var lightingData = GlobalGet<LightingPassData>();

			foreach (var renderView in renderViewData.RenderViews) {
				var camera = renderView.camera;
				var cameraTransform = renderView.transform;
				var viewRect = camera.ViewPixel;

				GL.Viewport(viewRect.x, viewRect.y, viewRect.width, viewRect.height);

				var cameraPos = cameraTransform.Position;

				for (int i = 0; i < ShadersByLightType.Length; i++) {
					var activeShader = ShadersByLightType[i];

					if (activeShader == null) {
						continue;
					}

					Shader.SetShader(activeShader);

					activeShader.SetupCommonUniforms();
					activeShader.SetupCameraUniforms(camera, cameraPos);

					var lightType = (Light.LightType)i;

					//TODO: Update & optimize this
					for (int j = 0; j < PassedTextures.Length; j++) {
						var tex = PassedTextures[j];

						GL.ActiveTexture((TextureUnit)((int)TextureUnit.Texture0 + j));
						GL.BindTexture(TextureTarget.Texture2D, tex.Id);

						if (activeShader.TryGetUniformLocation(tex.Name, out int location)) {
							GL.Uniform1(location, j);
						}
					}

					//TODO: Update & optimize this
					activeShader.TryGetUniformLocation("lightRange", out int uniformLightRange);
					activeShader.TryGetUniformLocation("lightPosition", out int uniformLightPosition);
					activeShader.TryGetUniformLocation("lightDirection", out int uniformLightDirection);
					activeShader.TryGetUniformLocation("lightIntensity", out int uniformLightIntensity);
					activeShader.TryGetUniformLocation("lightColor", out int uniformLightColor);

					foreach (var light in lightingData.Lights) {
						if (light.Type != lightType) {
							continue;
						}

						worldMatrix = light.Matrix;

						activeShader.SetupMatrixUniforms(
							in worldMatrix, ref inverseWorldMatrix,
							ref worldViewMatrix, ref inverseWorldViewMatrix,
							ref worldViewProjMatrix, ref inverseWorldViewProjMatrix,
							camera.ViewMatrix, camera.InverseViewMatrix,
							camera.ProjectionMatrix, camera.InverseProjectionMatrix
						);

						if (uniformLightRange != -1 && light.Range.HasValue) {
							GL.Uniform1(uniformLightRange, light.Range.Value);
						}

						if (uniformLightPosition != -1 && light.Position.HasValue) {
							var position = light.Position.Value;

							GL.Uniform3(uniformLightPosition, position.x, position.y, position.z);
						}

						if (uniformLightDirection != -1 && light.Direction.HasValue) {
							var direction = light.Direction.Value;

							GL.Uniform3(uniformLightDirection, direction.x, direction.y, direction.z);
						}

						if (uniformLightIntensity != -1) {
							GL.Uniform1(uniformLightIntensity, light.Intensity);
						}

						if (uniformLightColor != -1) {
							GL.Uniform3(uniformLightColor, light.Color.x, light.Color.y, light.Color.z);
						}

						switch (lightType) {
							case Light.LightType.Point:
								PrimitiveMeshes.IcoSphere.Render();
								break;
							case Light.LightType.Directional:
								PrimitiveMeshes.ScreenQuad.Render();
								break;
						}
					}
				}
			}

			GL.DepthMask(true);
			GL.Disable(EnableCap.Blend);
			GL.Disable(EnableCap.CullFace);

			Shader.SetShader(null);

			GL.BindTexture(TextureTarget.Texture2D, 0);
		}
	}
}
