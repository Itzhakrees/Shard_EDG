using Shard;

namespace GameDemo
{
    class Wall : GameObject
    {
        public const float HalfSize = 48f;

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