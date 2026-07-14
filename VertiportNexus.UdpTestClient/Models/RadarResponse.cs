namespace VertiportNexus.UdpTestClient.Models
{
    internal sealed class RadarResponse
    {
        public uint PacketNumber { get; set; }

        public ushort TargetId { get; set; }

        public byte TrackResult { get; set; }

        public float Azimuth { get; set; }

        public float Elevation { get; set; }

        public string RecognitionInfo { get; set; }

        public bool IsChecksumValid { get; set; }
    }
}