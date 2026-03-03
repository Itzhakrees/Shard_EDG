/*
*
*   SDL provides an input layer, and we're using that.  This class tracks input, anchors it to the 
*       timing of the game loop, and converts the SDL events into one that is more abstract so games 
*       can be written more interchangeably.
*   @author Michael Heron
*   @version 1.0
*   
*/

using SDL2;

namespace Shard
{

    // We'll be using SDL2 here to provide our underlying input system.
    class InputFramework : InputSystem
    {

        double tick, timeInterval;
        public override void getInput()
        {

            SDL.SDL_Event ev;
            int res;
            InputEvent ie;

            tick += Bootstrap.getDeltaTime();

            if (tick < timeInterval)
            {
                return;
            }

            while (tick >= timeInterval)
            {

                res = SDL.SDL_PollEvent(out ev);


                if (res != 1)
                {
                    return;
                }
                
                Shard.GUI.GuiManager.Instance.ProcessEvent(ev);

                ie = new InputEvent();

                if (ev.type == SDL.SDL_EventType.SDL_QUIT)
                {
                    Bootstrap.requestQuit();
                    return;
                }

                if (ev.type == SDL.SDL_EventType.SDL_MOUSEMOTION)
                {
                    SDL.SDL_MouseMotionEvent mot;

                    mot = ev.motion;

                    ie.X = mot.x;
                    ie.Y = mot.y;

                    informListeners(ie, "MouseMotion");
                }

                if (ev.type == SDL.SDL_EventType.SDL_MOUSEBUTTONDOWN)
                {
                    SDL.SDL_MouseButtonEvent butt;

                    butt = ev.button;

                    ie.Button = (int)butt.button;
                    ie.X = butt.x;
                    ie.Y = butt.y;

                    informListeners(ie, "MouseDown");
                }

                if (ev.type == SDL.SDL_EventType.SDL_MOUSEBUTTONUP)
                {
                    SDL.SDL_MouseButtonEvent butt;

                    butt = ev.button;

                    ie.Button = (int)butt.button;
                    ie.X = butt.x;
                    ie.Y = butt.y;

                    informListeners(ie, "MouseUp");
                }

                if (ev.type == SDL.SDL_EventType.SDL_MOUSEWHEEL)
                {
                    SDL.SDL_MouseWheelEvent wh;

                    wh = ev.wheel;

                    ie.X = (int)wh.direction * wh.x;
                    ie.Y = (int)wh.direction * wh.y;

                    informListeners(ie, "MouseWheel");
                }


                if (ev.type == SDL.SDL_EventType.SDL_KEYDOWN)
                {
                    ie.Key = (int)ev.key.keysym.scancode;

                    // Global editor shortcut: toggle fullscreen without relying on GUI menu visibility.
                    bool isF11 = ev.key.keysym.scancode == SDL.SDL_Scancode.SDL_SCANCODE_F11;
                    bool isAltEnter =
                        ev.key.keysym.scancode == SDL.SDL_Scancode.SDL_SCANCODE_RETURN &&
                        (((SDL.SDL_Keymod)ev.key.keysym.mod & SDL.SDL_Keymod.KMOD_ALT) != 0);

                    if (isF11 || isAltEnter)
                    {
                        Bootstrap.getDisplay().toggleFullscreen();
                    }

                    Debug.getInstance().log("Keydown: " + ie.Key);
                    informListeners(ie, "KeyDown");
                }

                if (ev.type == SDL.SDL_EventType.SDL_KEYUP)
                {
                    ie.Key = (int)ev.key.keysym.scancode;
                    informListeners(ie, "KeyUp");
                }

                tick -= timeInterval;
            }


        }

        public override void initialize()
        {
            tick = 0;
            timeInterval = 1.0 / 60.0;
        }

    }
}
