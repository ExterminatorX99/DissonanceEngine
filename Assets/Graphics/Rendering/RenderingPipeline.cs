﻿using System;
using System.Collections.Generic;

namespace GameEngine.Graphics
{
	public abstract class RenderingPipeline : IDisposable
	{
		internal Framebuffer[] framebuffers;
		public Framebuffer[] Framebuffers {
			get => framebuffers;
			set => framebuffers = value ?? new Framebuffer[0];
		}
		internal RenderPass[] renderPasses;
		public RenderPass[] RenderPasses {
			get => renderPasses;
			set => renderPasses = value ?? new RenderPass[0];
		}

		internal void Init()
		{
			var framebuffersList = new List<Framebuffer>();
			var renderPassesList = new List<RenderPass>();

			Setup(framebuffersList,renderPassesList);

			Rendering.CheckGLErrors();

			if(framebuffersList!=null) {
				foreach(var framebuffer in framebuffersList) {
					Framebuffer.Bind(framebuffer);
					Rendering.CheckFramebufferStatus();
				}
			}

			Framebuffers = framebuffersList.ToArray();
			RenderPasses = renderPassesList.ToArray();

			/*if(renderPasses==null || renderPasses.Length==0) {
				throw new Exception($"Cannot initialize rendering pipeline {GetType().Name}: Pipeline must have 1 or more rendering passes.");
			}*/
		}

		public abstract void Setup(List<Framebuffer> framebuffers,List<RenderPass> renderPasses); 
		
		public virtual void PreRender() {}
		public virtual void PostRender() {}
		public virtual void Dispose()
		{
			if(framebuffers!=null) {
				for(int i = 0;i<framebuffers.Length;i++) {
					framebuffers[i].Dispose();
				}
			}

			for(int i = 0;i<renderPasses.Length;i++) {
				renderPasses[i].Dispose();
			}
		}
	}
}