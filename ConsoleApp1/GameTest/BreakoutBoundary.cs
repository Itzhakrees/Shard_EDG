using Shard;

namespace GameTest
{
    class BreakoutBoundary : GameObject, CollisionHandler
    {
        public float Width = 100f;
        public float Height = 20f;

        private ColliderRect _collider;
        private float _lastWidth;
        private float _lastHeight;

        public override void initialize()
        {
            addTag("BreakoutBoundary");

            setPhysicsEnabled();
            MyBody.Kinematic = true;
            MyBody.Mass = 1000f;
            MyBody.StopOnCollision = true;
            MyBody.UsesGravity = false;

            _collider = MyBody.addRectCollider(0, 0, (int)Width, (int)Height);
            _lastWidth = Width;
            _lastHeight = Height;
        }

        public override void update()
        {
            if (Width < 4f) Width = 4f;
            if (Height < 4f) Height = 4f;

            if (_collider != null && (_lastWidth != Width || _lastHeight != Height))
            {
                _collider.BaseWid = Width;
                _collider.BaseHt = Height;
                _lastWidth = Width;
                _lastHeight = Height;
                MyBody.recalculateColliders();
            }
        }

        public void onCollisionEnter(PhysicsBody x) { }
        public void onCollisionExit(PhysicsBody x) { }
        public void onCollisionStay(PhysicsBody x) { }
    }
}
