using System;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;

namespace Shard.GUI
{
    class ConsolePanel
    {
        private bool _showInfo = true;
        private bool _showWarning = true;
        private bool _showError = true;
        private bool _autoScroll = true;

        public void Draw()
        {
            ImGui.Begin("Console");

            if (ImGui.Button("Clear"))
            {
                Debug.getInstance().GetLogs().Clear();
            }
            ImGui.SameLine();
            ImGui.Checkbox("Info", ref _showInfo);
            ImGui.SameLine();
            ImGui.Checkbox("Warning", ref _showWarning);
            ImGui.SameLine();
            ImGui.Checkbox("Error", ref _showError);
            ImGui.SameLine();
            ImGui.Checkbox("Auto-scroll", ref _autoScroll);

            ImGui.Separator();

            ImGui.BeginChild("ScrollingRegion", new Vector2(0, 0), ImGuiChildFlags.None, ImGuiWindowFlags.HorizontalScrollbar);

            var logs = Debug.getInstance().GetLogs();
            lock (logs) // Just in case, though we are single threaded mostly
            {
                foreach (var log in logs)
                {
                    if (log.Level == Debug.DEBUG_LEVEL_ERROR && !_showError) continue;
                    if (log.Level == Debug.DEBUG_LEVEL_WARNING && !_showWarning) continue;
                    // Assuming DEBUG_LEVEL_ALL or INFO covers the rest
                    if (log.Level > Debug.DEBUG_LEVEL_WARNING && !_showInfo) continue; 

                    Vector4 color = new Vector4(1, 1, 1, 1);
                    if (log.Level == Debug.DEBUG_LEVEL_ERROR) color = new Vector4(1, 0, 0, 1);
                    else if (log.Level == Debug.DEBUG_LEVEL_WARNING) color = new Vector4(1, 1, 0, 1);

                    ImGui.TextColored(color, $"[{log.Time}] {log.Message}");
                }
            }

            if (_autoScroll && ImGui.GetScrollY() >= ImGui.GetScrollMaxY())
            {
                ImGui.SetScrollHereY(1.0f);
            }

            ImGui.EndChild();
            ImGui.End();
        }
    }
}
