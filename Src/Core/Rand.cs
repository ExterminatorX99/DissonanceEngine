using System;
using Dissonance.Engine.Core.Modules;

namespace Dissonance.Engine.Core
{
	public sealed class Rand : EngineModule
	{
		internal static Random staticRandom;

		private static int globalSeed;

		public static int GlobalSeed {
			get => globalSeed;
			set {
				globalSeed = value;
				staticRandom = new Random(globalSeed);
			}
		}

		internal Random random;

		private int seed;

		public int Seed {
			get => seed;
			set {
				seed = value;
				random = new Random(seed);
			}
		}

		/*public Rand(int seed)
		{
			this.seed = seed;

			random = new Random(seed);
		}*/

		protected override void Init()
		{
			globalSeed = (int)DateTime.Now.Ticks;
			staticRandom = new Random(globalSeed);
		}

		public static int Next(int maxValue) => staticRandom.Next(maxValue);
		public static float Next(float maxValue) => (float)staticRandom.NextDouble()*maxValue;
		public static int Range(int minValue,int maxValue) => staticRandom.Next(minValue,maxValue);
		public static float Range(float minValue,float maxValue)
		{
			if(minValue>maxValue) {
				float tempVal = maxValue;

				maxValue = minValue;
				minValue = tempVal;
			}

			return minValue+(float)staticRandom.NextDouble()*(maxValue-minValue);
		}
	}
}
