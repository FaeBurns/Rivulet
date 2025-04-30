using FFmpegOut;
using FFmpegOut.LiveStream;
using PostProcessing;
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

    [SerializeField]
    private PostProcessingEffectScheduler m_effectScheduler;

    private Camera m_camera = null!;

    private Material m_combineEyesMaterial = null!;

    private RenderTexture m_preEffectIntermediaryTexture = null;

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

        if (m_preEffectIntermediaryTexture == null)
            m_preEffectIntermediaryTexture = RenderTexture.GetTemporary(ImageDestinationTexture.descriptor);

        if (m_camera.stereoActiveEye == Camera.MonoOrStereoscopicEye.Left)
        {
            m_combineEyesMaterial.SetInt(s_eyeIndex, 0);
            Graphics.Blit(source, m_preEffectIntermediaryTexture, m_combineEyesMaterial);
            m_leftEyeRenderCount++;
        }
        else if (m_camera.stereoActiveEye == Camera.MonoOrStereoscopicEye.Right)
        {
            m_combineEyesMaterial.SetInt(s_eyeIndex, 1);
            Graphics.Blit(source, m_preEffectIntermediaryTexture, m_combineEyesMaterial);
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

            // render effects
            if (m_effectScheduler.isActiveAndEnabled)
                m_effectScheduler.Render(m_preEffectIntermediaryTexture, ImageDestinationTexture);
            else
                Graphics.Blit(m_preEffectIntermediaryTexture, ImageDestinationTexture);

            m_leftEyeRenderCount = 0;
            m_rightEyeRenderCount = 0;
            m_cameraCapture.DoFrameUpdate();

            // release temporary effect texture - apparently more efficient that re-using
            RenderTexture.ReleaseTemporary(m_preEffectIntermediaryTexture);
            m_preEffectIntermediaryTexture = null;
        }
    }
}