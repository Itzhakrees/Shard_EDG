using System;
using System.IO;
using System.Numerics;
using ImGuiNET;

namespace Shard.GUI
{
    class ContentBrowser
    {
        private string _currentDirectory;
        private string _baseDirectory;

        public ContentBrowser()
        {
            _baseDirectory = Bootstrap.getBaseDir();
            if (string.IsNullOrEmpty(_baseDirectory))
            {
                _baseDirectory = Directory.GetCurrentDirectory();
            }
            _currentDirectory = _baseDirectory;
        }

        public void Draw()
        {
            ImGui.Begin("Content Browser");

            if (_currentDirectory != _baseDirectory)
            {
                if (ImGui.Button("<-"))
                {
                    _currentDirectory = Directory.GetParent(_currentDirectory).FullName;
                }
            }

            ImGui.SameLine();
            ImGui.Text(_currentDirectory);

            ImGui.Separator();

            float padding = 16.0f;
            float thumbnailSize = 64.0f;
            float cellSize = thumbnailSize + padding;

            float panelWidth = ImGui.GetContentRegionAvail().X;
            int columnCount = (int)(panelWidth / cellSize);
            if (columnCount < 1) columnCount = 1;

            ImGui.Columns(columnCount, "ContentBrowserColumns", false);

            try
            {
                var directories = Directory.GetDirectories(_currentDirectory);
                foreach (var dir in directories)
                {
                    var dirName = Path.GetFileName(dir);
                    ImGui.PushID(dirName);
                    if (ImGui.Button(dirName, new Vector2(thumbnailSize, thumbnailSize)))
                    {
                        _currentDirectory = dir;
                    }
                    ImGui.TextWrapped(dirName);
                    ImGui.NextColumn();
                    ImGui.PopID();
                }

                var files = Directory.GetFiles(_currentDirectory);
                foreach (var file in files)
                {
                    var fileName = Path.GetFileName(file);
                    ImGui.PushID(fileName);
                    ImGui.Button(fileName, new Vector2(thumbnailSize, thumbnailSize));

                    if (ImGui.BeginDragDropSource())
                    {
                        ImGui.SetDragDropPayload("CONTENT_BROWSER_ITEM", IntPtr.Zero, 0);
                        // We need to pass the file path.
                        // Since SetDragDropPayload takes a pointer, we can't easily pass a C# string directly without marshalling.
                        // For now, we'll just set a flag or use a static variable to hold the dragged file.
                        // Or we can just use the payload to indicate type and use a static property in GuiManager or here.
                        GuiManager.DragDropPayload = file;
                        
                        ImGui.Text(fileName);
                        ImGui.EndDragDropSource();
                    }

                    ImGui.TextWrapped(fileName);
                    ImGui.NextColumn();
                    ImGui.PopID();
                }
            }
            catch (Exception ex)
            {
                ImGui.TextColored(new Vector4(1, 0, 0, 1), "Error accessing directory: " + ex.Message);
            }

            ImGui.Columns(1);

            ImGui.End();
        }
    }
}
