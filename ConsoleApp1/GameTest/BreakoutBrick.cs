using Shard;

namespace GameTest
{
    class BreakoutBrick : Cube2D
    {
        public int Hits = 1;

        public override void initialize()
        {
            base.initialize();
            addTag("BreakoutBrick");

            Width = 96f;
            Height = 28f;
            ColorR = 230;
            ColorG = 120;
            ColorB = 60;
        }
    }
}
