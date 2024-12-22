namespace FFmpegOut.LiveStream
{
    public enum StreamPreset
    {
        UDP,
        RTP,
        RTSP,
        HLS,
        HLS_SSEGMENT,
        RTMP
    }

    public static class StreamPresetExtensions
    {
        public static string GetOptions(this StreamPreset preset)
        {
            switch (preset)
            {
                case StreamPreset.UDP:
                    return "-f mpegts";
                case StreamPreset.RTP:
                    return "-f rtp_mpegts";
                case StreamPreset.RTSP:
                    return "-f rtsp";
                case StreamPreset.HLS:
                    return "-f hls -hls_flags delete_segments -hls_init_time 0.5 -hls_time 0.5 -hls_list_size 10 -hls_allow_cache 1 -hls_base_url";
                case StreamPreset.HLS_SSEGMENT:
                    return "-f segment -segment_list_type m3u8 -segment_list_size 10 -segment_list_flags +live -segment_time 1 -segment_wrap 10 -segment_list_entry_prefix";
                case StreamPreset.RTMP:
                    return "-f flv";
            }

            return null;
        }
    }
}
