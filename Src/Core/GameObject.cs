using System;
using Dissonance.Engine.Core.ProgrammableEntities;
using Dissonance.Engine.Physics;

namespace Dissonance.Engine.Core
{
	public partial class GameObject : ProgrammableEntity, IDisposable
	{
		private static GameObjectManager Manager => Game.Instance.GetModule<GameObjectManager>();

		internal bool initialized;
		internal RigidbodyInternal rigidbodyInternal;

		private string name;
		private byte layer;

		public Transform Transform { get; }
		public string Name {
			get => name;
			set => name = value ?? throw new Exception("GameObject's name cannot be set to null");
		}
		public byte Layer {
			get => layer;
			set {
				if(value>=Layers.MaxLayers) {
					throw new IndexOutOfRangeException($"Layer values must be in [0..{Layers.MaxLayers-1}] range.");
				}

				layer = value;
			}
		}

		protected GameObject() : base()
		{
			Name = GetType().Name;
			Transform = new Transform(this);

			ComponentPreInit();
		}

		public virtual void OnInit() { }
		public virtual void OnDispose() { }

		public void Init()
		{
			if(initialized) {
				return;
			}

			Manager.gameObjects.Add(this);

			ProgrammableEntityManager.SubscribeEntity(this);

			OnInit();

			initialized = true;
		}
		public void Dispose()
		{
			ProgrammableEntityManager.UnsubscribeEntity(this);

			OnDispose();
			ComponentDispose();

			Manager.gameObjects.Remove(this);
		}

		public static T Instantiate<T>(Action<T> preinitializer = null) where T : GameObject
			=> Manager.Instantiate(preinitializer);
		public static GameObject Instantiate(Type type,Action<GameObject> preinitializer = null)
			=> Manager.Instantiate(type,preinitializer);
	}
}