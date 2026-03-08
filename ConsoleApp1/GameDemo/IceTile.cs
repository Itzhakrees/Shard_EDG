using Shard;

namespace GameDemo
{
    class IceTile : GameObject
    {
        public const float HalfSize = 49f;

        public override void initialize()
        {
            this.Visible = true;
        }

        public override void update()
        {
            Bootstrap.getDisplay().addToDraw(this);
        }
    }
}