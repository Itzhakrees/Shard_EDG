using Shard;
using System;
using System.Numerics;

namespace GameTest
{
    class BreakoutBall : GameObject, CollisionHandler
    {
        public float Speed = 8.5f;
        public float DirectionX = 0.72f;
        public float DirectionY = -0.72f;
        public int Radius = 10;

        private Vector2 _pendingDir;
        private Vector2 _lastDir;
        private float _startX;
        private float _startY;
        private ColliderCircle _collider;

        public override void initialize()
        {
            addTag("BreakoutBall");

            setPhysicsEnabled();
            _collider = MyBody.addCircleCollider(0, 0, Radius);
            _collider.RotateAtOffset = false;
            MyBody.Mass = 1f;
            MyBody.MaxForce = Speed;
            MyBody.Drag = 0f;
            MyBody.UsesGravity = false;
            MyBody.StopOnCollision = false;
            MyBody.ReflectOnCollision = true;
            MyBody.PassThrough = false;

            _pendingDir = Vector2.Normalize(new Vector2(DirectionX, DirectionY));
            _lastDir = _pendingDir;
        }

        public override void update()
        {
            if (_startX == 0f && _startY == 0f)
            {
                _startX = Transform.X;
                _startY = Transform.Y;
            }

            Bootstrap.getDisplay().drawFilledCircle((int)Transform.X, (int)Transform.Y, Radius, 240, 240, 240, 255);

            if (_collider != null && _collider.Rad != Radius)
            {
                _collider.Rad = Radius;
                MyBody.recalculateColliders();
            }

            EnforceScreenBoundsFallback();

            if (Transform.Y - Radius > Bootstrap.getDisplay().getHeight())
            {
                ResetBall();
            }
        }

        public override void physicsUpdate()
        {
            if (_pendingDir == Vector2.Zero)
            {
                return;
            }

            Vector2 dir = Vector2.Normalize(_pendingDir);

            if (dir.Y > -0.2f && dir.Y < 0f) dir.Y = -0.2f;
            else if (dir.Y < 0.2f && dir.Y >= 0f) dir.Y = 0.2f;

            if (dir.X > -0.2f && dir.X < 0f) dir.X = -0.2f;
            else if (dir.X < 0.2f && dir.X >= 0f) dir.X = 0.2f;

            dir = Vector2.Normalize(dir);
            MyBody.stopForces();
            MyBody.MaxForce = Speed;
            MyBody.addForce(dir, Speed);

            _lastDir = dir;
            _pendingDir = Vector2.Zero;
        }

        public void onCollisionEnter(PhysicsBody other)
        {
            if (other == null || other.Parent == null)
            {
                return;
            }

            if (other.Parent.checkTag("BreakoutPaddle"))
            {
                BreakoutPaddle paddle = other.Parent as BreakoutPaddle;
                if (paddle != null)
                {
                    float paddleCenter = paddle.Transform.X + (paddle.Width * 0.5f);
                    float hitNorm = (Transform.X - paddleCenter) / Math.Max(1f, paddle.Width * 0.5f);
                    hitNorm = Math.Clamp(hitNorm, -1f, 1f);
                    hitNorm += paddle.CurrentMoveAxis * 0.45f;
                    hitNorm = Math.Clamp(hitNorm, -1.25f, 1.25f);
                    _pendingDir = new Vector2(hitNorm, -Math.Abs(_lastDir.Y));
                }
                else
                {
                    _pendingDir = new Vector2(_lastDir.X, -Math.Abs(_lastDir.Y));
                }
                return;
            }

            if (other.Parent.checkTag("BreakoutBrick"))
            {
                if (other.Parent is BreakoutBrick brick)
                {
                    brick.Hits -= 1;
                    if (brick.Hits <= 0)
                    {
                        brick.ToBeDestroyed = true;
                    }
                }
                else
                {
                    other.Parent.ToBeDestroyed = true;
                }
            }
        }

        public void onCollisionExit(PhysicsBody other)
        {
        }

        public void onCollisionStay(PhysicsBody other)
        {
        }

        private void ResetBall()
        {
            MyBody.stopForces();

            BreakoutPaddle paddle = FindPaddle();
            if (paddle != null)
            {
                Transform.X = paddle.Transform.X + (paddle.Width * 0.5f);
                Transform.Y = paddle.Transform.Y - Radius - 4;
            }
            else
            {
                Transform.X = _startX;
                Transform.Y = _startY;
            }

            _pendingDir = Vector2.Normalize(new Vector2(DirectionX, DirectionY));
            _lastDir = _pendingDir;
        }

        private BreakoutPaddle FindPaddle()
        {
            foreach (GameObject gob in GameObjectManager.getInstance().GetGameObjects())
            {
                if (gob is BreakoutPaddle paddle)
                {
                    return paddle;
                }
            }

            return null;
        }

        private void EnforceScreenBoundsFallback()
        {
            float w = Bootstrap.getDisplay().getWidth();
            float h = Bootstrap.getDisplay().getHeight();
            bool adjusted = false;

            if (Transform.X < Radius)
            {
                Transform.X = Radius + 1;
                _pendingDir = new Vector2(Math.Abs(_lastDir.X), _lastDir.Y);
                adjusted = true;
            }
            else if (Transform.X > w - Radius)
            {
                Transform.X = w - Radius - 1;
                _pendingDir = new Vector2(-Math.Abs(_lastDir.X), _lastDir.Y);
                adjusted = true;
            }

            if (Transform.Y < Radius)
            {
                Transform.Y = Radius + 1;
                _pendingDir = new Vector2(_lastDir.X, Math.Abs(_lastDir.Y));
                adjusted = true;
            }

            if (adjusted)
            {
                MyBody.stopForces();
                MyBody.recalculateColliders();
            }
        }
    }
}
