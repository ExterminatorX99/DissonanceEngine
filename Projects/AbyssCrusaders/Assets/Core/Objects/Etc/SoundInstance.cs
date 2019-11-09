using GameEngine;

namespace AbyssCrusaders.Core
{
	public class SoundInstance : GameObject2D
	{
		public AudioSource source;

		public override void FixedUpdate()
		{
			if(source==null || !source.IsPlaying) {
				Dispose();
			}
		}

		public static SoundInstance Create(string sound,Vector2 position,float volume = 1f,float? pitch = null,Transform attachTo = null,bool is2D = false)
		{
			var instance = Instantiate<SoundInstance>("SoundInstance_"+sound);

			instance.Position = position;

			if(attachTo!=null) {
				instance.Transform.parent = attachTo;
			}

			(instance.source = instance.AddComponent<AudioSource>(c => {
				c.Clip = Resources.Get<AudioClip>(sound);
				c.Volume = volume;
				c.Pitch = pitch ?? Rand.Range(0.9f,1.1f);

				if(is2D) {
					c.Is2D = true;
				}
			})).Play();

			return instance;
		}
	}
}