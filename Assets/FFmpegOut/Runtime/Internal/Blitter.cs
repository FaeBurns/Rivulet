// FFmpegOut - FFmpeg video encoding plugin for Unity
// https://github.com/keijiro/KlakNDI

using UnityEngine;
using UnityEngine.Rendering;

namespace FFmpegOut
{
    internal sealed class Blitter : MonoBehaviour
    {
        #region Factory method

        private static System.Type[] s_initialComponents =
            { typeof(Camera), typeof(Blitter) };

        public static GameObject CreateInstance(Camera source)
        {
            GameObject go = new GameObject("Blitter", s_initialComponents);
            go.hideFlags = HideFlags.HideInHierarchy;

            Camera camera = go.GetComponent<Camera>();
            camera.cullingMask = 1 << UI_LAYER;
            camera.targetDisplay = source.targetDisplay;

            Blitter blitter = go.GetComponent<Blitter>();
            blitter.m_sourceTexture = source.targetTexture;

            return go;
        }

        #endregion

        #region Private members

        // Assuming that the 5th layer is "UI". #badcode
        private const int UI_LAYER = 5;

        private Texture m_sourceTexture;
        private Mesh m_mesh;
        private Material m_material;

        private void OnBeginCameraRendering(Camera camera)
        {
            if (m_mesh == null || camera != GetComponent<Camera>()) return;

            Graphics.DrawMesh(
                m_mesh, transform.localToWorldMatrix,
                m_material, UI_LAYER, camera
            );
        }

        #endregion

        #region MonoBehaviour implementation

        private void Update()
        {
            if (m_mesh == null)
            {
                // Index-only triangle mesh
                m_mesh = new Mesh();
                m_mesh.vertices = new Vector3[3];
                m_mesh.triangles = new int [] { 0, 1, 2 };
                m_mesh.bounds = new Bounds(Vector3.zero, Vector3.one);
                m_mesh.UploadMeshData(true);

                // Blitter shader material
                Shader shader = Shader.Find("Hidden/FFmpegOut/Blitter");
                m_material = new Material(shader);
                m_material.SetTexture("_MainTex", m_sourceTexture);

                // Register the camera render callback.
#if UNITY_URP
                UnityEngine.Experimental.Rendering.RenderPipeline.
                    beginCameraRendering += OnBeginCameraRendering; // SRP
#else
                Camera.onPreCull += OnBeginCameraRendering; // Legacy
#endif
            }
        }

        private void OnDisable()
        {
            if (m_mesh != null)
            {
                // Unregister the camera render callback.
#if UNITY_URP
                UnityEngine.Experimental.Rendering.RenderPipeline.
                    beginCameraRendering -= OnBeginCameraRendering; // SRP
#else
                Camera.onPreCull -= OnBeginCameraRendering; // Legacy
#endif

                // Destroy temporary objects.
                Destroy(m_mesh);
                Destroy(m_material);
                m_mesh = null;
                m_material = null;
            }
        }

        #endregion
    }
}
