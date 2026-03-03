using ImGuiNET;
using System.Reflection;
using System.Collections.Generic;

namespace Shard.GUI
{
    class Inspector
    {
        private GameObject _selectedObject;

        public GameObject SelectedObject { get => _selectedObject; set => _selectedObject = value; }

        public void Draw()
        {
            ImGui.Begin("Inspector");

            if (_selectedObject != null && _selectedObject.Transform == null)
            {
                _selectedObject = null;
            }

            if (_selectedObject != null)
            {
                // Draw name or ID
                ImGui.Text($"Selected: {_selectedObject.GetType().Name}");
                ImGui.Separator();

                // Draw Components
                // Transform
                if (ImGui.CollapsingHeader("Transform", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    var trans = _selectedObject.Transform;
                    float x = trans.X;
                    float y = trans.Y;
                    if (ImGui.DragFloat("X", ref x)) trans.X = x;
                    if (ImGui.DragFloat("Y", ref y)) trans.Y = y;
                    
                    float rot = trans.Rotz;
                    if (ImGui.DragFloat("Rotation", ref rot)) trans.Rotz = rot;

                    float sx = trans.Scalex;
                    float sy = trans.Scaley;
                    if (ImGui.DragFloat("Scale X", ref sx, 0.1f)) trans.Scalex = sx;
                    if (ImGui.DragFloat("Scale Y", ref sy, 0.1f)) trans.Scaley = sy;
                }

                // Draw other components using reflection
                // We need to access components list if available or reflect on GameObject fields
                
                // Since we added Component system, let's iterate components
                var components = _selectedObject.GetComponents();
                foreach (var comp in components)
                {
                    if (ImGui.CollapsingHeader(comp.GetType().Name, ImGuiTreeNodeFlags.DefaultOpen))
                    {
                        DrawProperties(comp);
                    }
                }
                
                // Also reflect on GameObject fields for legacy support
                if (ImGui.CollapsingHeader("Fields", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    DrawProperties(_selectedObject);
                }

                // Add Component Button
                if (ImGui.Button("Add Component"))
                {
                    ImGui.OpenPopup("AddComponentPopup");
                }
                
                if (ImGui.BeginPopup("AddComponentPopup"))
                {
                    if (ImGui.Selectable("AnimationComponent"))
                    {
                        _selectedObject.addComponent(new AnimationComponent());
                    }

                    if (ImGui.Selectable("SpawnerComponent"))
                    {
                        _selectedObject.addComponent(new SpawnerComponent());
                    }
                    // Add more
                    ImGui.EndPopup();
                }
            }

            ImGui.End();
        }

        private void DrawProperties(object obj)
        {
            var type = obj.GetType();
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (var field in fields)
            {
                var value = field.GetValue(obj);
                
                if (field.FieldType == typeof(float))
                {
                    float val = (float)value;
                    if (ImGui.DragFloat(field.Name, ref val))
                        field.SetValue(obj, val);
                }
                else if (field.FieldType == typeof(int))
                {
                    int val = (int)value;
                    if (ImGui.DragInt(field.Name, ref val))
                        field.SetValue(obj, val);
                }
                else if (field.FieldType == typeof(bool))
                {
                    bool val = (bool)value;
                    if (ImGui.Checkbox(field.Name, ref val))
                        field.SetValue(obj, val);
                }
                else if (field.FieldType == typeof(string))
                {
                    string val = (string)value;
                    if (val == null) val = "";
                    if (ImGui.InputText(field.Name, ref val, 100))
                        field.SetValue(obj, val);
                }
                // Color?
            }
        }
    }
}
