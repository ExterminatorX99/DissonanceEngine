﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Dissonance.Engine.Core.GameLogic
{
	[Flags]
	public enum GameFlags
	{
		None,
		NoGraphics = 1,
		NoAudio = 2
	}
}
