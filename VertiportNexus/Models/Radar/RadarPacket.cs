namespace VertiportNexus.Models.Radar
{
    /// <summary>
    /// [Radar] 공통 Packet 모델
    /// 
    /// Header / SubData / Tail 구조로 구성된
    /// Radar UDP 송수신 Packet을 표현한다.
    /// </summary>
    public class RadarPacket
    {
        #region [Header Properties]

        /// <summary>
        /// Packet Header
        /// 
        /// 송신 ID / 수신 ID / Command / Packet 길이 등
        /// 공통 Header 정보를 보관한다.
        /// </summary>
        public RadarPacketHeader Header { get; set; } =
            new RadarPacketHeader();

        #endregion

        #region [Body Properties]

        /// <summary>
        /// 가변 길이 데이터 영역
        /// 
        /// Tracking Request / Tracking Response 등
        /// Command별 Payload 직렬화 데이터를 보관한다.
        /// </summary>
        public byte[] SubData { get; set; } =
            new byte[0];

        #endregion

        #region [Tail Properties]

        /// <summary>
        /// Packet Tail
        /// 
        /// Checksum 및 종료 Frame 정보를 보관한다.
        /// </summary>
        public RadarPacketTail Tail { get; set; } =
            new RadarPacketTail();

        #endregion
    }

}
