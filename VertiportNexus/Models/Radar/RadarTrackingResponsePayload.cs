namespace VertiportNexus.Models.Radar
{
    /// <summary>
    /// [IF-CSE-CSR-001] 추적 결과 Payload
    /// 
    /// CSE에서 Radar로 전달하는 추적 수행 결과 정보를 보관한다.
    /// </summary>
    public class RadarTrackingResponsePayload
    {
        #region [Properties]

        public long TimeStamp { get; set; }

        public ushort Id { get; set; }

        public byte TrackResult { get; set; }

        public float Azimuth { get; set; }

        public float Elevation { get; set; }

        public string RecognitionInfo { get; set; }

        public uint Reserved { get; set; }

        #endregion
    }

}
