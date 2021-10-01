﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dissonance.Engine.Utilities;

namespace Dissonance.Engine.Graphics
{
	public sealed class AutomaticUniformModule : EngineModule
	{
		private struct UniformEntry
		{
			public delegate void CalculateAndCacheDelegate(Shader shader, in Transform transform, in RenderViewData.RenderView viewData);
			public delegate void ApplyFromCacheDelegate(int location);

			public AutomaticUniform Instance;
			public CalculateAndCacheDelegate CalculateAndCache;
			public ApplyFromCacheDelegate ApplyFromCache;

			public UniformEntry(AutomaticUniform instance) : this()
			{
				Instance = instance;

				var type = instance.GetType();

				CalculateAndCache = type
					.GetMethod(
						nameof(AutomaticUniform<float>.CalculateAndCache),
						BindingFlags.Instance | BindingFlags.NonPublic,
						new[] { typeof(Shader), typeof(Transform).MakeByRefType(), typeof(RenderViewData.RenderView).MakeByRefType() }
					)
					.MakeGenericMethod(type)
					.CreateDelegate<CalculateAndCacheDelegate>(Instance);

				ApplyFromCache = type
					.GetMethod(
						nameof(AutomaticUniform<float>.ApplyFromCache),
						BindingFlags.Instance | BindingFlags.NonPublic,
						new[] { typeof(int) }
					)
					.MakeGenericMethod(type)
					.CreateDelegate<ApplyFromCacheDelegate>(Instance);
			}
		}

		private static readonly List<UniformEntry> instancesEntries = new();
		private static readonly Dictionary<Type, int> entryIndicesByType = new();

		protected override void PreInit()
		{
			AssemblyRegistrationModule.OnAssemblyRegistered += (assembly, types) => {
				foreach (var type in types) {
					if (type.IsAbstract || !typeof(AutomaticUniform).IsAssignableFrom(type)) {
						continue;
					}

					entryIndicesByType[type] = instancesEntries.Count;

					instancesEntries.Add(new UniformEntry((AutomaticUniform)Activator.CreateInstance(type)));
				}

				foreach (var entry in instancesEntries) {
					var dependencyIndices = entry.Instance
						.GetType()
						.GetCustomAttributes<AutomaticUniformDependencyAttribute>(true)
						.Select(a => entryIndicesByType.TryGetValue(a.Type, out int index) ? index : throw new ArgumentException($"Invalid uniform dependency: '{a.Type.Name}'."))
						.ToList();

					entry.Instance.dependencyIndices.AddRange(dependencyIndices);
				}
			};
		}

		/// <summary> Returns an ordered array of indices of default uniforms that should be calculated, together with (nullable) locations on the shader. </summary>
		internal static AutomaticUniformUsageInfo[] GetPresentDefaultUniformInfo(Shader shader)
		{
			int[] locationsByDefaultUniformIndex = new int[instancesEntries.Count];
			var indicesOfUniformsToCalculate = new HashSet<int>();

			for (int i = 0; i < instancesEntries.Count; i++) {
				var instance = instancesEntries[i].Instance;

				if (shader.TryGetUniformLocation(instance.UniformName, out int location)) {
					locationsByDefaultUniformIndex[i] = location;

					indicesOfUniformsToCalculate.Add(i);

					foreach (int dependency in instance.DependencyIndices) {
						indicesOfUniformsToCalculate.Add(dependency);
					}
				} else {
					locationsByDefaultUniformIndex[i] = -1;
				}
			}

			return DependencyUtils.DependencySort(indicesOfUniformsToCalculate, uniformId => instancesEntries[uniformId].Instance.DependencyIndices)
				.Select(uniformId => {
					int location = locationsByDefaultUniformIndex[uniformId];

					return new AutomaticUniformUsageInfo(uniformId, location >= 0 ? location : null);
				})
				.ToArray();
		}

		internal static void Apply(Shader shader, in Transform transform, in RenderViewData.RenderView viewData)
		{
			foreach (var usageInfo in shader.defaultUniforms) {
				var uniformEntry = instancesEntries[usageInfo.Index];

				uniformEntry.CalculateAndCache(shader, in transform, in viewData);

				if (usageInfo.Location.HasValue) {
					uniformEntry.ApplyFromCache(usageInfo.Location.Value);
				}
			}
		}
	}
}