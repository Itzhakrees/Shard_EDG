using ImGuiNET;
using SDL2;
using System;

namespace Shard.GUI
{
    class Viewport
    {
        private IntPtr _framebufferTexture;
        private int _width, _height;
        private IntPtr _renderer;
        private bool _autoFitView = true;

        public Viewport(IntPtr renderer)
        {
            _renderer = renderer;
            _width = 800;
            _height = 600;
            
            // Create Framebuffer Texture (Render Target)
            _framebufferTexture = SDL.SDL_CreateTexture(_renderer, 
                SDL.SDL_PIXELFORMAT_RGBA8888, 
                (int)SDL.SDL_TextureAccess.SDL_TEXTUREACCESS_TARGET, 
                _width, _height);
        }

        public void Resize(int w, int h)
        {
            if (_width != w || _height != h)
            {
                _width = w;
                _height = h;
                if (_framebufferTexture != IntPtr.Zero)
                    SDL.SDL_DestroyTexture(_framebufferTexture);
                
                _framebufferTexture = SDL.SDL_CreateTexture(_renderer, 
                    SDL.SDL_PIXELFORMAT_RGBA8888, 
                    (int)SDL.SDL_TextureAccess.SDL_TEXTUREACCESS_TARGET, 
                    _width, _height);
            }
        }

        public void BeginRender()
        {
            // Set Render Target to Texture
            SDL.SDL_SetRenderTarget(_renderer, _framebufferTexture);
            // Clear
            SDL.SDL_SetRenderDrawColor(_renderer, 50, 50, 50, 255);
            SDL.SDL_RenderClear(_renderer);
        }

        public void EndRender()
        {
            // Reset Render Target to default (window)
            SDL.SDL_SetRenderTarget(_renderer, IntPtr.Zero);
        }

        public void Draw()
        {
            ImGui.Begin("Viewport", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);

            DrawViewportToolbar();

            var size = ImGui.GetContentRegionAvail();

            if (size.X < 1f) size.X = 1f;
            if (size.Y < 1f) size.Y = 1f;
            
            if ((int)size.X != _width || (int)size.Y != _height)
            {
                Resize((int)size.X, (int)size.Y);
            }

            ApplyAutoFitCamera(size.X, size.Y);
            
            // Render Image
            ImGui.Image(_framebufferTexture, size);
            
            // Handle Camera Input
            HandleCameraInput();

            // Handle Drag Drop
            if (ImGui.BeginDragDropTarget())
            {
                var payload = ImGui.AcceptDragDropPayload("ASSET_PATH");
                unsafe 
                {
                    if (payload.NativePtr != null)
                    {
                        string path = System.Runtime.InteropServices.Marshal.PtrToStringAnsi(payload.Data);
                        InstantiateAsset(path);
                    }
                }
                ImGui.EndDragDropTarget();
            }

            // Gizmos logic
            DrawGizmos();
            
            ImGui.End();
        }

        private void HandleCameraInput()
        {
            if (_autoFitView)
            {
                return;
            }

            if (ImGui.IsItemHovered())
            {
                var io = ImGui.GetIO();
                var display = Bootstrap.getDisplay() as DisplaySDL;
                if (display == null) return;
                
                // Zoom
                if (io.MouseWheel != 0)
                {
                    display.Zoom += io.MouseWheel * 0.1f;
                    if (display.Zoom < 0.1f) display.Zoom = 0.1f;
                }
                
                // Pan (Middle Mouse)
                if (ImGui.IsMouseDragging(ImGuiMouseButton.Middle))
                {
                    var delta = io.MouseDelta;
                    display.CameraX -= delta.X / display.Zoom;
                    display.CameraY -= delta.Y / display.Zoom;
                }
            }
        }

        private void InstantiateAsset(string path)
        {
             // Simple logic: Create GameObject with Sprite
            if (path.EndsWith(".png") || path.EndsWith(".jpg"))
            {
                 GameObject go = new GameObject();
                 go.Transform.SpritePath = path;
                 
                 var min = ImGui.GetItemRectMin();
                 var mouse = ImGui.GetMousePos();
                 var localMouse = mouse - min;
                 
                 var display = Bootstrap.getDisplay() as DisplaySDL;
                 if (display != null)
                 {
                     go.Transform.X = (localMouse.X / display.Zoom) + display.CameraX;
                     go.Transform.Y = (localMouse.Y / display.Zoom) + display.CameraY;
                 }
            }
        }

        private void DrawGizmos()
        {
             var selected = GuiManager.Instance.SelectedObject;
             if (selected == null) return;
             
             var display = Bootstrap.getDisplay() as DisplaySDL;
             if (display == null) return;

             var drawList = ImGui.GetWindowDrawList();
             
             var min = ImGui.GetItemRectMin();
             
             float screenX = (selected.Transform.X - display.CameraX) * display.Zoom + min.X;
             float screenY = (selected.Transform.Y - display.CameraY) * display.Zoom + min.Y;
             
             // Draw Arrows
             // X Axis (Red)
             drawList.AddLine(new System.Numerics.Vector2(screenX, screenY), 
                              new System.Numerics.Vector2(screenX + 50, screenY), 
                              ImGui.GetColorU32(new System.Numerics.Vector4(1,0,0,1)), 3.0f);
             
             // Y Axis (Green)
             drawList.AddLine(new System.Numerics.Vector2(screenX, screenY), 
                              new System.Numerics.Vector2(screenX, screenY + 50), 
                              ImGui.GetColorU32(new System.Numerics.Vector4(0,1,0,1)), 3.0f);
        }

        private void DrawViewportToolbar()
        {
            bool autoFit = _autoFitView;
            if (ImGui.Checkbox("Auto Fit", ref autoFit))
            {
                _autoFitView = autoFit;
            }

            ImGui.SameLine();
            if (ImGui.Button("Reset View"))
            {
                var display = Bootstrap.getDisplay() as DisplaySDL;
                if (display != null)
                {
                    display.CameraX = 0f;
                    display.CameraY = 0f;
                    display.Zoom = 1f;
                }
            }

            ImGui.Separator();
        }

        private void ApplyAutoFitCamera(float viewportW, float viewportH)
        {
            if (!_autoFitView)
            {
                return;
            }

            var display = Bootstrap.getDisplay() as DisplaySDL;
            if (display == null)
            {
                return;
            }

            float designW = Bootstrap.getDisplay().getWidth();
            float designH = Bootstrap.getDisplay().getHeight();
            if (designW <= 1f || designH <= 1f)
            {
                return;
            }

            // Auto Fit: keep aspect ratio and ensure full scene is visible.
            float fitZoom = Math.Min(viewportW / designW, viewportH / designH);
            if (fitZoom <= 0.01f)
            {
                fitZoom = 0.01f;
            }

            float drawW = designW * fitZoom;
            float drawH = designH * fitZoom;
            float offsetX = (viewportW - drawW) * 0.5f;
            float offsetY = (viewportH - drawH) * 0.5f;

            display.Zoom = fitZoom;
            display.CameraX = -offsetX / fitZoom;
            display.CameraY = -offsetY / fitZoom;
        }
    }
}
