using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

namespace ParticleFlow
{
    [CustomEditor(typeof(Node))]
    public class NodeEditor : Editor
    {
        bool[] visibility;

        public override void OnInspectorGUI()
        {
            Node node = (Node)target;

            if (visibility == null)
                visibility = new bool[node.propertyGroups.Length];

            EditorGUILayout.LabelField("Color");
            var c = EditorGUILayout.ColorField(node.color);

            bool changed = node.color != c;

            node.color = c;

            if (node.propertyGroups != null)
            {
                int i = 0;
                foreach (var group in node.propertyGroups)
                {
                    visibility[i] = EditorGUILayout.ToggleLeft(group.name.ToUpper(), visibility[i]);
                    if (visibility[i])
                    {
                        foreach (var prop in group.properties)
                        {
                            EditorGUILayout.LabelField(prop.name);
                            var v = EditorGUILayout.Slider(prop.value, prop.rangeMin, prop.rangeMax);
                            if (v != prop.value)
                            {
                                prop.value = v;
                                changed = true;
                            }
                        }
                    }
                    ++i;
                }
            }

            if (changed)
            {
                node.OnValidate();
                EditorUtility.SetDirty(target);
            }
        }
    }
}