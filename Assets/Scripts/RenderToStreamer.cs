using FFmpegOut;
using FFmpegOut.LiveStream;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class RenderToStreamer : MonoBehaviour
{
    private static readonly int s_eyeIndex = Shader.PropertyToID("_eyeIndex");

    private int m_leftEyeRenderCount = 0;
    private int m_rightEyeRenderCount = 0;

    [SerializeField]
    private StreamCameraCapture m_cameraCapture;

    [SerializeField]
    private Shader m_combineEyesShader;

    private Camera m_camera = null!;

    private Material m_combineEyesMaterial = null!;

    public RenderTexture ImageDestinationTexture { get; set; } = null;

    private void Awake()
    {
        m_camera = GetComponent<Camera>();

        m_combineEyesMaterial = new Material(m_combineEyesShader);
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        // passthrough
        // do at start so early exit can occur
        Graphics.Blit(source, destination);

        // exit if no destination texture exists
        if (ImageDestinationTexture == null) return;

        if (m_camera.stereoActiveEye == Camera.MonoOrStereoscopicEye.Left)
        {
            m_combineEyesMaterial.SetInt(s_eyeIndex, 0);
            Graphics.Blit(source, ImageDestinationTexture, m_combineEyesMaterial);
            m_leftEyeRenderCount++;
        }
        else if (m_camera.stereoActiveEye == Camera.MonoOrStereoscopicEye.Right)
        {
            m_combineEyesMaterial.SetInt(s_eyeIndex, 1);
            Graphics.Blit(source, ImageDestinationTexture, m_combineEyesMaterial);
            m_rightEyeRenderCount++;
        }
        else
        {
            Debug.LogError("Invalid active eye");
        }

        // if both eyes have been rendered then push frame to ffmpeg
        if (m_leftEyeRenderCount > 0 && m_rightEyeRenderCount > 0)
        {
            if (m_leftEyeRenderCount > 1 || m_rightEyeRenderCount > 1)
                Debug.LogError($"More than one of each eye has been rendered per push. left: {m_leftEyeRenderCount} | right: {m_rightEyeRenderCount}");

            m_leftEyeRenderCount = 0;
            m_rightEyeRenderCount = 0;
            m_cameraCapture.DoFrameUpdate();
        }
    }
}