// FFmpegOut - FFmpeg video encoding plugin for Unity
// https://github.com/keijiro/KlakNDI

namespace FFmpegOut
{
    public enum FFmpegPreset
    {
        H_264DEFAULT,
        H264_NVIDIA,
        H264_LOSSLESS420,
        H264_LOSSLESS444,
        HEVC_DEFAULT,
        HEVC_NVIDIA,
        PRO_RES422,
        PRO_RES4444,
        VP_8DEFAULT,
        VP_9DEFAULT,
        HAP,
        HAP_ALPHA,
        HAP_Q
    }

    static public class FFmpegPresetExtensions
    {
        public static string GetDisplayName(this FFmpegPreset preset)
        {
            switch (preset)
            {
                case FFmpegPreset.H_264DEFAULT:     return "H.264 Default (MP4)";
                case FFmpegPreset.H264_NVIDIA:      return "H.264 NVIDIA (MP4)";
                case FFmpegPreset.H264_LOSSLESS420: return "H.264 Lossless 420 (MP4)";
                case FFmpegPreset.H264_LOSSLESS444: return "H.264 Lossless 444 (MP4)";
                case FFmpegPreset.HEVC_DEFAULT:     return "HEVC Default (MP4)";
                case FFmpegPreset.HEVC_NVIDIA:      return "HEVC NVIDIA (MP4)";
                case FFmpegPreset.PRO_RES422:       return "ProRes 422 (QuickTime)";
                case FFmpegPreset.PRO_RES4444:      return "ProRes 4444 (QuickTime)";
                case FFmpegPreset.VP_8DEFAULT:      return "VP8 (WebM)";
                case FFmpegPreset.VP_9DEFAULT:      return "VP9 (WebM)";
                case FFmpegPreset.HAP:             return "HAP (QuickTime)";
                case FFmpegPreset.HAP_ALPHA:        return "HAP Alpha (QuickTime)";
                case FFmpegPreset.HAP_Q:            return "HAP Q (QuickTime)";
            }
            return null;
        }

        public static string GetSuffix(this FFmpegPreset preset)
        {
            switch (preset)
            {
                case FFmpegPreset.H_264DEFAULT:
                case FFmpegPreset.H264_NVIDIA:
                case FFmpegPreset.H264_LOSSLESS420:
                case FFmpegPreset.H264_LOSSLESS444:
                case FFmpegPreset.HEVC_DEFAULT:
                case FFmpegPreset.HEVC_NVIDIA:      return ".mp4";
                case FFmpegPreset.PRO_RES422:
                case FFmpegPreset.PRO_RES4444:      return ".mov";
                case FFmpegPreset.VP_9DEFAULT:
                case FFmpegPreset.VP_8DEFAULT:      return ".webm";
                case FFmpegPreset.HAP:
                case FFmpegPreset.HAP_Q:
                case FFmpegPreset.HAP_ALPHA:        return ".mov";
            }
            return null;
        }

        public static string GetOptions(this FFmpegPreset preset)
        {
            switch (preset)
            {
                case FFmpegPreset.H_264DEFAULT:     return "-pix_fmt yuv420p";
                case FFmpegPreset.H264_NVIDIA:      return "-c:v h264_nvenc -pix_fmt yuv420p";
                case FFmpegPreset.H264_LOSSLESS420: return "-pix_fmt yuv420p -preset ultrafast -crf 0";
                case FFmpegPreset.H264_LOSSLESS444: return "-pix_fmt yuv444p -preset ultrafast -crf 0";
                case FFmpegPreset.HEVC_DEFAULT:     return "-c:v libx265 -pix_fmt yuv420p";
                case FFmpegPreset.HEVC_NVIDIA:      return "-c:v hevc_nvenc -pix_fmt yuv420p";
                case FFmpegPreset.PRO_RES422:       return "-c:v prores_ks -pix_fmt yuv422p10le";
                case FFmpegPreset.PRO_RES4444:      return "-c:v prores_ks -pix_fmt yuva444p10le";
                case FFmpegPreset.VP_8DEFAULT:      return "-c:v libvpx -pix_fmt yuv420p";
                case FFmpegPreset.VP_9DEFAULT:      return "-c:v libvpx-vp9";
                case FFmpegPreset.HAP:             return "-c:v hap";
                case FFmpegPreset.HAP_ALPHA:        return "-c:v hap -format hap_alpha";
                case FFmpegPreset.HAP_Q:            return "-c:v hap -format hap_q";
            }
            return null;
        }
    }
}
