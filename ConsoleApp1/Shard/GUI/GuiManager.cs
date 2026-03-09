using System;
using ImGuiNET;
using SDL2;
using System.Collections.Generic;
using System.Numerics;
using System.IO;
using System.Threading;

namespace Shard.GUI
{
    class GuiManager
    {
        private static GuiManager _instance;
        private ImGuiRenderer _renderer;
        private IntPtr _window;
        private bool _showDemoWindow = false;
        private bool _showSceneHierarchy = true;
        private bool _showInspector = true;
        private bool _showViewport = true;
        private bool _showContentBrowser = true;
        private bool _showConsole = true;
        private bool _forceLayoutOnce = true;

        // Panels
        private SceneHierarchy _sceneHierarchy;
        private Inspector _inspector;
        private Viewport _viewport;
        private ContentBrowser _contentBrowser;
        private ConsolePanel _consolePanel;
        
        private string _pendingSceneLoadPath = null;

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
            if (_renderer != null)
            {
                _window = window;
                return;
            }

            _window = window;
            _renderer = new ImGuiRenderer(window, renderer);
            ApplyModernTheme();
            
            _sceneHierarchy = new SceneHierarchy();
            _inspector = new Inspector();
            _viewport = new Viewport(renderer);
            _contentBrowser = new ContentBrowser();
            _consolePanel = new ConsolePanel();
        }

        private void ApplyModernTheme()
        {
            ImGui.StyleColorsDark();
            ImGui.GetIO().FontGlobalScale = 1.0f;

            var style = ImGui.GetStyle();
            style.WindowRounding = 6f;
            style.ChildRounding = 6f;
            style.FrameRounding = 5f;
            style.PopupRounding = 6f;
            style.ScrollbarRounding = 10f;
            style.GrabRounding = 5f;
            style.TabRounding = 5f;
            style.FrameBorderSize = 1f;
            style.WindowBorderSize = 1f;

            style.WindowPadding = new Vector2(10f, 10f);
            style.FramePadding = new Vector2(8f, 6f);
            style.ItemSpacing = new Vector2(8f, 6f);
            style.ItemInnerSpacing = new Vector2(6f, 4f);

            var colors = style.Colors;
            colors[(int)ImGuiCol.Text] = new Vector4(0.86f, 0.86f, 0.86f, 1.00f);
            colors[(int)ImGuiCol.TextDisabled] = new Vector4(0.55f, 0.55f, 0.55f, 1.00f);
            colors[(int)ImGuiCol.WindowBg] = new Vector4(0.13f, 0.13f, 0.13f, 1.00f);
            colors[(int)ImGuiCol.ChildBg] = new Vector4(0.15f, 0.15f, 0.15f, 1.00f);
            colors[(int)ImGuiCol.PopupBg] = new Vector4(0.16f, 0.16f, 0.16f, 0.98f);
            colors[(int)ImGuiCol.Border] = new Vector4(0.27f, 0.27f, 0.27f, 1.00f);
            colors[(int)ImGuiCol.FrameBg] = new Vector4(0.22f, 0.22f, 0.22f, 1.00f);
            colors[(int)ImGuiCol.FrameBgHovered] = new Vector4(0.27f, 0.27f, 0.27f, 1.00f);
            colors[(int)ImGuiCol.FrameBgActive] = new Vector4(0.30f, 0.30f, 0.30f, 1.00f);
            colors[(int)ImGuiCol.Header] = new Vector4(0.24f, 0.24f, 0.24f, 1.00f);
            colors[(int)ImGuiCol.HeaderHovered] = new Vector4(0.30f, 0.30f, 0.30f, 1.00f);
            colors[(int)ImGuiCol.HeaderActive] = new Vector4(0.34f, 0.34f, 0.34f, 1.00f);
            colors[(int)ImGuiCol.Button] = new Vector4(0.24f, 0.24f, 0.24f, 1.00f);
            colors[(int)ImGuiCol.ButtonHovered] = new Vector4(0.31f, 0.31f, 0.31f, 1.00f);
            colors[(int)ImGuiCol.ButtonActive] = new Vector4(0.35f, 0.35f, 0.35f, 1.00f);
            colors[(int)ImGuiCol.Tab] = new Vector4(0.18f, 0.18f, 0.18f, 1.00f);
            colors[(int)ImGuiCol.TabHovered] = new Vector4(0.28f, 0.28f, 0.28f, 1.00f);
            colors[(int)ImGuiCol.TabSelected] = new Vector4(0.24f, 0.24f, 0.24f, 1.00f);
            colors[(int)ImGuiCol.TextSelectedBg] = new Vector4(0.42f, 0.42f, 0.42f, 0.35f);
            colors[(int)ImGuiCol.NavCursor] = new Vector4(0.70f, 0.70f, 0.70f, 0.60f);
            colors[(int)ImGuiCol.DockingPreview] = new Vector4(0.42f, 0.42f, 0.42f, 0.35f);
        }

        public void ProcessEvent(SDL.SDL_Event e)
        {
            if (_renderer != null)
                _renderer.ProcessEvent(e);
        }

        public void Render()
        {
            if (_renderer == null) return;

            // Check for pending scene load from the file dialog thread
            if (_pendingSceneLoadPath != null)
            {
                string path = _pendingSceneLoadPath;
                _pendingSceneLoadPath = null;
                
                bool ok = GameObjectManager.getInstance().LoadSceneFromFile(path);
                if (ok)
                {
                    Bootstrap.setScenePath(path);
                    _inspector.SelectedObject = null;
                    Debug.getInstance().log("Scene loaded: " + path);
                }
                else
                {
                    Debug.getInstance().log("Scene load failed: " + path, Debug.DEBUG_LEVEL_ERROR);
                }
            }

            _renderer.NewFrame();
            HandleEditorShortcuts();
            DrawMainDockspace();

            if (_showSceneHierarchy)
            {
                ApplyWindowLayoutHint(PanelKind.SceneHierarchy);
                _sceneHierarchy.Draw();
            }

            if (_showInspector)
            {
                ApplyWindowLayoutHint(PanelKind.Inspector);
                _inspector.Draw();
            }

            if (_showViewport)
            {
                ApplyWindowLayoutHint(PanelKind.Viewport);
                _viewport.Draw();
            }

            if (_showContentBrowser)
            {
                ApplyWindowLayoutHint(PanelKind.ContentBrowser);
                _contentBrowser.Draw();
            }

            if (_showConsole)
            {
                ApplyWindowLayoutHint(PanelKind.Console);
                _consolePanel.Draw();
            }

            _forceLayoutOnce = false;

            if (_showDemoWindow)
                ImGui.ShowDemoWindow(ref _showDemoWindow);

            _renderer.Render();
        }

        private void HandleEditorShortcuts()
        {
            ImGuiIOPtr io = ImGui.GetIO();

            if (ImGui.IsKeyPressed(ImGuiKey.F11, false))
            {
                ToggleFullscreen();
            }

            if (ImGui.IsKeyPressed(ImGuiKey.F5, false))
            {
                Bootstrap.TogglePlayMode();
            }

            if (!Bootstrap.IsPlayMode() && io.KeyCtrl && ImGui.IsKeyPressed(ImGuiKey.S, false))
            {
                string path = Bootstrap.getScenePath();
                bool ok = GameObjectManager.getInstance().SaveSceneToFile(path);

                if (ok)
                {
                    Debug.getInstance().log("Scene saved: " + path);
                }
                else
                {
                    Debug.getInstance().log("Scene save failed: " + path, Debug.DEBUG_LEVEL_ERROR);
                }
            }
        }

        private void DrawMainDockspace()
        {
            ImGuiViewportPtr viewport = ImGui.GetMainViewport();
            ImGui.SetNextWindowPos(viewport.Pos);
            ImGui.SetNextWindowSize(viewport.Size);
            ImGui.SetNextWindowViewport(viewport.ID);

            ImGuiWindowFlags flags =
                ImGuiWindowFlags.NoDocking |
                ImGuiWindowFlags.NoTitleBar |
                ImGuiWindowFlags.NoCollapse |
                ImGuiWindowFlags.NoResize |
                ImGuiWindowFlags.NoMove |
                ImGuiWindowFlags.NoBringToFrontOnFocus |
                ImGuiWindowFlags.NoNavFocus |
                ImGuiWindowFlags.MenuBar;

            ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0.0f);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0.0f);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 0));
            ImGui.Begin("MainDockspace", flags);
            ImGui.PopStyleVar(3);

            DrawMainMenuBar();

            uint dockspaceId = ImGui.GetID("ShardEditorDockspace");
            ImGui.DockSpace(dockspaceId, Vector2.Zero, ImGuiDockNodeFlags.None);

            ImGui.End();
        }

        private void DrawMainMenuBar()
        {
            if (!ImGui.BeginMenuBar()) return;

            if (ImGui.BeginMenu("File"))
            {
                bool canSave = !Bootstrap.IsPlayMode();
                if (!canSave)
                {
                    ImGui.BeginDisabled();
                }

                if (ImGui.MenuItem("Select Scene"))
                {
                    OpenSceneSelectionDialog();
                }

                if (ImGui.MenuItem("Reload Scene"))
                {
                    string path = Bootstrap.getScenePath();
                    bool ok = GameObjectManager.getInstance().LoadSceneFromFile(path);

                    if (ok)
                    {
                        Bootstrap.setScenePath(path);
                        _inspector.SelectedObject = null;
                        Debug.getInstance().log("Scene loaded: " + path);
                    }
                    else
                    {
                        Debug.getInstance().log("Scene load failed: " + path, Debug.DEBUG_LEVEL_WARNING);
                    }
                }

                if (ImGui.MenuItem("Save Scene", "Ctrl+S"))
                {
                    string path = Bootstrap.getScenePath();
                    bool ok = GameObjectManager.getInstance().SaveSceneToFile(path);

                    if (ok)
                    {
                        Debug.getInstance().log("Scene saved: " + path);
                    }
                    else
                    {
                        Debug.getInstance().log("Scene save failed: " + path, Debug.DEBUG_LEVEL_ERROR);
                    }
                }

                if (!canSave)
                {
                    ImGui.EndDisabled();
                }

                if (ImGui.MenuItem("Exit"))
                {
                    Bootstrap.requestQuit();
                }

                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("Run"))
            {
                bool play = Bootstrap.IsPlayMode();
                if (ImGui.MenuItem(play ? "Stop" : "Start", "F5"))
                {
                    Bootstrap.TogglePlayMode();
                }

                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("View"))
            {
                ImGui.MenuItem("Viewport", "", ref _showViewport);
                ImGui.MenuItem("Scene Hierarchy", "", ref _showSceneHierarchy);
                ImGui.MenuItem("Inspector", "", ref _showInspector);
                ImGui.MenuItem("Content Browser", "", ref _showContentBrowser);
                ImGui.MenuItem("Console", "", ref _showConsole);
                ImGui.MenuItem("ImGui Demo", "", ref _showDemoWindow);
                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("Window"))
            {
                bool fullscreen = IsFullscreen();
                if (ImGui.MenuItem("Toggle Fullscreen", "F11", fullscreen))
                {
                    ToggleFullscreen();
                }

                if (ImGui.MenuItem("Reset Layout"))
                {
                    _forceLayoutOnce = true;
                }

                ImGui.EndMenu();
            }

            ImGui.Separator();
            ImGui.TextUnformatted(Bootstrap.IsPlayMode() ? "PLAY" : "EDIT");

            ImGui.EndMenuBar();
        }

        private enum PanelKind
        {
            SceneHierarchy,
            Inspector,
            Viewport,
            ContentBrowser,
            Console
        }

        private void ApplyWindowLayoutHint(PanelKind panel)
        {
            ImGuiViewportPtr vp = ImGui.GetMainViewport();
            float menuBar = 24f;
            float padding = 8f;
            float top = vp.Pos.Y + menuBar + padding;
            float left = vp.Pos.X + padding;
            float totalW = vp.Size.X - padding * 2f;
            float totalH = vp.Size.Y - menuBar - padding * 2f;

            float leftPanelW = MathF.Min(300f, totalW * 0.22f);
            float rightPanelW = MathF.Min(360f, totalW * 0.26f);
            float centerW = MathF.Max(420f, totalW - leftPanelW - rightPanelW - padding * 2f);
            float bottomH = MathF.Min(270f, totalH * 0.30f);
            float topH = MathF.Max(260f, totalH - bottomH - padding);
            float centerX = left + leftPanelW + padding;
            float rightX = centerX + centerW + padding;
            float bottomY = top + topH + padding;
            float contentW = MathF.Max(240f, centerW * 0.45f);

            ImGuiCond cond = _forceLayoutOnce ? ImGuiCond.Always : ImGuiCond.FirstUseEver;

            switch (panel)
            {
                case PanelKind.SceneHierarchy:
                    ImGui.SetNextWindowPos(new Vector2(left, top), cond);
                    ImGui.SetNextWindowSize(new Vector2(leftPanelW, totalH), cond);
                    break;
                case PanelKind.Inspector:
                    ImGui.SetNextWindowPos(new Vector2(rightX, top), cond);
                    ImGui.SetNextWindowSize(new Vector2(rightPanelW, totalH), cond);
                    break;
                case PanelKind.Viewport:
                    ImGui.SetNextWindowPos(new Vector2(centerX, top), cond);
                    ImGui.SetNextWindowSize(new Vector2(centerW, topH), cond);
                    break;
                case PanelKind.ContentBrowser:
                    ImGui.SetNextWindowPos(new Vector2(centerX, bottomY), cond);
                    ImGui.SetNextWindowSize(new Vector2(contentW, bottomH), cond);
                    break;
                case PanelKind.Console:
                    ImGui.SetNextWindowPos(new Vector2(centerX + contentW + padding, bottomY), cond);
                    ImGui.SetNextWindowSize(new Vector2(MathF.Max(220f, centerW - contentW - padding), bottomH), cond);
                    break;
            }
        }

        private bool IsFullscreen()
        {
            if (_window == IntPtr.Zero) return false;
            uint flags = SDL.SDL_GetWindowFlags(_window);
            return (flags & (uint)SDL.SDL_WindowFlags.SDL_WINDOW_FULLSCREEN_DESKTOP) != 0;
        }

        private void ToggleFullscreen()
        {
            if (_window == IntPtr.Zero) return;

            if (IsFullscreen())
            {
                SDL.SDL_SetWindowFullscreen(_window, 0);
            }
            else
            {
                SDL.SDL_SetWindowFullscreen(_window, (uint)SDL.SDL_WindowFlags.SDL_WINDOW_FULLSCREEN_DESKTOP);
            }
        }

        private void OpenSceneSelectionDialog()
        {
            if (Bootstrap.IsPlayMode())
            {
                Debug.getInstance().log("Cannot load scene while in PLAY mode. Stop first.", Debug.DEBUG_LEVEL_WARNING);
                return;
            }

            // Start file dialog thread
            Thread t = new Thread(() => 
            {
                try 
                {
                    // This sleep helps ensure the thread has time to initialize its message loop if needed, 
                    // though for ShowDialog it's usually automatic.
                    // However, sometimes quick thread execution can be weird with COM.
                    
                    // Also check if we need to Application.Run() or just ShowDialog.
                    // ShowDialog handles its own message loop.
                    
                    string selectedPath = null;
                    using (var openFileDialog = new System.Windows.Forms.OpenFileDialog())
                    {
                        openFileDialog.InitialDirectory = Path.Combine(Bootstrap.getBaseDir(), "Assets", "Scenes");
                        openFileDialog.Filter = "Scene files (*.json)|*.json";
                        openFileDialog.FilterIndex = 1;
                        openFileDialog.RestoreDirectory = true;
                        openFileDialog.AutoUpgradeEnabled = true; // Ensure new style dialogs

                        if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                        {
                            selectedPath = openFileDialog.FileName;
                        }
                    }

                    if (selectedPath != null)
                    {
                        _pendingSceneLoadPath = selectedPath;
                    }
                    else
                    {
                        // Don't log on cancel to avoid spam if user just closes it
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error in file dialog: " + ex.Message);
                }
            });
            
            t.SetApartmentState(ApartmentState.STA);
            t.IsBackground = true; // Make it a background thread so it doesn't prevent app exit
            t.Start();
        }

        public Inspector GetInspector() => _inspector;
        public Viewport GetViewport() => _viewport;
    }
}
