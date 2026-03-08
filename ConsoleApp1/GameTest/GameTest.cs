/*
*   Contributions to the code made by others:
*   @author Lisa te Braak (see Changelog for 1.3.0) 
*/

using GameTest;
using System;
using System.Collections.Generic;
using System.IO;

namespace Shard
{
    class GameTest : Game
    {
        GameObject background;

        public override void update()
        {
            Bootstrap.getDisplay().showText("FPS: " + Bootstrap.getSecondFPS() + " / " + Bootstrap.getFPS(), 10, 10, 12, 255, 255, 255);
            if (IsBreakoutScene())
            {
                Bootstrap.getDisplay().showText("A/D or Left/Right: Move Paddle", 10, 26, 12, 220, 220, 220);

                int bricks = CountBreakoutBricks();
                Bootstrap.getDisplay().showText("Bricks: " + bricks, 10, 42, 12, 255, 220, 120);

                if (Bootstrap.IsPlayMode() && bricks == 0)
                {
                    Bootstrap.getDisplay().showText("YOU WIN", 780, 540, 48, 255, 230, 120);
                }
            }

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

        private int CountBreakoutBricks()
        {
            int count = 0;
            foreach (GameObject gob in GameObjectManager.getInstance().GetGameObjects())
            {
                if (gob != null && !gob.ToBeDestroyed && gob.checkTag("BreakoutBrick"))
                {
                    count += 1;
                }
            }

            return count;
        }

        private bool IsBreakoutScene()
        {
            bool hasBall = false;
            bool hasPaddle = false;

            foreach (GameObject gob in GameObjectManager.getInstance().GetGameObjects())
            {
                if (gob == null)
                {
                    continue;
                }

                if (gob.checkTag("BreakoutBall"))
                {
                    hasBall = true;
                }

                if (gob.checkTag("BreakoutPaddle"))
                {
                    hasPaddle = true;
                }

                if (hasBall && hasPaddle)
                {
                    return true;
                }
            }

            return false;
        }

        private void CreateDefaultScene()
        {
            GameObjectManager.getInstance().ClearScene();

            background = new GameObject();
            background.Transform.SpritePath = getAssetManager().getAssetPath("background2.jpg");
            background.Transform.X = 0;
            background.Transform.Y = 0;

            BreakoutPaddle paddle = new BreakoutPaddle();
            paddle.Transform.X = 850f;
            paddle.Transform.Y = 980f;

            BreakoutBall ball = new BreakoutBall();
            ball.Transform.X = paddle.Transform.X + (paddle.Width * 0.5f);
            ball.Transform.Y = paddle.Transform.Y - 16f;

            BreakoutBoundary wallTop = new BreakoutBoundary();
            wallTop.Transform.X = 0f;
            wallTop.Transform.Y = 0f;
            wallTop.Width = 1920f;
            wallTop.Height = 12f;

            BreakoutBoundary wallLeft = new BreakoutBoundary();
            wallLeft.Transform.X = 0f;
            wallLeft.Transform.Y = 0f;
            wallLeft.Width = 12f;
            wallLeft.Height = 1080f;

            BreakoutBoundary wallRight = new BreakoutBoundary();
            wallRight.Transform.X = 1908f;
            wallRight.Transform.Y = 0f;
            wallRight.Width = 12f;
            wallRight.Height = 1080f;

            int cols = 8;
            int rows = 4;
            float startX = 340f;
            float startY = 140f;
            float gapX = 70f;
            float gapY = 22f;

            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < cols; x++)
                {
                    BreakoutBrick brick = new BreakoutBrick();
                    brick.Transform.X = startX + (x * (brick.Width + gapX));
                    brick.Transform.Y = startY + (y * (brick.Height + gapY));

                    brick.ColorR = 180 + (y * 10);
                    brick.ColorG = 80 + (x * 8 % 140);
                    brick.ColorB = 60 + (y * 25);
                }
            }
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
