namespace VertiportNexus.Models.Radar
{
    /// <summary>
    /// [Radar] Packet Header 모델
    /// 
    /// [CSR] ↔ [CSE] 간 Radar UDP Packet의
    /// 공통 Header 영역을 보관한다.
    /// </summary>
    public class RadarPacketHeader
    {
        #region [Frame Properties]

        /// <summary>
        /// 시작 프레임
        /// 
        /// Radar Packet 시작 여부를 판단하기 위한
        /// 고정 Frame 값이다.
        /// </summary>
        public byte StartFrame { get; set; }

        #endregion

        #region [ID Properties]

        /// <summary>
        /// 송신 시스템 ID
        /// </summary>
        public byte SendId { get; set; }

        /// <summary>
        /// 수신 시스템 ID
        /// </summary>
        public byte ReceiveId { get; set; }

        #endregion

        #region [Command Properties]

        /// <summary>
        /// Command 코드
        /// 
        /// Tracking Request / Tracking Response 등
        /// Radar Packet 종류를 구분한다.
        /// </summary>
        public byte Command { get; set; }

        #endregion

        #region [Packet Properties]

        /// <summary>
        /// Packet 번호
        /// 
        /// 송수신 Packet 식별 및 추적에 사용한다.
        /// </summary>
        public uint PacketNumber { get; set; }

        /// <summary>
        /// Packet 전체 길이
        /// 
        /// Header / SubData / Tail을 포함한
        /// 전체 Packet 길이를 나타낸다.
        /// </summary>
        public uint PacketLength { get; set; }

        #endregion
    }

}
