using System;
using ImGuiNET;
using SDL2;
using System.Collections.Generic;

namespace Shard.GUI
{
    class GuiManager
    {
        private static GuiManager _instance;
        private ImGuiRenderer _renderer;
        private bool _showDemoWindow = true;

        // Panels
        private SceneHierarchy _sceneHierarchy;
        private Inspector _inspector;
        private Viewport _viewport;
        private ContentBrowser _contentBrowser;
        private ConsolePanel _consolePanel;

        public GameObject DragDropObject { get; set; }

        public GameObject SelectedObject
        {
            get => _inspector.SelectedObject;
            set => _inspector.SelectedObject = value;
        }

        public static GuiManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new GuiManager();
                return _instance;
            }
        }

        public static string DragDropPayload;

        public void Initialize(IntPtr window, IntPtr renderer)
        {
            _renderer = new ImGuiRenderer(window, renderer);
            
            _sceneHierarchy = new SceneHierarchy();
            _inspector = new Inspector();
            _viewport = new Viewport(renderer);
            _contentBrowser = new ContentBrowser();
            _consolePanel = new ConsolePanel();
        }

        public void ProcessEvent(SDL.SDL_Event e)
        {
            if (_renderer != null)
                _renderer.ProcessEvent(e);
        }

        public void Render()
        {
            if (_renderer == null) return;

            _renderer.NewFrame();

            // Dockspace
            ImGui.DockSpaceOverViewport(0, null, ImGuiDockNodeFlags.PassthruCentralNode);

            // Draw Panels
            _sceneHierarchy.Draw();
            _inspector.Draw();
            _viewport.Draw();
            _contentBrowser.Draw();
            _consolePanel.Draw();

            if (_showDemoWindow)
                ImGui.ShowDemoWindow(ref _showDemoWindow);

            _renderer.Render();
        }

        public Inspector GetInspector() => _inspector;
        public Viewport GetViewport() => _viewport;
    }
}
