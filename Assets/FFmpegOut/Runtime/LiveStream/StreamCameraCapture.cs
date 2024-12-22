using UnityEngine;
using UnityEngine.Serialization;

namespace FFmpegOut.LiveStream
{
    public class StreamCameraCapture : CameraCapture
    {
        [FormerlySerializedAs("_streamPreset")]
        [SerializeField] protected StreamPreset StreamPreset;
        [FormerlySerializedAs("_streamAddress")]
        [SerializeField] protected string StreamAddress;

        protected override FFmpegSession GetSession(int texWidth, int texHeight, float frameRate)
        {
            return StreamFFmpegSession.Create(
                texWidth,
                texHeight,
                frameRate,
                Preset,
                StreamPreset,
                StreamAddress);
        }
    }
}
