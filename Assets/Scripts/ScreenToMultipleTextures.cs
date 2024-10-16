using UnityEngine;

[RequireComponent(typeof(Camera))]
public class ScreenToMultipleRenderTextures : MonoBehaviour
{
    [SerializeField]
    private RenderTexture m_leftEyeTexture;

    [SerializeField]
    private RenderTexture m_rightEyeTexture;

    private Camera m_camera = null!;

    private void Awake()
    {
        m_camera = GetComponent<Camera>();
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (m_camera.stereoActiveEye == Camera.MonoOrStereoscopicEye.Left)
            Graphics.Blit(source, m_leftEyeTexture);
        else if (m_camera.stereoActiveEye == Camera.MonoOrStereoscopicEye.Right)
            Graphics.Blit(source, m_rightEyeTexture);
        else
            Debug.LogError("Invalid active eye");

        Graphics.Blit(source, destination);
    }
}