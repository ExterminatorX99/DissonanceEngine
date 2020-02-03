﻿namespace GameEngine
{
	public abstract class ProgrammableEntity //TODO: Come up with a better name?
	{
		public virtual void FixedUpdate() {}
		public virtual void RenderUpdate() {}
		public virtual void OnGUI() {}
	}
}
