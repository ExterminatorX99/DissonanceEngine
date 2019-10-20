namespace AbyssCrusaders.Tiles
{
	public class GoldOre : SolidTileBase
	{
		protected override TileFrameset Frameset => TileFrameset.GetInstance<TerrariaFrameset>();

		public override void OnInit()
		{
			base.OnInit();

			canBeWall = false;
		}
	}
}
