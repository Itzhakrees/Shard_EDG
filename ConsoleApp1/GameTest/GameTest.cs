/*
*   Contributions to the code made by others:
*   @author Lisa te Braak (see Changelog for 1.3.0) 
*/

using GameTest;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace Shard
{
    class GameTest : Game
    {
        GameObject background;
        public override void update()
        {
            
            Bootstrap.getDisplay().showText("FPS: " + Bootstrap.getSecondFPS() + " / " + Bootstrap.getFPS(), 10, 10, 12, 255, 255, 255);
            EnsureBackgroundReference();

            if (background != null && background.Transform != null && !string.IsNullOrEmpty(background.Transform.SpritePath))
            {
                Bootstrap.getDisplay().addToDraw(background);
            }

        }

        public override int getTargetFrameRate()
        {
            return 100;

        }
        private void CreateDefaultScene()
        {
            GameObjectManager.getInstance().ClearScene();

            background = new GameObject();
            background.Transform.SpritePath = getAssetManager().getAssetPath ("background2.jpg");
            background.Transform.X = 0;
            background.Transform.Y = 0;

            GameObject ship = new Spaceship();
            ship.Transform.X = 500f;
            ship.Transform.Y = 500f;

            GameObject spawner = new GameObject();
            spawner.addComponent(new SpawnerComponent());
        }

        public override void initialize()
        {
            string scenePath = Bootstrap.getScenePath();
            Bootstrap.setScenePath(scenePath);
            bool loaded = GameObjectManager.getInstance().LoadSceneFromFile(scenePath);

            if (!loaded)
            {
                CreateDefaultScene();
                GameObjectManager.getInstance().SaveSceneToFile(scenePath);
            }

            foreach (GameObject gob in GameObjectManager.getInstance().GetGameObjects())
            {
                if (gob.Transform != null && gob.Transform.SpritePath != null && gob.Transform.SpritePath.EndsWith("background2.jpg"))
                {
                    background = gob;
                    break;
                }
            }
        }

        public override void editorUpdate()
        {
            EnsureBackgroundReference();
        }

        private void EnsureBackgroundReference()
        {
            if (background != null && background.Transform != null && !string.IsNullOrEmpty(background.Transform.SpritePath))
            {
                return;
            }

            background = null;

            foreach (GameObject gob in GameObjectManager.getInstance().GetGameObjects())
            {
                if (gob == null || gob.Transform == null || string.IsNullOrEmpty(gob.Transform.SpritePath))
                {
                    continue;
                }

                string name = Path.GetFileName(gob.Transform.SpritePath);
                if (string.Equals(name, "background2.jpg", StringComparison.OrdinalIgnoreCase))
                {
                    background = gob;
                    break;
                }
            }
        }

    }
}
