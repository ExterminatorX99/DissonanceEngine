using System;
using System.Collections.Generic;
using BulletSharp;

namespace GameEngine
{
	public struct RaycastHit
	{
		public Vector3 point;
		public Vector3 normal;
		public int triangleIndex;
	}
	public static class Physics
	{
		#region StaticFields
		internal static DiscreteDynamicsWorld world;
		internal static CollisionDispatcher dispatcher;
		internal static DbvtBroadphase broadphase;
		internal static List<CollisionShape> collisionShapes;
		internal static CollisionConfiguration collisionConf;
		internal static List<RigidbodyInternal> rigidbodies;
		internal static List<Collider> collidersToUpdate;
		internal static bool enabled;
		#endregion
		#region StaticProperties
		public static List<RigidbodyBase> ActiveRigidbodies	{ get; private set; }
		public static Vector3 Gravity {
			get => world.Gravity;
			set => world.Gravity = value;
		}
		#endregion
		
		public static void Init()
		{
			collisionShapes = new List<CollisionShape>();
			collisionConf = new DefaultCollisionConfiguration();//collision configuration contains default setup for memory,collision setup
			rigidbodies = new List<RigidbodyInternal>();
			ActiveRigidbodies = new List<RigidbodyBase>();
			collidersToUpdate = new List<Collider>();
			dispatcher = new CollisionDispatcher(collisionConf);
			
			broadphase = new DbvtBroadphase();
			world = new DiscreteDynamicsWorld(dispatcher,broadphase,null,collisionConf);
			world.SetInternalTickCallback(InternalTickCallback);
			Gravity = new Vector3(0f,-9.81f,0f);

			ManifoldPoint.ContactAdded += Callback_ContactAdded;
			PersistentManifold.ContactProcessed += Callback_ContactProcessed;
			PersistentManifold.ContactDestroyed += Callback_ContactDestroyed;
		}
		public static void Update()
		{
			
		}
		public static void UpdateFixed()
		{
			enabled = true;
			for(int i=0;i<rigidbodies.Count;i++) {
				var rigidbody = rigidbodies[i];
				if(!rigidbody.enabled) {
					continue;
				}
				if(rigidbody.gameObject.transform.updatePhysics/*&& !rigidbody.isKinematic*/) {
					rigidbody.btRigidbody.WorldTransform = rigidbody.gameObject.transform.WorldMatrix;
					//Debug.Log(rigidbody.gameObject.name+"-Tried to reposition the internal rigidbody");
				}
				rigidbody.gameObject.transform.updatePhysics = false;
			}
			//fixedStep = true;
			world.StepSimulation(Time.fixedDeltaTime);
		}
		public static void UpdateRender()
		{
			//world.StepSimulation(Time.renderDeltaTime,1,0f);
			//fixedStep = false;
		}
		public static bool Raycast(Vector3 origin,Vector3 direction,out RaycastHit hit,float range = 100000f,Func<ulong,ulong> mask = null,Func<GameObject,bool?> customFilter = null,bool debug = false)
		{
			direction.Normalize();

			ulong layerMask = ulong.MaxValue;
			if(mask!=null) {
				layerMask = mask(layerMask);
			}

			BulletSharp.Vector3 rayEnd = origin+direction*range;
			BulletSharp.Vector3 origin2 = origin;
			var callback = new RaycastCallback(ref origin2,ref rayEnd,layerMask,customFilter);
			//LocalRayResult result = new LocalRayResult(callback.CollisionObject,
			//callback.AddSingleResult(
			world.RayTest(origin,rayEnd,callback);
			//Debug.Log(callback.triangleIndex);

			hit = new RaycastHit {
				triangleIndex = -1
			};
			if(!callback.HasHit) {
				return false;
			}
			hit.point = callback.HitPointWorld;
			hit.triangleIndex = callback.triangleIndex;
			return true;
		}
		public static void Dispose()
		{
			world?.Dispose();
			dispatcher?.Dispose();
			broadphase?.Dispose();
			collidersToUpdate?.Clear();
			if(collisionShapes!=null) {
				for(int i=0;i<collisionShapes.Count;i++) {
					collisionShapes[i].Dispose();
				}
			}
			if(rigidbodies!=null) {
				for(int i=0;i<rigidbodies.Count;i++) {
					rigidbodies[i].Dispose();
				}
				rigidbodies.Clear();
			}
		}
		internal static CollisionShape GetSubShape(CollisionShape shape,int subIndex)
		{
			if(!(shape is CompoundShape) || shape==null || subIndex<0) {
				return shape;
			}
			return ((CompoundShape)shape).GetChildShape(subIndex);
		}

		#region Callbacks
		private static void Callback_ContactAdded(ManifoldPoint cp,CollisionObjectWrapper colObj0,int partId0,int index0,CollisionObjectWrapper colObj1,int partId1,int index1)
		{
			//Bullet seems to use edge normals by default. Code below corrects it so it uses face normals instead.
			//This fixes tons of issues with rigidbodies jumping up when moving between terrain quads,even if terrain is 100% flat.
			var shape0 = colObj0.CollisionShape;
			var shape1 = colObj1.CollisionShape;
			var obj = shape0.ShapeType==BroadphaseNativeType.TriangleShape ? colObj0 : shape1.ShapeType==BroadphaseNativeType.TriangleShape ? colObj1 : null;
			if(obj!=null) {
				Matrix4x4 transform = obj.WorldTransform;
				transform.ClearTranslation();
				var shape = (TriangleShape)obj.CollisionShape;
				cp.NormalWorldOnB = (transform*Vector3.Cross(shape.Vertices[1]-shape.Vertices[0],shape.Vertices[2]-shape.Vertices[0])).Normalized;
			}
			//Debug.Log("Added Contact between "+Rand.Range(0,100));

			//cp.UserPersistentData = colObj1Wrap.CollisionObject.UserObject;
		}
		private static void Callback_ContactProcessed(ManifoldPoint cp,CollisionObject body0,CollisionObject body1)
		{
			var rigidbodyA = (RigidbodyInternal)body0.UserObject;
			var rigidbodyB = (RigidbodyInternal)body1.UserObject;
			rigidbodyA.AddCollision(rigidbodyB);
			rigidbodyB.AddCollision(rigidbodyA);
			
			//Debug.Log("Processed Contact. "+Rand.Range(0,100));
			//cp.UserPersistentData = body0.UserObject;
		}
		private static void Callback_ContactDestroyed(object userPersistantData)
		{
			Debug.Log("Contact destroyed. "+Rand.Range(0,100));
		}
		internal static void InternalTickCallback(DynamicsWorld world,float timeStep)
		{
			var dispatcher = world.Dispatcher;
			int numManifolds = dispatcher.NumManifolds;
			
			for(int i=0;i<rigidbodies.Count;i++) {
				rigidbodies[i].collisions.Clear();
			}
			for(int i=0;i<numManifolds;i++) {
				var contactManifold = dispatcher.GetManifoldByIndexInternal(i);
				int numContacts = contactManifold.NumContacts;
				if(numContacts==0) {
					continue;
				}
				var objA = contactManifold.Body0;
				var objB = contactManifold.Body1;
				if(!(objA.UserObject is RigidbodyInternal rigidBodyA) || !(objB.UserObject is RigidbodyInternal rigidbodyB)) {
					throw new Exception("UserObject wasn't a '"+typeof(RigidbodyInternal).FullName+"'.");
				}
				for(int j=0;j<2;j++) {
					bool doingA = j==0;
					var thisRB = doingA ? rigidBodyA : rigidbodyB;
					var otherRB = doingA ? rigidbodyB : rigidBodyA;
					if(thisRB.rigidbody is Rigidbody) {
						var contacts = new ContactPoint[numContacts];
						for(int k=0;k<numContacts;k++) {
							var cPoint = contactManifold.GetContactPoint(k);
							contacts[k] = new ContactPoint {
								point = doingA ? cPoint.PositionWorldOnB : cPoint.PositionWorldOnA,	//Should ContactPoint have two pairs of vectors?
								normal = cPoint.NormalWorldOnB,
								separation = cPoint.Distance,
							};
						}
						var collision = new Collision(otherRB.gameObject,otherRB.rigidbody as Rigidbody,null,contacts);
						thisRB.collisions.Add(collision);
					}else{
						var contacts = new ContactPoint2D[numContacts];
						for(int k=0;k<numContacts;k++) {
							var cPoint = contactManifold.GetContactPoint(k);
							contacts[k] = new ContactPoint2D {
								point = doingA ? ((Vector3)cPoint.PositionWorldOnB).XY : ((Vector3)cPoint.PositionWorldOnA).XY,	//Should ContactPoint have two pairs of vectors?
								normal = ((Vector3)cPoint.NormalWorldOnB).XY,
								separation = cPoint.Distance,
							};
						}
						var collision = new Collision2D(otherRB.gameObject,otherRB.rigidbody as Rigidbody2D,null,contacts);
						thisRB.collisions2D.Add(collision);
					}
				}
			}

			/*int numManifolds = world->getDispatcher()->getNumManifolds();
			for (int i = 0; i < numManifolds; i++)
			{
				btPersistentManifold*contactManifold = world->getDispatcher()->getManifoldByIndexInternal(i);
				const btCollisionObject*obA = contactManifold->getBody0();
				const btCollisionObject*obB = contactManifold->getBody1();

				int numContacts = contactManifold->getNumContacts();
				for (int j = 0; j < numContacts; j++)
				{
					btManifoldPoint & pt = contactManifold->getContactPoint(j);
					if (pt.getDistance() < 0.f)
					{
						const btVector3 & ptA = pt.getPositionWorldOnA();
						const btVector3 & ptB = pt.getPositionWorldOnB();
						const btVector3 & normalOnB = pt.m_normalWorldOnB;
					}
				}
			}*/
			/*var dispatcher = world.Dispatcher;
			int numManifolds = dispatcher.NumManifolds;
			for(int i=0;i<numManifolds;i++) {
				var contactManifold = dispatcher.GetManifoldByIndexInternal(i);
				var objA = contactManifold.Body0;
				var objB = contactManifold.Body1;

				int numContacts = contactManifold.NumContacts;
			}*/

			/*int numManifolds = world->getDispatcher()->getNumManifolds();
			for (int i = 0; i < numManifolds; i++)
			{
				btPersistentManifold*contactManifold = world->getDispatcher()->getManifoldByIndexInternal(i);
				const btCollisionObject*obA = contactManifold->getBody0();
				const btCollisionObject*obB = contactManifold->getBody1();

				int numContacts = contactManifold->getNumContacts();
				for (int j = 0; j < numContacts; j++)
				{
					btManifoldPoint & pt = contactManifold->getContactPoint(j);
					if (pt.getDistance() < 0.f)
					{
						const btVector3 & ptA = pt.getPositionWorldOnA();
						const btVector3 & ptB = pt.getPositionWorldOnB();
						const btVector3 & normalOnB = pt.m_normalWorldOnB;
					}
				}
			}*/
		}
		#endregion
	}
	internal class RaycastCallback : ClosestRayResultCallback
	{
		public int triangleIndex = -1;
		public ulong layerMask;
		public Func<GameObject,bool?> customFilter;

		public RaycastCallback(ref BulletSharp.Vector3 rayFromWorld,ref BulletSharp.Vector3 rayToWorld,ulong layerMask,Func<GameObject,bool?> customFilter) : base(ref rayFromWorld,ref rayToWorld)
		{
			this.layerMask = layerMask;
			this.customFilter = customFilter;
		}
		public override float AddSingleResult(LocalRayResult rayResult,bool normalInWorldSpace)
		{
			try {
				var rb = rayResult.CollisionObject;
				var shapeInfo = rayResult.LocalShapeInfo;
				if(rb!=null && shapeInfo!=null) {
					//Debug.Log(shapeInfo.ShapePart);
					var collShape = Physics.GetSubShape(rb.CollisionShape,shapeInfo.ShapePart);
					if(collShape!=null) {
						//Debug.Log(collShape.ShapeType);
						triangleIndex = shapeInfo.TriangleIndex;
					}
				}
			}
			catch {}
			return base.AddSingleResult(rayResult,normalInWorldSpace);
		}
		public override bool NeedsCollision(BroadphaseProxy proxy)
		{
			if(proxy.ClientObject is RigidBody bulletBody) {
				var rbInternal = bulletBody.UserObject as RigidbodyInternal;
				ulong objLayerMask = Layers.GetLayerMask(rbInternal.gameObject.layer);
				if(rbInternal!=null) {
					var resultOverride = customFilter?.Invoke(rbInternal.gameObject);
					if(resultOverride!=null) {
						return resultOverride.Value;
					}
					if((objLayerMask & layerMask)==0) {
						return false;
					}
				}
			}
			return base.NeedsCollision(proxy);
		}
	}
}

