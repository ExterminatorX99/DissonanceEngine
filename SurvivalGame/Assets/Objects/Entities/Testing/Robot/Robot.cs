using System;
using System.Collections.Generic;
using GameEngine;

namespace SurvivalGame
{
	public class Robot : Entity
	{
		public SkinnedMeshRenderer renderer;

		public override void OnInit()
		{
			renderer = AddComponent<SkinnedMeshRenderer>();
			renderer.Mesh = Resources.Import<Mesh>("Robot.mesh");//Mesh.Import("Assets/Robot.mesh",0.0133f);
			renderer.Material = Resources.Find<Material>("DualSided");
			//Resources.Export(renderer.Mesh,"NewRobot.mesh");
			//transform.position = new Vector3(0f,0f,-40f);

			var posMarker = Instantiate<Entity>(world,"PosMarker");
			posMarker.Transform.parent = Transform;
			posMarker.Transform.LocalPosition = Vector3.zero;
			var tempRenderer = posMarker.AddComponent<MeshRenderer>();
			tempRenderer.Mesh = PrimitiveMeshes.Cube;
			renderer.Material = Resources.Find<Material>("Robot");//Material.Find("Robot");
		}
		public override void FixedUpdate()
		{
			if(Input.GetKey(Keys.Up)) {
				Transform.Position += Vector3.up*Time.FixedDeltaTime*4f;
			}
			if(Input.GetKey(Keys.Down)) {
				Transform.Position += Vector3.down*Time.FixedDeltaTime*4f;
			}
			if(Input.GetKey(Keys.Right)) {
				//renderer.skeleton.bonesByName["RightUpperArm"].transform.eulerRot += new Vector3(0f,0f,Time.fixedDeltaTime*30f);
			}
		}
	}
}