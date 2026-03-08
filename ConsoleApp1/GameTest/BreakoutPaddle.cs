using SDL2;
using Shard;

namespace GameTest
{
    class BreakoutPaddle : Cube2D, InputListener
    {
        private bool _moveLeft;
        private bool _moveRight;

        public float MoveSpeed = 640f;
        public float CurrentMoveAxis = 0f;

        public override void initialize()
        {
            base.initialize();
            addTag("BreakoutPaddle");

            Width = 220f;
            Height = 26f;
            ColorR = 80;
            ColorG = 150;
            ColorB = 230;

            Bootstrap.getInput().addListener(this);
        }

        public override void physicsUpdate()
        {
            float dt = PhysicsManager.getInstance().TimeInterval / 1000f;
            float dir = 0f;
            if (_moveLeft) dir -= 1f;
            if (_moveRight) dir += 1f;
            CurrentMoveAxis = dir;

            Transform.X += dir * MoveSpeed * dt;

            int screenW = Bootstrap.getDisplay().getWidth();
            if (Transform.X < 0) Transform.X = 0;
            if (Transform.X + Width > screenW) Transform.X = screenW - Width;
        }

        public void handleInput(InputEvent inp, string eventType)
        {
            if (!Bootstrap.IsPlayMode())
            {
                return;
            }

            if (eventType == "KeyDown")
            {
                if (inp.Key == (int)SDL.SDL_Scancode.SDL_SCANCODE_A || inp.Key == (int)SDL.SDL_Scancode.SDL_SCANCODE_LEFT)
                {
                    _moveLeft = true;
                }

                if (inp.Key == (int)SDL.SDL_Scancode.SDL_SCANCODE_D || inp.Key == (int)SDL.SDL_Scancode.SDL_SCANCODE_RIGHT)
                {
                    _moveRight = true;
                }
            }
            else if (eventType == "KeyUp")
            {
                if (inp.Key == (int)SDL.SDL_Scancode.SDL_SCANCODE_A || inp.Key == (int)SDL.SDL_Scancode.SDL_SCANCODE_LEFT)
                {
                    _moveLeft = false;
                }

                if (inp.Key == (int)SDL.SDL_Scancode.SDL_SCANCODE_D || inp.Key == (int)SDL.SDL_Scancode.SDL_SCANCODE_RIGHT)
                {
                    _moveRight = false;
                }
            }
        }
    }
}
