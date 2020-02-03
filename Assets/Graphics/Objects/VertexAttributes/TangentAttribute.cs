﻿using System;
using Dissonance.Framework.OpenGL;

namespace GameEngine.Graphics
{
	public class TangentAttribute : CustomVertexAttribute<TangentBuffer>
	{
		public override void Init(out string nameId,out VertexAttribPointerType pointerType,out bool isNormalized,out int size,out int offset)
		{
			nameId = "tangent";
			pointerType = VertexAttribPointerType.Float;
			isNormalized = false;
			size = 4;
			offset = 0;
		}
	}
}
