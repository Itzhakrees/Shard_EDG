using System;
using System.Drawing;

namespace Shard
{
    class Cube2D : GameObject, CollisionHandler
    {
        private ColliderRect _collider;
        private float _lastWidth;
        private float _lastHeight;
        private float _lastX;
        private float _lastY;
        private float _lastRot;
        private bool _appliedLegacyScale;

        // Editable in Inspector ("Fields")
        public float Width = 120f;
        public float Height = 24f;
        public int ColorR = 220;
        public int ColorG = 80;
        public int ColorB = 40;

        public override void initialize()
        {
            addTag("Ground");

            setPhysicsEnabled();
            MyBody.Kinematic = true;
            MyBody.Mass = 1000f;
            MyBody.StopOnCollision = true;
            MyBody.UsesGravity = false;

            _collider = MyBody.addRectCollider(0, 0, (int)Width, (int)Height);
            _lastWidth = Width;
            _lastHeight = Height;
            _lastX = Transform.X;
            _lastY = Transform.Y;
            _lastRot = Transform.Rotz;
            _appliedLegacyScale = false;
        }

        public override void update()
        {
            SyncColliderAndDraw();
        }

        public override void editorUpdate()
        {
            SyncColliderAndDraw();
        }

        protected void SyncColliderAndDraw()
        {
            ApplyLegacyScaleIfNeeded();
            bool colliderDirty = false;

            if (Width < 4f) Width = 4f;
            if (Height < 4f) Height = 4f;

            if (!NearlyEqual(_lastWidth, Width) || !NearlyEqual(_lastHeight, Height))
            {
                _collider.BaseWid = Width;
                _collider.BaseHt = Height;
                _lastWidth = Width;
                _lastHeight = Height;
                colliderDirty = true;
            }

            if (!NearlyEqual(_lastX, Transform.X) || !NearlyEqual(_lastY, Transform.Y) || !NearlyEqual(_lastRot, Transform.Rotz))
            {
                _lastX = Transform.X;
                _lastY = Transform.Y;
                _lastRot = Transform.Rotz;
                colliderDirty = true;
            }

            // In EDIT mode physics ticks are paused, so we must keep collider bounds in sync manually.
            if (colliderDirty)
            {
                MyBody?.recalculateColliders();
            }

            DrawFilledRect((int)Transform.X, (int)Transform.Y, (int)Width, (int)Height, ColorR, ColorG, ColorB, 255);
        }

        private void ApplyLegacyScaleIfNeeded()
        {
            // Backward compatibility: older scenes stored platform size in Transform scale.
            if (_appliedLegacyScale)
            {
                return;
            }

            if (Transform == null)
            {
                return;
            }

            if (Transform.Scalex != 1f || Transform.Scaley != 1f)
            {
                Width *= Transform.Scalex;
                Height *= Transform.Scaley;
                Transform.Scalex = 1f;
                Transform.Scaley = 1f;
            }

            _appliedLegacyScale = true;
        }

        private void DrawFilledRect(int x, int y, int w, int h, int r, int g, int b, int a)
        {
            Display d = Bootstrap.getDisplay();
            d.drawFilledRect(x, y, w, h, r, g, b, a);
        }

        private static bool NearlyEqual(float a, float b)
        {
            return Math.Abs(a - b) < 0.001f;
        }

        public void onCollisionEnter(PhysicsBody x) { }
        public void onCollisionExit(PhysicsBody x) { }
        public void onCollisionStay(PhysicsBody x) { }
    }
}
