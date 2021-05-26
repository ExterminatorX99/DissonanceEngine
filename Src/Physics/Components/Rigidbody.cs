namespace Dissonance.Engine.Physics
{
	public struct Rigidbody : IComponent
	{
		public Vector3 Velocity { get; set; }
		public Vector3 AngularVelocity { get; set; }
		public Vector3 AngularFactor { get; set; }
	}
}
