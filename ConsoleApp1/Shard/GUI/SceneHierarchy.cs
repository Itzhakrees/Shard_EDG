using ImGuiNET;
using System;
using System.Collections.Generic;

namespace Shard.GUI
{
    class SceneHierarchy
    {
        private GameObject _clipboardSource;

        public void Draw()
        {
            ImGui.Begin("Scene Hierarchy");

            HandleCopyPasteShortcuts();

            var objects = GameObjectManager.getInstance().GetGameObjects();

            foreach (var gob in objects)
            {
                if (gob.Transform.Parent == null)
                {
                    DrawNode(gob);
                }
            }
            
            if (ImGui.BeginPopupContextWindow())
            {
                if (ImGui.MenuItem("Create Empty GameObject"))
                {
                    new GameObject();
                }

                if (ImGui.MenuItem("Create 2D Cube"))
                {
                    Cube2D cube = new Cube2D();
                    cube.Transform.X = 200;
                    cube.Transform.Y = 200;
                }

                ImGui.Separator();

                bool hasSelection = GuiManager.Instance.GetInspector().SelectedObject != null;
                if (!hasSelection)
                {
                    ImGui.BeginDisabled();
                }
                if (ImGui.MenuItem("Copy Selected", "Ctrl+C"))
                {
                    _clipboardSource = GuiManager.Instance.GetInspector().SelectedObject;
                }
                if (!hasSelection)
                {
                    ImGui.EndDisabled();
                }

                bool canPaste = _clipboardSource != null && _clipboardSource.Transform != null;
                if (!canPaste)
                {
                    ImGui.BeginDisabled();
                }
                if (ImGui.MenuItem("Paste", "Ctrl+V"))
                {
                    PasteClipboard();
                }
                if (!canPaste)
                {
                    ImGui.EndDisabled();
                }

                if (!hasSelection)
                {
                    ImGui.BeginDisabled();
                }
                if (ImGui.MenuItem("Delete Selected", "Del"))
                {
                    DeleteSelected();
                }
                if (!hasSelection)
                {
                    ImGui.EndDisabled();
                }
                ImGui.EndPopup();
            }

            ImGui.End();
        }

        private void DrawNode(GameObject gob)
        {
            ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.SpanAvailWidth;
            
            if (GuiManager.Instance.GetInspector().SelectedObject == gob)
            {
                flags |= ImGuiTreeNodeFlags.Selected;
            }

            if (gob.Transform.Children.Count == 0)
            {
                flags |= ImGuiTreeNodeFlags.Leaf;
            }

            bool opened = ImGui.TreeNodeEx(gob.GetHashCode().ToString(), flags, gob.ToString());

            if (ImGui.IsItemClicked())
            {
                GuiManager.Instance.GetInspector().SelectedObject = gob;
            }

            if (ImGui.BeginDragDropSource())
            {
                GuiManager.Instance.DragDropObject = gob;
                ImGui.SetDragDropPayload("GAMEOBJECT", IntPtr.Zero, 0);
                ImGui.Text(gob.ToString());
                ImGui.EndDragDropSource();
            }
            
            if (ImGui.BeginDragDropTarget())
            {
                var payload = ImGui.AcceptDragDropPayload("GAMEOBJECT");
                unsafe
                {
                    if (payload.NativePtr != null)
                    {
                        var dropped = GuiManager.Instance.DragDropObject;
                        if (dropped != null && dropped != gob)
                        {
                            if (!IsDescendant(dropped, gob))
                            {
                                dropped.Transform.Parent = gob.Transform;
                            }
                        }
                    }
                }
                ImGui.EndDragDropTarget();
            }

            if (opened)
            {
                foreach(var child in gob.Transform.Children)
                {
                    DrawNode(child.Owner);
                }
                ImGui.TreePop();
            }
        }
        
        private bool IsDescendant(GameObject parent, GameObject potentialChild)
        {
            foreach(var child in parent.Transform.Children)
            {
                if (child.Owner == potentialChild) return true;
                if (IsDescendant(child.Owner, potentialChild)) return true;
            }
            return false;
        }

        private void HandleCopyPasteShortcuts()
        {
            if (!ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows))
            {
                return;
            }

            ImGuiIOPtr io = ImGui.GetIO();
            if (io.KeyCtrl && ImGui.IsKeyPressed(ImGuiKey.C, false))
            {
                _clipboardSource = GuiManager.Instance.GetInspector().SelectedObject;
            }

            if (io.KeyCtrl && ImGui.IsKeyPressed(ImGuiKey.V, false))
            {
                PasteClipboard();
            }

            if (ImGui.IsKeyPressed(ImGuiKey.Delete, false))
            {
                DeleteSelected();
            }
        }

        private void PasteClipboard()
        {
            if (_clipboardSource == null || _clipboardSource.Transform == null)
            {
                return;
            }

            GameObject pasted = GameObjectManager.getInstance().DuplicateObject(_clipboardSource, 24f, 24f);
            if (pasted != null)
            {
                _clipboardSource = pasted;
                GuiManager.Instance.GetInspector().SelectedObject = pasted;
            }
        }

        private void DeleteSelected()
        {
            GameObject selected = GuiManager.Instance.GetInspector().SelectedObject;
            if (selected == null)
            {
                return;
            }

            if (_clipboardSource == selected)
            {
                _clipboardSource = null;
            }

            GameObjectManager.getInstance().DestroyObjectTree(selected);
            GuiManager.Instance.GetInspector().SelectedObject = null;
        }
    }
}
