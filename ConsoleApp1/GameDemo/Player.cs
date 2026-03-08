using SDL2;
using Shard;
using System.Drawing;
using System.Collections.Generic;

namespace GameDemo
{
    class Player : GameObject, InputListener
    {
        bool up, down, left, right;
        float speed = 4f;

        public const float HalfSize = 49f;

        public override void initialize()
        {
            this.Visible = true;
            Bootstrap.getInput().addListener(this);
        }

        public override void update()
        {
            if (!Bootstrap.IsPlayMode())
            {
                Bootstrap.getDisplay().addToDraw(this);
                return;
            }

            float dx = 0;
            float dy = 0;

            if (up) dy -= speed;
            if (down) dy += speed;
            if (left) dx -= speed;
            if (right) dx += speed;

            if (dx != 0 || dy != 0)
            {
                float targetX = Transform.X + dx;
                float targetY = Transform.Y + dy;

                Penguin penguin = FindPenguin();

                // 先检查目标位置是不是会撞到企鹅
                if (penguin != null && IsOverlapping(
                        targetX, targetY, HalfSize,
                        penguin.Transform.X, penguin.Transform.Y, Penguin.HalfSize))
                {
                    // 撞到企鹅时，不允许玩家和企鹅重叠
                    // 只尝试推动企鹅，玩家自己不前进
                    TryPushPenguin(penguin, dx, dy);
                }
                else
                {
                    // 没撞到企鹅时，再检查墙和 goal
                    if (!CollidesWithWall(targetX, targetY) &&
                        !CollidesWithGoal(targetX, targetY))
                    {
                        Transform.X = targetX;
                        Transform.Y = targetY;
                    }
                }
            }

            Penguin p = FindPenguin();
            if (p != null && p.Won)
            {
                Bootstrap.getDisplay().showText("YOU WIN", 400, 200, 120, Color.White);
            }

            Bootstrap.getDisplay().addToDraw(this);
        }

        public void handleInput(InputEvent inp, string eventType)
        {
            if (!Bootstrap.IsPlayMode())
            {
                return;
            }

            if (eventType == "KeyDown")
            {
                if (inp.Key == (int)SDL.SDL_Scancode.SDL_SCANCODE_W) up = true;
                if (inp.Key == (int)SDL.SDL_Scancode.SDL_SCANCODE_S) down = true;
                if (inp.Key == (int)SDL.SDL_Scancode.SDL_SCANCODE_A) left = true;
                if (inp.Key == (int)SDL.SDL_Scancode.SDL_SCANCODE_D) right = true;
            }
            else if (eventType == "KeyUp")
            {
                if (inp.Key == (int)SDL.SDL_Scancode.SDL_SCANCODE_W) up = false;
                if (inp.Key == (int)SDL.SDL_Scancode.SDL_SCANCODE_S) down = false;
                if (inp.Key == (int)SDL.SDL_Scancode.SDL_SCANCODE_A) left = false;
                if (inp.Key == (int)SDL.SDL_Scancode.SDL_SCANCODE_D) right = false;
            }
        }

        private void TryPushPenguin(Penguin penguin, float dx, float dy)
        {
            if (dx == 0 && dy == 0)
            {
                return;
            }

            // 企鹅已经在滑时，不再重复推
            if (penguin.IsSliding)
            {
                return;
            }

            if (dx > 0) penguin.push(8f, 0f);
            else if (dx < 0) penguin.push(-8f, 0f);
            else if (dy > 0) penguin.push(0f, 8f);
            else if (dy < 0) penguin.push(0f, -8f);
        }

        private Penguin FindPenguin()
        {
            List<GameObject> objects = GameObjectManager.getInstance().GetGameObjects();
            for (int i = 0; i < objects.Count; i++)
            {
                if (objects[i] is Penguin penguin)
                {
                    return penguin;
                }
            }
            return null;
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