using Shard;
using System.Collections.Generic;

namespace GameDemo
{
    class Penguin : GameObject
    {
        public float vx = 0;
        public float vy = 0;

        public bool Won = false;
        public bool IsSliding => sliding;

        public const float HalfSize = 45f;

        bool sliding = false;

        public override void initialize()
        {
            this.Visible = true;
            Won = false;
            vx = 0;
            vy = 0;
            sliding = false;
        }

        public override void update()
        {
            if (!Bootstrap.IsPlayMode())
            {
                Bootstrap.getDisplay().addToDraw(this);
                return;
            }

            if (sliding)
            {
                float targetX = Transform.X + vx;
                float targetY = Transform.Y + vy;

                // 先判 goal：碰到就赢，但不真正走进去
                if (CollidesWithGoal(targetX, targetY))
                {
                    Won = true;
                    stopSliding();
                }
                // 再判 wall：撞墙就停，也不走进去
                else if (CollidesWithWall(targetX, targetY))
                {
                    stopSliding();
                }
                else
                {
                    Transform.X = targetX;
                    Transform.Y = targetY;
                }
            }

            Bootstrap.getDisplay().addToDraw(this);
        }

        public void push(float dx, float dy)
        {
            if (sliding)
            {
                return;
            }

            vx = dx;
            vy = dy;
            sliding = true;
        }

        private void stopSliding()
        {
            vx = 0;
            vy = 0;
            sliding = false;
        }

        private Goal FindGoal()
        {
            List<GameObject> objects = GameObjectManager.getInstance().GetGameObjects();
            for (int i = 0; i < objects.Count; i++)
            {
                if (objects[i] is Goal goal)
                {
                    return goal;
                }
            }
            return null;
        }

        private bool CollidesWithWall(float x, float y)
        {
            List<GameObject> objects = GameObjectManager.getInstance().GetGameObjects();

            for (int i = 0; i < objects.Count; i++)
            {
                if (objects[i] is Wall wall)
                {
                    if (IsOverlapping(x, y, HalfSize, wall.Transform.X, wall.Transform.Y, Wall.HalfSize))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool CollidesWithGoal(float x, float y)
        {
            Goal goal = FindGoal();
            if (goal == null)
            {
                return false;
            }

            return IsOverlapping(x, y, HalfSize, goal.Transform.X, goal.Transform.Y, Goal.HalfSize);
        }

        private bool IsOverlapping(float x1, float y1, float h1, float x2, float y2, float h2)
        {
            return !(x1 + h1 < x2 - h2 ||
                     x1 - h1 > x2 + h2 ||
                     y1 + h1 < y2 - h2 ||
                     y1 - h1 > y2 + h2);
        }
    }
}