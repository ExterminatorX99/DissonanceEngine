﻿using System;
using Dissonance.Framework.Audio;

namespace Dissonance.Engine.Audio
{
	[Reads<AudioListener>]
	[Reads<Transform>]
	public sealed class AudioListenerSystem : GameSystem
	{
		private static readonly float[] OrientationArray = new float[6];

		private EntitySet entities;

		protected internal override void Initialize()
		{
			entities = World.GetEntitySet(e => e.Has<AudioListener>());
		}

		protected internal override void RenderUpdate()
		{
			var entitySpan = entities.ReadEntities();

			if (entitySpan.Length == 0) {
				return;
			}

			if (entitySpan.Length > 1) {
				throw new InvalidOperationException($"Only a single {nameof(AudioListener)} is allowed to exist in a world.");
			}

			var entity = entitySpan[0];

			Vector3 position;

			if (entity.Has<Transform>()) {
				var transform = entity.Get<Transform>();
				var up = transform.Up;
				var lookAt = -transform.Forward;

				position = transform.Position;

				OrientationArray[0] = lookAt.X;
				OrientationArray[1] = lookAt.Y;
				OrientationArray[2] = lookAt.Z;
				OrientationArray[3] = up.X;
				OrientationArray[4] = up.Y;
				OrientationArray[5] = up.Z;
			} else {
				position = Vector3.Zero;

				for (int i = 0; i < OrientationArray.Length; i++) {
					OrientationArray[i] = 0f;
				}
			}

			AL.Listener3(ListenerFloat3.Position, position.X, position.Y, position.Z);
			AL.Listener(ListenerFloatArray.Orientation, OrientationArray);

			AudioEngine.CheckALErrors();
		}
	}
}
