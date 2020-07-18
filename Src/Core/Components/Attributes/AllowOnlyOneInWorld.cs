﻿using System;

namespace Dissonance.Engine.Core.Components.Attributes
{
	public class AllowOnlyOneInWorld : ComponentAttribute
	{
		public override void SetParameters(Type type) => ComponentManager.typeParameters[type].allowOnlyOneInWorld = true;
	}
}