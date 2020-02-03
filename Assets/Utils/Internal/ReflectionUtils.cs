﻿using System;
using System.Collections.Generic;

namespace GameEngine.Utils.Internal
{
	internal static class ReflectionUtils
	{
		public static IEnumerable<Type> EnumerateBaseTypes(Type type)
		{
			type = type.BaseType;

			while(type!=null) {
				yield return type;

				type = type.BaseType;
			}
		}
	}
}
