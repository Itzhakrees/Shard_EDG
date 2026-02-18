using System;
using System.Collections.Generic;

namespace Shard
{
    class Animation
    {
        private List<string> frames;
        private double frameDuration;
        private bool loop;
        private int currentFrame;
        private double timeElapsed;
        private bool isPlaying;

        public Animation()
        {
            frames = new List<string>();
            frameDuration = 0.1; // Default 100ms per frame
            loop = true;
            currentFrame = 0;
            timeElapsed = 0;
            isPlaying = false;
        }

        public void loadFromPrefix(string prefix)
        {
            frames = Bootstrap.getAssetManager().getAssetPathList(prefix);
            if (frames.Count == 0)
            {
                Debug.Log("Animation: No frames found for prefix " + prefix);
            }
        }

        public void update()
        {
            if (!isPlaying || frames.Count == 0) return;

            timeElapsed += Bootstrap.getDeltaTime();

            if (timeElapsed >= frameDuration)
            {
                timeElapsed -= frameDuration;
                currentFrame++;

                if (currentFrame >= frames.Count)
                {
                    if (loop)
                    {
                        currentFrame = 0;
                    }
                    else
                    {
                        currentFrame = frames.Count - 1;
                        isPlaying = false;
                    }
                }
            }
        }

        public string getCurrentSprite()
        {
            if (frames.Count == 0) return null;
            return frames[currentFrame];
        }

        public void play()
        {
            isPlaying = true;
        }

        public void pause()
        {
            isPlaying = false;
        }

        public void stop()
        {
            isPlaying = false;
            currentFrame = 0;
            timeElapsed = 0;
        }

        public double FrameDuration { get => frameDuration; set => frameDuration = value; }
        public bool Loop { get => loop; set => loop = value; }
        public bool IsPlaying { get => isPlaying; }
        public int CurrentFrame { get => currentFrame; }
        public int FrameCount { get => frames.Count; }
    }
}
