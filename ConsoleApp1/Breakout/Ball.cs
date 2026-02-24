using Shard;
using System.Numerics;

namespace GameBreakout
{
    class Ball : GameObject, CollisionHandler
    {
        float cx, cy;
        Vector2 dir, lastDir;
        internal Vector2 LastDir { get => lastDir; set => lastDir = value; }
        internal Vector2 Dir { get => dir; set => dir = value; }

        public override void initialize()
        {
            this.Transform.SpritePath = Bootstrap.getAssetManager().getAssetPath("ball.png");
            setPhysicsEnabled();

            MyBody.addCircleCollider();

            MyBody.Mass = 1;
            MyBody.MaxForce = 15;
            MyBody.Drag = 0f;
            MyBody.UsesGravity = false;
            MyBody.StopOnCollision = false;
            MyBody.ReflectOnCollision = true;

            Transform.Scalex = 2;
            Transform.Scaley = 2;

            Transform.rotate(90);
        }

        public override void update()
        {
            Bootstrap.getDisplay().addToDraw(this);
        }

        public void onCollisionStay(PhysicsBody other) { }

        public void onCollisionEnter(PhysicsBody other)
        {
            if (other.Parent.checkTag("Paddle"))
            {
                Dir = new Vector2(Transform.Centre.X - other.Trans.Centre.X, LastDir.Y * -1);
            }

            if (other.Parent.checkTag("Brick"))
            {
                // keep default reflect; no extra dir override
            }

            // Walls: do nothing; engine reflection handles it
        }

        public void changeDir(int x, int y)
        {
            if (Dir == Vector2.Zero)
            {
                dir = lastDir;
            }

            if (x != 0)
            {
                dir = new Vector2(x, dir.Y);
            }

            if (y != 0)
            {
                dir = new Vector2(dir.X, y);
            }
        }

        public override void physicsUpdate()
        {
            if (Dir != Vector2.Zero)
            {
                Dir = Vector2.Normalize(Dir);

                if (Dir.Y > -0.2f && Dir.Y < 0)
                {
                    dir.Y = -0.2f;
                }
                else if (Dir.Y < 0.2f && Dir.Y >= 0)
                {
                    dir.Y = 0.2f;
                }

                if (Dir.X > -0.2f && Dir.X < 0)
                {
                    dir.X = -0.2f;
                }
                else if (Dir.X < 0.2f && Dir.X >= 0)
                {
                    dir.X = 0.2f;
                }

                MyBody.stopForces();
                MyBody.addForce(Dir, 15);

                LastDir = Dir;
                dir = Vector2.Zero;
            }
        }

        public void onCollisionExit(PhysicsBody x) { }

        public override string ToString()
        {
            return "Ball: [" + Transform.X + ", " + Transform.Y + ", Dir: " + Dir + ", LastDir: " + LastDir + ", " + Transform.Lx + ", " + Transform.Ly + "]";
        }
    }
}