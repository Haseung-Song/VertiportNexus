namespace VertiportNexus.Models.Radar
{
    /// <summary>
    /// [Radar] Packet
    /// 
    /// Header / SubData / Tail로 구성된
    /// Radar 공통 Packet 모델이다.
    /// </summary>
    public class RadarPacket
    {
        #region [Properties]

        /// <summary>
        /// Packet Header
        /// </summary>
        public RadarPacketHeader Header { get; set; }

        /// <summary>
        /// 가변 길이 데이터 영역
        /// </summary>
        public byte[] SubData { get; set; }

        /// <summary>
        /// Packet Tail
        /// </summary>
        public RadarPacketTail Tail { get; set; }

        #endregion
    }

}
