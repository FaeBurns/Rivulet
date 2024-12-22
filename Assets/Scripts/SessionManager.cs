using System;
using System.Threading.Tasks;
using FFmpegOut;
using FFmpegOut.LiveStream;
using Network;
using UnityEngine;

public class SessionManager : MonoBehaviour
{
    public bool IsStreaming => m_cameraCapture.enabled;

    [SerializeField]
    private StreamCameraCapture m_cameraCapture;

    private async void Start()
    {
        // starting server before port forwarding is more conventient for testing
        ServerSessionCommunicator sessionCommunicator = new ServerSessionCommunicator(this);
        sessionCommunicator.StartServer(9944);

        // try and reverse forward ports
        // no guarantee that ports will be forwarded
        // no guarantee that android device is connected
        // no guarantee that adb is installed and in PATH
        await Task.WhenAll(
            AdbManager.ForwardPort(9943),
            AdbManager.ForwardPort(9944));

        // wait for connection
        await sessionCommunicator.AcceptOneConnectionAsync();
    }

    public void BeginStream(int width, int height, float framerate)
    {
        if (IsStreaming)
            Debug.LogError("Stream requested while one is ongoing.");

        Application.targetFrameRate = (int)framerate;
        QualitySettings.vSyncCount = 0;

        Debug.Log($"Beginning stream {width}x{height} @{framerate:N2}fps");

        m_cameraCapture.Width = width;
        m_cameraCapture.Height = height;
        m_cameraCapture.FrameRate = framerate;
        m_cameraCapture.enabled = true;
    }

    public void EndStream()
    {
        m_cameraCapture.enabled = false;
    }
}