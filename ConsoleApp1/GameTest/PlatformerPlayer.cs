using SDL2;
using Shard;
using System;
using System.Collections.Generic;

namespace GameTest
{
    class PlatformerPlayer : GameObject, InputListener, CollisionHandler
    {
        private bool moveLeft;
        private bool moveRight;
        private bool fastDrop;
        private bool jumpQueued;
        private bool grounded;
        private float verticalVelocity;
        private bool facingRight = true;
        private float animTimer;
        private int animFrame = 1;

        private const float MoveSpeed = 260f;
        private const float JumpVelocity = 430f;
        private const float Gravity = 900f;
        private const float FastDropGravityMul = 1.7f;
        private const float MaxFallSpeed = 820f;
        private const float WalkAnimInterval = 0.09f;

        public override void initialize()
        {
            Transform.SpritePath = Bootstrap.getAssetManager().getAssetPath("right1.png");
            grounded = false;
            verticalVelocity = 0f;
            animTimer = 0f;
            animFrame = 1;
            facingRight = true;

            addTag("Player");
            Bootstrap.getInput().addListener(this);
        }

        public override void update()
        {
            UpdateAnimation();
            Bootstrap.getDisplay().addToDraw(this);
        }

        public override void physicsUpdate()
        {
            // Use fixed physics timestep to avoid frame-rate dependent movement.
            float dt = PhysicsManager.getInstance().TimeInterval / 1000f;

            float horizontal = 0f;
            if (moveLeft) horizontal -= 1f;
            if (moveRight) horizontal += 1f;

            Transform.X += horizontal * MoveSpeed * dt;
            ResolveHorizontalCollisions();

            float g = Gravity * (fastDrop ? FastDropGravityMul : 1f);
            verticalVelocity += g * dt;
            if (verticalVelocity > MaxFallSpeed)
            {
                verticalVelocity = MaxFallSpeed;
            }

            if (jumpQueued && grounded)
            {
                verticalVelocity = -JumpVelocity;
                grounded = false;
            }
            jumpQueued = false;

            float previousY = Transform.Y;
            Transform.Y += verticalVelocity * dt;
            ResolveVerticalCollisions(previousY);
        }

        public void handleInput(InputEvent inp, string eventType)
        {
            if (!Bootstrap.IsPlayMode())
            {
                return;
            }

            if (eventType == "KeyDown")
            {
                if (inp.Key == (int)SDL.SDL_Scancode.SDL_SCANCODE_A) moveLeft = true;
                if (inp.Key == (int)SDL.SDL_Scancode.SDL_SCANCODE_D) moveRight = true;
                if (inp.Key == (int)SDL.SDL_Scancode.SDL_SCANCODE_S) fastDrop = true;
                if (inp.Key == (int)SDL.SDL_Scancode.SDL_SCANCODE_W || inp.Key == (int)SDL.SDL_Scancode.SDL_SCANCODE_SPACE)
                {
                    jumpQueued = true;
                }
            }
            else if (eventType == "KeyUp")
            {
                if (inp.Key == (int)SDL.SDL_Scancode.SDL_SCANCODE_A) moveLeft = false;
                if (inp.Key == (int)SDL.SDL_Scancode.SDL_SCANCODE_D) moveRight = false;
                if (inp.Key == (int)SDL.SDL_Scancode.SDL_SCANCODE_S) fastDrop = false;
            }
        }

        public void onCollisionEnter(PhysicsBody x)
        {
        }

        public void onCollisionExit(PhysicsBody x)
        {
        }

        public void onCollisionStay(PhysicsBody x)
        {
        }

        private void UpdateAnimation()
        {
            bool movingLeftOnly = moveLeft && !moveRight;
            bool movingRightOnly = moveRight && !moveLeft;
            bool walking = movingLeftOnly || movingRightOnly;

            if (movingLeftOnly)
            {
                facingRight = false;
            }
            else if (movingRightOnly)
            {
                facingRight = true;
            }

            if (walking)
            {
                animTimer += (float)Bootstrap.getDeltaTime();
                if (animTimer >= WalkAnimInterval)
                {
                    animTimer -= WalkAnimInterval;
                    animFrame += 1;
                    if (animFrame > 4)
                    {
                        animFrame = 1;
                    }
                }
            }
            else
            {
                animTimer = 0f;
                animFrame = 1;
            }

            string frameName = (facingRight ? "right" : "left") + animFrame + ".png";
            string path = Bootstrap.getAssetManager().getAssetPath(frameName);
            if (!string.IsNullOrWhiteSpace(path))
            {
                Transform.SpritePath = path;
            }
        }

        private void ResolveHorizontalCollisions()
        {
            float pw = GetObjectWidth(this, 32f);
            float ph = GetObjectHeight(this, 32f);

            foreach (GameObject ground in GetGroundObjects())
            {
                float gw = GetObjectWidth(ground, 64f);
                float gh = GetObjectHeight(ground, 16f);

                if (!IsOverlapping(Transform.X, Transform.Y, pw, ph, ground.Transform.X, ground.Transform.Y, gw, gh))
                {
                    continue;
                }

                if (moveRight && !moveLeft)
                {
                    Transform.X = ground.Transform.X - pw;
                }
                else if (moveLeft && !moveRight)
                {
                    Transform.X = ground.Transform.X + gw;
                }
            }
        }

        private void ResolveVerticalCollisions(float previousY)
        {
            grounded = false;
            float pw = GetObjectWidth(this, 32f);
            float ph = GetObjectHeight(this, 32f);
            float playerLeft = Transform.X;
            float playerRight = Transform.X + pw;
            float previousTop = previousY;
            float previousBottom = previousY + ph;
            float playerTop = Transform.Y;
            float playerBottom = Transform.Y + ph;

            foreach (GameObject ground in GetGroundObjects())
            {
                float gw = GetObjectWidth(ground, 64f);
                float gh = GetObjectHeight(ground, 16f);
                float groundLeft = ground.Transform.X;
                float groundTop = ground.Transform.Y;
                float groundRight = ground.Transform.X + gw;
                float groundBottom = ground.Transform.Y + gh;
                bool hasHorizontalOverlap = playerRight > groundLeft && playerLeft < groundRight;
                if (!hasHorizontalOverlap)
                {
                    continue;
                }

                // Sweep test: if we crossed the platform top this tick, snap to top.
                if (verticalVelocity >= 0f &&
                    previousBottom <= groundTop + 1f &&
                    playerBottom >= groundTop)
                {
                    Transform.Y = groundTop - ph;
                    verticalVelocity = 0f;
                    grounded = true;
                    playerTop = Transform.Y;
                    playerBottom = Transform.Y + ph;
                }
                // Fallback penetration correction: if we are already inside the platform while falling,
                // force the player back to the top surface.
                else if (verticalVelocity >= 0f &&
                         playerBottom > groundTop &&
                         playerTop < groundTop)
                {
                    Transform.Y = groundTop - ph;
                    verticalVelocity = 0f;
                    grounded = true;
                    playerTop = Transform.Y;
                    playerBottom = Transform.Y + ph;
                }
                // Sweep test for hitting underside of a platform while going up.
                else if (verticalVelocity < 0f &&
                         previousTop >= groundBottom - 1f &&
                         playerTop <= groundBottom)
                {
                    Transform.Y = groundBottom;
                    verticalVelocity = 0f;
                    playerTop = Transform.Y;
                    playerBottom = Transform.Y + ph;
                }
            }
        }

        private IEnumerable<GameObject> GetGroundObjects()
        {
            foreach (GameObject gob in GameObjectManager.getInstance().GetGameObjects())
            {
                if (gob == null || gob == this || gob.Transform == null)
                {
                    continue;
                }

                if (gob.checkTag("Ground"))
                {
                    yield return gob;
                }
            }
        }

        private static bool IsOverlapping(float ax, float ay, float aw, float ah, float bx, float by, float bw, float bh)
        {
            return ax < bx + bw && ax + aw > bx && ay < by + bh && ay + ah > by;
        }

        private static float GetObjectWidth(GameObject gob, float fallback)
        {
            if (gob is Cube2D cube)
            {
                return Math.Max(4f, cube.Width);
            }

            if (gob.Transform.Wid > 0)
            {
                return gob.Transform.Wid * gob.Transform.Scalex;
            }

            return fallback * gob.Transform.Scalex;
        }

        private static float GetObjectHeight(GameObject gob, float fallback)
        {
            if (gob is Cube2D cube)
            {
                return Math.Max(4f, cube.Height);
            }

            if (gob.Transform.Ht > 0)
            {
                return gob.Transform.Ht * gob.Transform.Scaley;
            }

            return fallback * gob.Transform.Scaley;
        }
    }
}
