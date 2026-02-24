using GameBreakout;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Shard
{
    class GameBreakout : Game
    {
        GameObject top, left, right, bottom;
        Random rand;
        List<Brick> myBricks;

        public override void update()
        {
            Bootstrap.getDisplay().showText("FPS: " + Bootstrap.getFPS(), 10, 10, 12, 255, 255, 255);
            Bootstrap.getDisplay().showText("Delta: " + Bootstrap.getDeltaTime(), 10, 20, 12, 255, 255, 255);

            foreach (Brick b in myBricks)
            {
                if (b != null && b.ToBeDestroyed == false)
                {
                    return;
                }
            }

            createBricks();
        }

        public void createBricks()
        {
            myBricks.Clear();

            for (int i = 0; i < 17; i++)
            {
                for (int j = 0; j < 10; j++)
                {
                    if (i % 2 == 0 || j % 2 == 0)
                    {
                        continue;
                    }

                    Brick br = new Brick();
                    br.Transform.X = 100 + (i * 65);
                    br.Transform.Y = 100 + (j * 33);
                    br.Health = 1 + rand.Next(3);
                    myBricks.Add(br);
                }
            }
        }

        public override void initialize()
        {
            rand = new Random();
            myBricks = new List<Brick>();

            int w = Bootstrap.getDisplay().getWidth();
            int h = Bootstrap.getDisplay().getHeight();

            // Create boundary walls (physics colliders), so ball bounces via engine
            // Thickness: tune if you like
            int t = 40;

            top = new Wall();
            top.Transform.X = 0;
            top.Transform.Y = 0;
            top.Transform.Wid = w;
            top.Transform.Ht = t;

            bottom = new Wall();
            bottom.Transform.X = 0;
            bottom.Transform.Y = h - t;
            bottom.Transform.Wid = w;
            bottom.Transform.Ht = t;

            left = new Wall();
            left.Transform.X = 0;
            left.Transform.Y = 0;
            left.Transform.Wid = t;
            left.Transform.Ht = h;

            right = new Wall();
            right.Transform.X = w - t;
            right.Transform.Y = 0;
            right.Transform.Wid = t;
            right.Transform.Ht = h;

            Paddle p = new Paddle();

            Ball b = new Ball();
            b.Transform.X = 50;
            b.Transform.Y = 50;
            b.Dir = new Vector2(1, 1);
            b.LastDir = new Vector2(1, 1);

            createBricks();
        }

        public override int getTargetFrameRate()
        {
            return 60;
        }
    }
}