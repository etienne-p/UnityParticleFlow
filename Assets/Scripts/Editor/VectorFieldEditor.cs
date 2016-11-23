using UnityEngine;
using System.Collections;
using UnityEditor;

namespace ParticleFlow
{
    [CustomEditor(typeof(VectorField))]
    public class VectorFieldEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            VectorField obj = (VectorField)target;
            if (GUILayout.Button("Export Lookup Textures"))
            {
                obj.SaveVectorFieldsAsTexture();
            }
        }
    }
}
