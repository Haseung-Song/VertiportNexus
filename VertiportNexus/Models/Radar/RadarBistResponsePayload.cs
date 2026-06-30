namespace VertiportNexus.Models.Radar
{
    /// <summary>
    /// [IF-CSE-CSR-002] BIST 결과 Payload
    /// 
    /// CSE에서 Radar로 전달하는 BIST 결과 정보를 보관한다.
    /// </summary>
    public class RadarBistResponsePayload
    {
        #region [Properties]

        public long TimeStamp { get; set; }

        public byte BistType { get; set; }

        public byte RecvResult { get; set; }

        public uint CameraType { get; set; }

        public double Latitude { get; set; }

        public double Longitude { get; set; }

        public double Height { get; set; }

        public float Azimuth { get; set; }

        public float Roll { get; set; }

        public float Pitch { get; set; }

        public float Yaw { get; set; }

        public uint Reserved { get; set; }

        #endregion
    }

}
