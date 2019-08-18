using GameEngine;
using GameEngine.Graphics;
using GameEngine.Extensions.Chains;
using GameEngine.Physics;

namespace AbyssCrusaders
{
	public class CubeObj : GameObject
	{
		public override void OnInit()
		{
			layer = Layers.GetLayerIndex("Entity");

			AddComponent<MeshRenderer>(c => {
				c.Mesh = PrimitiveMeshes.Cube;
				c.Material = new Material("CubeMaterial",Resources.Find<Shader>("Diffuse"));
			});

			AddComponent<Box2DCollider>();

			AddComponent<Rigidbody2D>(c => c.IsKinematic = true);

			Transform.LocalScale = new Vector3(10f,1f,1f);
		}
	}
}