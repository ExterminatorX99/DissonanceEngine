using Dissonance.Engine.Core.Modules;
using Dissonance.Framework.Windowing.Input;

namespace Dissonance.Engine
{
	public sealed partial class Input : EngineModule
	{
		public const int MaxMouseButtons = 12;
		public const int MaxGamepads = 4;

		internal static InputVariables fixedInput;
		internal static InputVariables renderInput;
		internal static InputVariables prevFixedInput;
		internal static InputVariables prevRenderInput;

		public static InputTrigger[] Triggers => triggers;
		//Mouse
		public static Vector2 MouseDelta => PrevInput.mousePosition-CurrentInput.mousePosition;
		public static Vector2 MousePosition => CurrentInput.mousePosition;
		public static int MouseWheel => CurrentInput.mouseWheel;
		//Keyboard
		public static string InputString => CurrentInput.inputString;

		internal static InputVariables CurrentInput => Game.IsFixedUpdate ? fixedInput : renderInput;
		internal static InputVariables PrevInput => Game.IsFixedUpdate ? prevFixedInput : prevRenderInput;

		protected override void Init()
		{
			fixedInput = new InputVariables();
			renderInput = new InputVariables();
			prevFixedInput = new InputVariables();
			prevRenderInput = new InputVariables();

			InitSignals();
			InitTriggers();
			InitCallbacks();

			SingletonInputTrigger.StaticInit();
		}
		protected override void PreFixedUpdate() => PreUpdate();
		protected override void PostFixedUpdate() => PostUpdate();
		protected override void PreRenderUpdate() => PreUpdate();
		protected override void PostRenderUpdate() => PostUpdate();

		private void PreUpdate()
		{
			CurrentInput.Update();

			UpdateTriggers();

			CheckSpecialCombinations();
		}
		private void PostUpdate()
		{
			CurrentInput.inputString = string.Empty;
			CurrentInput.mouseWheel = 0;

			CurrentInput.CopyTo(PrevInput);
		}

		private static void CheckSpecialCombinations()
		{
			if(GetKeyDown(Keys.F4) && (GetKey(Keys.LeftAlt) || GetKey(Keys.RightAlt))) {
				Game.Quit();
			}

			/*if(GetKeyDown(Keys.Enter) && (GetKey(Keys.LAlt) || GetKey(Keys.RAlt))) {
				Screen.Fullscreen = !Screen.Fullscreen;
			}*/
		}
	}
}