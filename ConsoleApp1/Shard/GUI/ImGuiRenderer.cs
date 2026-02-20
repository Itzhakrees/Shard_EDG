using System;
using ImGuiNET;
using SDL2;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Numerics;

namespace Shard.GUI
{
    class ImGuiRenderer
    {
        private IntPtr _window;
        private IntPtr _renderer;
        private IntPtr _fontTexture;
        private IntPtr _mouseCursors = IntPtr.Zero;

        // SDL2 Scancode to ImGui Key mapping
        private Dictionary<int, ImGuiKey> _keyMap;

        public ImGuiRenderer(IntPtr window, IntPtr renderer)
        {
            _window = window;
            _renderer = renderer;

            IntPtr context = ImGui.CreateContext();
            ImGui.SetCurrentContext(context);
            
            var io = ImGui.GetIO();
            io.BackendFlags |= ImGuiBackendFlags.HasMouseCursors;
            io.BackendFlags |= ImGuiBackendFlags.HasSetMousePos;
            io.ConfigFlags |= ImGuiConfigFlags.DockingEnable; // Enable Docking

            SetupKeyMap();
            CreateDeviceObjects();
        }

        private void SetupKeyMap()
        {
            // Map SDL2 scancodes to ImGui keys
            // This is a partial mapping, add more as needed
            // ImGui.NET 1.90+ uses ImGuiKey enum directly in AddKeyEvent
        }

        public void ProcessEvent(SDL.SDL_Event e)
        {
            var io = ImGui.GetIO();
            switch (e.type)
            {
                case SDL.SDL_EventType.SDL_MOUSEMOTION:
                    io.AddMousePosEvent(e.motion.x, e.motion.y);
                    break;
                case SDL.SDL_EventType.SDL_MOUSEBUTTONDOWN:
                case SDL.SDL_EventType.SDL_MOUSEBUTTONUP:
                    int button = 0;
                    if (e.button.button == SDL.SDL_BUTTON_LEFT) button = 0;
                    if (e.button.button == SDL.SDL_BUTTON_RIGHT) button = 1;
                    if (e.button.button == SDL.SDL_BUTTON_MIDDLE) button = 2;
                    io.AddMouseButtonEvent(button, e.type == SDL.SDL_EventType.SDL_MOUSEBUTTONDOWN);
                    break;
                case SDL.SDL_EventType.SDL_MOUSEWHEEL:
                    io.AddMouseWheelEvent(e.wheel.x, e.wheel.y);
                    break;
                case SDL.SDL_EventType.SDL_TEXTINPUT:
                    unsafe
                    {
                         string text = System.Text.Encoding.UTF8.GetString(e.text.text);
                         text = text.Trim('\0');
                         if (text.Length > 0)
                             io.AddInputCharactersUTF8(text);
                    }
                    break;
                case SDL.SDL_EventType.SDL_KEYDOWN:
                case SDL.SDL_EventType.SDL_KEYUP:
                    UpdateKeyModifiers((SDL.SDL_Keymod)e.key.keysym.mod);
                    ImGuiKey key = KeycodeToImGuiKey(e.key.keysym.scancode);
                    if (key != ImGuiKey.None)
                        io.AddKeyEvent(key, e.type == SDL.SDL_EventType.SDL_KEYDOWN);
                    break;
            }
        }

        private void UpdateKeyModifiers(SDL.SDL_Keymod mod)
        {
            var io = ImGui.GetIO();
            io.AddKeyEvent(ImGuiKey.ModCtrl, (mod & SDL.SDL_Keymod.KMOD_CTRL) != 0);
            io.AddKeyEvent(ImGuiKey.ModShift, (mod & SDL.SDL_Keymod.KMOD_SHIFT) != 0);
            io.AddKeyEvent(ImGuiKey.ModAlt, (mod & SDL.SDL_Keymod.KMOD_ALT) != 0);
            io.AddKeyEvent(ImGuiKey.ModSuper, (mod & SDL.SDL_Keymod.KMOD_GUI) != 0);
        }

        private ImGuiKey KeycodeToImGuiKey(SDL.SDL_Scancode code)
        {
            switch (code)
            {
                case SDL.SDL_Scancode.SDL_SCANCODE_TAB: return ImGuiKey.Tab;
                case SDL.SDL_Scancode.SDL_SCANCODE_LEFT: return ImGuiKey.LeftArrow;
                case SDL.SDL_Scancode.SDL_SCANCODE_RIGHT: return ImGuiKey.RightArrow;
                case SDL.SDL_Scancode.SDL_SCANCODE_UP: return ImGuiKey.UpArrow;
                case SDL.SDL_Scancode.SDL_SCANCODE_DOWN: return ImGuiKey.DownArrow;
                case SDL.SDL_Scancode.SDL_SCANCODE_PAGEUP: return ImGuiKey.PageUp;
                case SDL.SDL_Scancode.SDL_SCANCODE_PAGEDOWN: return ImGuiKey.PageDown;
                case SDL.SDL_Scancode.SDL_SCANCODE_HOME: return ImGuiKey.Home;
                case SDL.SDL_Scancode.SDL_SCANCODE_END: return ImGuiKey.End;
                case SDL.SDL_Scancode.SDL_SCANCODE_INSERT: return ImGuiKey.Insert;
                case SDL.SDL_Scancode.SDL_SCANCODE_DELETE: return ImGuiKey.Delete;
                case SDL.SDL_Scancode.SDL_SCANCODE_BACKSPACE: return ImGuiKey.Backspace;
                case SDL.SDL_Scancode.SDL_SCANCODE_SPACE: return ImGuiKey.Space;
                case SDL.SDL_Scancode.SDL_SCANCODE_RETURN: return ImGuiKey.Enter;
                case SDL.SDL_Scancode.SDL_SCANCODE_ESCAPE: return ImGuiKey.Escape;
                case SDL.SDL_Scancode.SDL_SCANCODE_A: return ImGuiKey.A;
                case SDL.SDL_Scancode.SDL_SCANCODE_C: return ImGuiKey.C;
                case SDL.SDL_Scancode.SDL_SCANCODE_V: return ImGuiKey.V;
                case SDL.SDL_Scancode.SDL_SCANCODE_X: return ImGuiKey.X;
                case SDL.SDL_Scancode.SDL_SCANCODE_Y: return ImGuiKey.Y;
                case SDL.SDL_Scancode.SDL_SCANCODE_Z: return ImGuiKey.Z;
                default: return ImGuiKey.None;
            }
        }

        public void NewFrame()
        {
            var io = ImGui.GetIO();
            int w, h;
            SDL.SDL_GetWindowSize(_window, out w, out h);
            io.DisplaySize = new Vector2(w, h);
            io.DeltaTime = (float)Bootstrap.getDeltaTime(); 
            // If delta time is 0, set to small value to avoid div by zero in ImGui
            if (io.DeltaTime <= 0) io.DeltaTime = 1.0f / 60.0f;

            // Update Mouse Position
            int mx, my;
            uint mouseState = SDL.SDL_GetMouseState(out mx, out my);
            io.AddMousePosEvent(mx, my);

            ImGui.NewFrame();
        }

        public void Render()
        {
            ImGui.Render();
            RenderDrawData(ImGui.GetDrawData());
        }

        private void CreateDeviceObjects()
        {
            var io = ImGui.GetIO();
            unsafe
            {
                byte* pixels;
                int width, height;
                io.Fonts.GetTexDataAsRGBA32(out pixels, out width, out height);

                // Create Texture
                _fontTexture = SDL.SDL_CreateTexture(_renderer, SDL.SDL_PIXELFORMAT_RGBA8888, (int)SDL.SDL_TextureAccess.SDL_TEXTUREACCESS_STATIC, width, height);
                
                // Upload
                SDL.SDL_UpdateTexture(_fontTexture, IntPtr.Zero, (IntPtr)pixels, width * 4);
                
                // Store texture ID
                io.Fonts.SetTexID(_fontTexture);
                
                // Clear texture data
                io.Fonts.ClearTexData();
            }
        }

        private void RenderDrawData(ImGuiDrawDataPtr drawData)
        {
            if (drawData.DisplaySize.X <= 0 || drawData.DisplaySize.Y <= 0)
                return;

            for (int n = 0; n < drawData.CmdListsCount; n++)
            {
                ImGuiCmdListPtr cmdList = drawData.CmdLists[n];
                
                int vtxCount = cmdList.VtxBuffer.Size;
                int idxCount = cmdList.IdxBuffer.Size;
                
                SDL.SDL_Vertex[] sdlVertices = new SDL.SDL_Vertex[vtxCount];
                
                for (int i = 0; i < vtxCount; i++)
                {
                    var v = cmdList.VtxBuffer[i];
                    sdlVertices[i] = new SDL.SDL_Vertex
                    {
                        position = new SDL.SDL_FPoint { x = v.pos.X, y = v.pos.Y },
                        color = new SDL.SDL_Color { r = (byte)(v.col & 0xFF), g = (byte)((v.col >> 8) & 0xFF), b = (byte)((v.col >> 16) & 0xFF), a = (byte)((v.col >> 24) & 0xFF) },
                        tex_coord = new SDL.SDL_FPoint { x = v.uv.X, y = v.uv.Y }
                    };
                }

                for (int cmd_i = 0; cmd_i < cmdList.CmdBuffer.Size; cmd_i++)
                {
                    ImGuiCmdPtr pcmd = cmdList.CmdBuffer[cmd_i];
                    if (pcmd.UserCallback != IntPtr.Zero)
                    {
                         // Handle user callback
                    }
                    else
                    {
                        SDL.SDL_Rect clipRect;
                        clipRect.x = (int)pcmd.ClipRect.X;
                        clipRect.y = (int)pcmd.ClipRect.Y;
                        clipRect.w = (int)(pcmd.ClipRect.Z - pcmd.ClipRect.X);
                        clipRect.h = (int)(pcmd.ClipRect.W - pcmd.ClipRect.Y);
                        SDL.SDL_RenderSetClipRect(_renderer, ref clipRect);

                        IntPtr texture = pcmd.TextureId;
                        
                        int vtxOffset = (int)pcmd.VtxOffset;
                        int idxOffset = (int)pcmd.IdxOffset;
                        int elemCount = (int)pcmd.ElemCount;
                        
                        int[] cmdIndices = new int[elemCount];
                        for(int j=0; j<elemCount; j++)
                        {
                            cmdIndices[j] = (int)cmdList.IdxBuffer[idxOffset + j] + vtxOffset;
                        }
                        
                        SDL.SDL_RenderGeometry(_renderer, texture, sdlVertices, vtxCount, cmdIndices, elemCount);
                    }
                }
            }
            
            SDL.SDL_RenderSetClipRect(_renderer, IntPtr.Zero);
        }
    }
}
