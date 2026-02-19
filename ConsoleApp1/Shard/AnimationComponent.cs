using System;

namespace Shard
{
    class AnimationComponent : Component
    {
        private Animation currentAnimation;

        public void playAnimation(Animation anim)
        {
            currentAnimation = anim;
            if (currentAnimation != null)
            {
                currentAnimation.play();
            }
        }

        public override void update()
        {
            if (currentAnimation != null)
            {
                currentAnimation.update();
                string sprite = currentAnimation.getCurrentSprite();
                if (sprite != null && Owner != null)
                {
                    Owner.Transform.SpritePath = sprite;
                }
            }
        }

        public Animation CurrentAnimation { get => currentAnimation; }
    }
}
