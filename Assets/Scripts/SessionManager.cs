using System;
using System.Threading.Tasks;
using FFmpegOut;
using FFmpegOut.LiveStream;
using Network;
using UnityEngine;

public class SessionManager : MonoBehaviour
{
    public bool IsStreaming => m_eyeBlitter.ImageDestinationTexture != null;

    [SerializeField]
    private StreamCameraCapture m_cameraCapture;

    [SerializeField]
    private RenderToStreamer m_eyeBlitter;

    private RenderTexture m_renderTexture;

    private ServerSessionCommunicator m_session;

    private async void Start()
    {
        // starting server before port forwarding is more conventient for testing
        m_session = new ServerSessionCommunicator(this);
        bool serverStarted = m_session.StartServer(9944);
        if (!serverStarted)
        {
            Debug.LogError("Failed to start server. Exiting");
            Application.Quit();
            return;
        }

        // try and reverse forward ports
        // no guarantee that ports will be forwarded
        // no guarantee that android device is connected
        // no guarantee that adb is installed and in PATH
        await Task.WhenAll(
            AdbManager.ForwardPort(9943),
            AdbManager.ForwardPort(9944));

        // wait for connection
        await m_session.AcceptOneConnectionAsync();
    }

    public void BeginStream(int width, int height, float framerate)
    {
        if (IsStreaming)
            Debug.LogError("Stream requested while one is ongoing.");

        Application.targetFrameRate = (int)framerate;
        QualitySettings.vSyncCount = 0;

        Debug.Log($"Beginning stream {width}x{height} @{framerate:N2}fps");

        // set up new render texture (camera - RT - ffmpeg)
        // destroy render texture if it still exists
        if (m_renderTexture != null) Destroy(m_renderTexture);
        // depth buffer of 24? had that in CameraCapture
        m_renderTexture = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);

        m_eyeBlitter.ImageDestinationTexture = m_renderTexture;
        m_cameraCapture.InitSession(width, height, framerate, m_renderTexture);
    }

    public void EndStream()
    {
        m_cameraCapture.EndSession();
    }
}