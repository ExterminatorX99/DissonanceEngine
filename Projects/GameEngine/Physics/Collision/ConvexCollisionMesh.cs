﻿using BulletSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameEngine.Physics
{
	//Convex shapes can be used for anything
	public class ConvexCollisionMesh : CollisionMesh
	{
		public override void SetupFromMesh(Mesh mesh)
		{
			var triMesh = new TriangleMesh();

			int i = 0;
			while(i<mesh.triangles.Length) {
				triMesh.AddTriangle(
					mesh.vertices[mesh.triangles[i++]],
					mesh.vertices[mesh.triangles[i++]],
					mesh.vertices[mesh.triangles[i++]]
				);
			}

			var tempShape = new ConvexTriangleMeshShape(triMesh);
			using var tempHull = new ShapeHull(tempShape);

			tempHull.BuildHull(tempShape.Margin);

			collShape = new ConvexHullShape(tempHull.Vertices);
		}

		public static explicit operator ConvexCollisionMesh(Mesh mesh)
		{
			var collisionMesh = new ConvexCollisionMesh();
			collisionMesh.SetupFromMesh(mesh);
			return collisionMesh;
		}
	}
}
