using ImGuiNET;
using System;
using System.Collections.Generic;

namespace Shard.GUI
{
    class SceneHierarchy
    {
        public void Draw()
        {
            ImGui.Begin("Scene Hierarchy");

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
    }
}
