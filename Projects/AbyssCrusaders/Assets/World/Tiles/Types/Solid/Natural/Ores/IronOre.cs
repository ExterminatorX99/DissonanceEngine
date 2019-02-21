namespace AbyssCrusaders.Tiles
{
	public class IronOre : SolidTileBase
	{
		protected override TileFrameset Frameset => TileFrameset.GetInstance<TerrariaFrameset>();

		public override void OnInit()
		{
			canBeWall = false;
		}
	}
}
