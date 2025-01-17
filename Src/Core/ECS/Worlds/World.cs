﻿using System;
using System.Linq.Expressions;

namespace Dissonance.Engine
{
	public sealed class World
	{
		internal readonly int Id;

		internal Entity WorldEntity;

		/// <summary> Whether or not this is the default engine-provided world. The default world cannot be removed. </summary>
		public bool IsDefault => Id == WorldManager.DefaultWorldId;

		internal World(int id)
		{
			Id = id;
		}

		// Entities

		public Entity CreateEntity()
			=> EntityManager.CreateEntity(Id);

		public EntitySet GetEntitySet(Expression<Predicate<Entity>> expression)
			=> EntityManager.GetEntitySet(Id, expression);

		public ReadOnlySpan<Entity> ReadEntities(bool? active = true)
			=> EntityManager.ReadEntities(Id, active);

		// Systems

		public void AddSystem(GameSystem system)
			=> SystemManager.AddSystemToWorld(this, system);

		// Components

		internal bool Has<T>() where T : struct
			=> WorldEntity.Has<T>();

		internal ref T Get<T>() where T : struct
			=> ref WorldEntity.Get<T>();

		internal void Set<T>(T value) where T : struct
			=> WorldEntity.Set(value);

		// Messages

		public ReadOnlySpan<T> ReadMessages<T>() where T : struct
			=> MessageManager.ReadMessages<T>(Id);

		public void SendMessage<T>(in T message) where T : struct
			=> MessageManager.SendMessage(Id, message);

		// Etc

		internal void Init()
		{
			WorldEntity = CreateEntity();
		}
	}
}
