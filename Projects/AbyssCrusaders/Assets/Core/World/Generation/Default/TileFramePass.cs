﻿using GameEngine;

namespace AbyssCrusaders.Core.Generation.Default
{
	public class TileFramePass : GenPass
	{
		public override void Run(int seed,int index,World world)
		{
			for(int y = 0;y<world.height;y++) {
				for(int x = 0;x<world.width;x++) {
					world.TileFrame(x,y);
				}
			}
		}
	}
}