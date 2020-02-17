using BulletSharp;

namespace Dissonance.Engine.Physics
{
	public abstract class Collider : PhysicsComponent
	{
		internal CollisionShape collShape;

		protected Vector3 offset = Vector3.Zero;

		private bool needsUpdate;

		public Vector3 Offset {
			get => offset;
			set {
				if(offset!=value) {
					offset = value;

					TryUpdateCollider();
				}
			}
		}

		internal virtual void UpdateCollider()
		{
			if(collShape!=null) {
				collShape.UserObject = this;
			}

			gameObject.rigidbodyInternal.UpdateShape();

			needsUpdate = false;
		}

		protected override void OnInit()
		{
			base.OnInit();

			needsUpdate = true;
		}
		protected override void OnEnable()
		{
			base.OnEnable();

			if(needsUpdate) {
				UpdateCollider();
			}
		}

		protected void TryUpdateCollider()
		{
			if(enabled) {
				UpdateCollider();
			} else {
				needsUpdate = true;
			}
		}
	}
}
