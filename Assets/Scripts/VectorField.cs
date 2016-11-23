#if UNITY_EDITOR

using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

namespace ParticleFlow
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(MeshFilter))]
    public class VectorField : MonoBehaviour
    {
        public int size = 16;
        public int visualizationStep = 4;
        public float visualizationVelocityScale = 1.0f;
    
        Vector3[,,] velocityField = null;
        Color[,,] colorField = null;
        Sampler[] samplers = null;
        Node[] nodes;
        bool needsUpdate = false;

        public void SaveVectorFieldsAsTexture()
        {
            var velocityPixels = new Color[size * size * size];
            var colorPixels = new Color[size * size * size];

            for (var x = 0; x < size; ++x)
            {
                for (var y = 0; y < size; ++y)
                {
                    for (var z = 0; z < size; ++z)
                    {
                        var p = (velocityField[x, y, z] + Vector3.one) * .5f;
                        velocityPixels[y * size * size + z * size + x] = new Color(p.x, p.y, p.z, 1.0f);
                        colorPixels[y * size * size + z * size + x] = colorField[x, y, z];
                    }
                }
            }
            var path = EditorUtility.SaveFilePanel("Save", Application.dataPath, "vector_field", "");
            SavePixelsAsTexture(velocityPixels, size * size, size, path + "_velocity.png");
            SavePixelsAsTexture(colorPixels, size * size, size, path + "_color.png");
        }

        void SavePixelsAsTexture(Color[] pixels, int width, int height, string path)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBAFloat, false);
            texture.SetPixels(pixels);
            texture.Apply();

            Uri appDataUri = new Uri(Application.dataPath);
            Uri aboluteUri = new Uri(path); 
            Uri relativeUri = appDataUri.MakeRelativeUri(aboluteUri);
            var relativePath = relativeUri.OriginalString;

            File.WriteAllBytes(path, texture.EncodeToPNG());
            Texture2D.DestroyImmediate(texture, true);

            AssetDatabase.ImportAsset(relativePath);

            TextureImporter importer = AssetImporter.GetAtPath(relativePath) as TextureImporter;
            importer.mipmapEnabled = false;
            importer.textureFormat = TextureImporterFormat.AutomaticTruecolor;
            importer.SaveAndReimport();
        }

        void OnEnable()
        {
            CollectNodes();
        }

        void OnValidate()
        {
            needsUpdate = true;
        }

        void OnTransformChildrenChanged()
        {
            CollectNodes();
        }

        public void CollectNodes()
        {
            nodes = GetComponentsInChildren<Node>();

            List<Sampler> l = new List<Sampler>();
            foreach (Transform child in transform)
            {
                Sampler a = child.GetComponent<Sampler>();
                if (a != null)
                    l.Add(a);
            }
            samplers = l.ToArray();

            foreach (Node node in nodes)
            {
                node.OnChanged.RemoveAllListeners();
                node.OnChanged.AddListener(() => Update());
            }
            needsUpdate = true;
        }

        void UpdateField()
        {
            CollectSampler();
            ComputeVectorField();
            ComputeVisualizationMesh();
        }

        void Update()
        {
            if (nodes != null)
            {
                foreach (Node node in nodes)
                {
                    if (node.isDirty)
                    {
                        needsUpdate = true;
                    }
                    node.isDirty = false;
                }
            }
            
            if (needsUpdate)
            {
                needsUpdate = false;
                UpdateField();
            }
        }

        void CollectSampler()
        {
            samplers = GetComponentsInChildren<Sampler>();
            List<Sampler> tmp = new List<Sampler>();
            foreach (var c in samplers)
            {
                if (c.enabled && c.gameObject.activeInHierarchy)
                    tmp.Add(c);
            }
            samplers = tmp.ToArray();
        }

        void ComputeVectorField()
        {
            velocityField = new Vector3[size, size, size];
            colorField = new Color[size, size, size];
            for (int x = 0; x < size; ++x)
            {
                for (int y = 0; y < size; ++y)
                {
                    for (int z = 0; z < size; ++z)
                    {
                        // compute node position within a unit cube
                        Vector3 normalizedPosition = new Vector3(x, y, z) / (float)(size - 1);
                        Vector3 velocity = Vector3.zero;
                        Color color = Color.black;
                        float mSum = .0f;

                        for (int i = samplers.Length - 1; i > -1; --i)
                        {
                            Vector3 v;
                            Color c;
                            samplers[i].Sample(normalizedPosition, out v, out c);
                            velocity += v;
                            float m = v.magnitude; // color weight
                            mSum += m;
                            color += c * m;
                        }

                        velocityField[x, y, z] = velocity;
                        color /= Mathf.Max(.0000001f, mSum); // prevent div by 0
                        color.a = Mathf.Clamp01(color.a);
                        colorField[x, y, z] = color;
                    }
                }
            }
        }

        void ComputeVisualizationMesh()
        {
            var meshFilter = GetComponent<MeshFilter>();
            var mesh = meshFilter.sharedMesh == null ? new Mesh() : meshFilter.sharedMesh;

            int mSize = size / visualizationStep;
            int nVertices = 2 * mSize * mSize * mSize; // 2 vertices per node
            var vertices = new Vector3[nVertices];
            var colors = new Color[nVertices];
            var indices = new int[nVertices];
            int index = 0;

            for (var x = 0; x < mSize; ++x)
            {
                for (var y = 0; y < mSize; ++y)
                {
                    for (var z = 0; z < mSize; ++z)
                    {
                        var pos = new Vector3(x, y, z) / (float)(mSize - 1);
                        var vel = velocityField[x * visualizationStep, y * visualizationStep, z * visualizationStep];
                        var color = colorField[x * visualizationStep, y * visualizationStep, z * visualizationStep];
                        vertices[index] = pos;
                        indices[index] = index;
                        colors[index] = color;
                        ++index;
                        vertices[index] = pos + vel * visualizationVelocityScale;
                        indices[index] = index;
                        colors[index] = color;
                        ++index;
                    }
                }
            }

            mesh.Clear();
            mesh.vertices = vertices;
            mesh.colors = colors;
            mesh.SetIndices(indices, MeshTopology.Lines, 0);
            meshFilter.sharedMesh = mesh;
        }
    }
}
#endif
