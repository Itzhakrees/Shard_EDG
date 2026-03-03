using SDL2;

namespace Shard
{
    class SpawnerComponent : Component, InputListener
    {
        public string SpawnTypeName = "GameTest.Asteroid";
        public string SpawnSpriteName = "asteroid.png";
        public int MouseButton = 1;
        public bool SpawnAtMouse = true;

        public override void initialize()
        {
            Bootstrap.getInput().addListener(this);
        }

        public void handleInput(InputEvent inp, string eventType)
        {
            if (!Bootstrap.IsPlayMode())
            {
                return;
            }

            if (eventType != "MouseDown" || inp.Button != MouseButton)
            {
                return;
            }

            GameObject spawned = GameObjectManager.getInstance().CreateObjectFromTypeName(SpawnTypeName);
            if (spawned == null)
            {
                return;
            }

            if (SpawnAtMouse)
            {
                spawned.Transform.X = inp.X;
                spawned.Transform.Y = inp.Y;
            }
            else if (Owner != null)
            {
                spawned.Transform.X = Owner.Transform.X;
                spawned.Transform.Y = Owner.Transform.Y;
            }

            if (string.IsNullOrEmpty(spawned.Transform.SpritePath) && !string.IsNullOrWhiteSpace(SpawnSpriteName))
            {
                string path = Bootstrap.getAssetManager().getAssetPath(SpawnSpriteName);
                if (!string.IsNullOrEmpty(path))
                {
                    spawned.Transform.SpritePath = path;
                }
            }
        }
    }
}
