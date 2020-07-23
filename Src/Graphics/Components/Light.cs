using Dissonance.Engine.Core.Components;
using Dissonance.Engine.Structures;

namespace Dissonance.Engine.Graphics.Components
{
	public class Light : Component
	{
		public enum Type
		{
			Point,
			Directional,
			Spot
		}

		public float range = 16f;
		public float intensity = 1f;
		public Vector3 color = Vector3.One;
		public Type type = Type.Point;
	}
}