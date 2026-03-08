using Shard;

namespace GameDemo
{
    class Background : GameObject
    {
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