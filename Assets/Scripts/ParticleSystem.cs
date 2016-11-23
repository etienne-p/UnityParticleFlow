using UnityEngine;
using System.Collections;

namespace ParticleFlow
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class ParticleSystem : MonoBehaviour
    {
        public Texture particleTex;
        public Texture velocityLookup;
        public Texture colorLookup;
        public Shader updateColorShader;
        public Shader updateVelocityShader;
        public Shader renderShader;
        public float particlesSizeFactor = .02f;
        public float particlesAgingFactor = .02f;
        public float particlesMobilityThreshold = .02f;
        public float particlesSizeFactorOffset = .0f;
        public float particlesSizeFactorMul = 1.0f;
        public float velocitySpeedFactor = 1.0f;
        public int numParticlesSquareRoot = 8;
        public bool drawDebugTextures = false;

        Material updateVelocityMaterial = null;
        Material renderMaterial = null;
        RenderTexture[] bufferTextures = null;
        int bufferSwapIndex = 0;
        int vectorFieldSize = 0;

        void OnValidate()
        {
            ReleaseAssets();
            if (velocityLookup == null)
                return;
            vectorFieldSize = velocityLookup.height;

            CreateBufferTextures();
            CreateMaterials();
            CreateMesh();
        }

        void Start()
        {
            OnValidate();
        }

        void OnDestroy()
        {
            ReleaseAssets();
        }

        void Update()
        {
            // useful while updating in the editor 
            if (updateVelocityMaterial == null)
                return;
            // compute particles update using ping pong buffers
            var sourceIndex = bufferSwapIndex;
            bufferSwapIndex = (bufferSwapIndex + 1) % 2;
            var destIndex = bufferSwapIndex;

            // update velocity 
            updateVelocityMaterial.SetFloat("_DeltaTime", (Time.deltaTime * velocitySpeedFactor));
            updateVelocityMaterial.SetFloat("_BaseTime", (Time.realtimeSinceStartup * velocitySpeedFactor));
            Graphics.Blit(bufferTextures[sourceIndex], bufferTextures[destIndex], updateVelocityMaterial);

            // sync mesh vertices position
            renderMaterial.SetFloat("_QuadSize", particlesSizeFactor);
            renderMaterial.SetTexture("_PositionBufferTex", bufferTextures[destIndex]);
        }

        void OnRenderObject()
        {
            if (!drawDebugTextures || velocityLookup == null)
                return;
            GL.PushMatrix();
            Graphics.DrawTexture(new Rect(-4, 5, vectorFieldSize, 1), velocityLookup);
            Graphics.DrawTexture(new Rect(-4, 0, 4, 4), bufferTextures[0]);
            Graphics.DrawTexture(new Rect(0, 0, 4, 4), bufferTextures[1]);
            GL.PopMatrix();
        }

        void ReleaseAssets()
        {
            if (bufferTextures != null)
            {
                for (var i = 0; i < bufferTextures.Length; ++i)
                {
                    bufferTextures[i].Release();
                    RenderTexture.DestroyImmediate(bufferTextures[i], true);
                    bufferTextures[i] = null;
                }
                bufferTextures = null;
            }
        }

        void CreateBufferTextures()
        {
            bufferSwapIndex = 0;
            bufferTextures = new RenderTexture[2];
            for (var i = 0; i < 2; ++i)
            {
                bufferTextures[i] = new RenderTexture(
                    numParticlesSquareRoot, numParticlesSquareRoot, 0, RenderTextureFormat.ARGBFloat);
                bufferTextures[i].Create();
                bufferTextures[i].filterMode = FilterMode.Point;
            }

            var numParticles = numParticlesSquareRoot * numParticlesSquareRoot;

            var velocityPixels = new Color[numParticles];

            for (var i = 0; i < numParticles; ++i)
            {
                velocityPixels[i].r = Random.value;
                velocityPixels[i].g = Random.value;
                velocityPixels[i].b = Random.value;
                velocityPixels[i].a = Random.value;
            }

            var velTmpTex = new Texture2D(
                                numParticlesSquareRoot, numParticlesSquareRoot, TextureFormat.RGBAFloat, false);
            velTmpTex.SetPixels(velocityPixels);
            velTmpTex.Apply();
            Util.CopyTexture2DToRenderTexture(velTmpTex, bufferTextures[0]);
            Util.CopyTexture2DToRenderTexture(velTmpTex, bufferTextures[1]);

            DestroyImmediate(velTmpTex, true);
        }

        void CreateMaterials()
        {
            if (updateVelocityMaterial == null)
                updateVelocityMaterial = new Material(updateVelocityShader);
            updateVelocityMaterial.SetFloat("_FieldSize", vectorFieldSize);
            updateVelocityMaterial.SetTexture("_VelocityLookupTex", velocityLookup);
            updateVelocityMaterial.SetFloat("_AgingFactor", particlesAgingFactor);
            updateVelocityMaterial.SetFloat("_MobilityThreshold", particlesMobilityThreshold);

            renderMaterial = new Material(renderShader);
            renderMaterial.SetTexture("_MainTex", particleTex);
            renderMaterial.SetFloat("_SizeFactorOffset", particlesSizeFactorOffset);
            renderMaterial.SetFloat("_SizeFactorMul", particlesSizeFactorMul);
            renderMaterial.SetFloat("_FieldSize", vectorFieldSize);
            renderMaterial.SetTexture("_ColorLookupTex", colorLookup);
            renderMaterial.SetTexture("_VelocityLookupTex", velocityLookup);
            GetComponent<MeshRenderer>().sharedMaterial = renderMaterial;
        }

        void CreateMesh()
        {
            var meshFilter = GetComponent<MeshFilter>();
            var mesh = meshFilter.sharedMesh == null ? new Mesh() : meshFilter.sharedMesh;

            var numParticles = numParticlesSquareRoot * numParticlesSquareRoot;
            var numVertices = numParticles * 4; // 1 quad (4 vertices) per particle
            var numIndices = numParticles * 6; // 2 tris per quad

            Vector3[] vertices = new Vector3[numVertices];
            Vector2[] uvs = new Vector2[numVertices];
            int[] indices = new int[numIndices];

            var pxSize = 1.0f / (float)numParticlesSquareRoot; // sample texel center

            for (var i = 0; i < numParticles; ++i)
            {

                var u = (float)(i % numParticlesSquareRoot) / (float)numParticlesSquareRoot + pxSize * .5f;
                var v = (float)(i / numParticlesSquareRoot) / (float)numParticlesSquareRoot + pxSize * .5f;

                // note that we pass particles position buffer tex uv as xy
                // the z coodinates holds an angle used to build the quad

                // triangle 1
                indices[i * 6] = i * 4;
                indices[i * 6 + 1] = i * 4 + 1;
                indices[i * 6 + 2] = i * 4 + 2;
                // triangle 2
                indices[i * 6 + 3] = i * 4 + 2;
                indices[i * 6 + 4] = i * 4 + 3;
                indices[i * 6 + 5] = i * 4;

                // top left
                vertices[i * 4] = new Vector3(u, v, .0f);
                uvs[i * 4] = new Vector2(0, 0);

                // top right
                vertices[i * 4 + 1] = new Vector3(u, v, -Mathf.PI * .5f);
                uvs[i * 4 + 1] = new Vector2(0, 1);

                // bottom right
                vertices[i * 4 + 2] = new Vector3(u, v, -Mathf.PI);
                uvs[i * 4 + 2] = new Vector2(1, 1);

                // bottom left
                vertices[i * 4 + 3] = new Vector3(u, v, -Mathf.PI * 1.5f);
                uvs[i * 4 + 3] = new Vector2(1, 0);
            }

            mesh.Clear();
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = indices;
            meshFilter.sharedMesh = mesh;
        }
    }
}