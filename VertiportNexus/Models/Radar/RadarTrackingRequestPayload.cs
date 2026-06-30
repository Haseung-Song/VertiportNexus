namespace VertiportNexus.Models.Radar
{
    /// <summary>
    /// [IF-CSR-CSE-001] 추적 요청 Payload
    /// 
    /// Radar에서 CSE로 전달하는 선택 표적 정보를 보관한다.
    /// </summary>
    public class RadarTrackingRequestPayload
    {
        #region [Properties]

        public long TimeStamp { get; set; }

        public byte PtMove { get; set; }

        public ushort Id { get; set; }

        public float Azimuth { get; set; }

        public float Elevation { get; set; }

        public float Distance { get; set; }

        public float Vx { get; set; }

        public float Vy { get; set; }

        public float Vz { get; set; }

        public double EcefX { get; set; }

        public double EcefY { get; set; }

        public double EcefZ { get; set; }

        public uint Reserved { get; set; }

        #endregion
    }

}
