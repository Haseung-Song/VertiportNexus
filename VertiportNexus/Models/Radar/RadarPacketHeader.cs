namespace VertiportNexus.Models.Radar
{
    /// <summary>
    /// [Radar] Packet Header
    /// 
    /// [CSR] ↔ [CSE] Packet의 공통 Header 정보를 보관한다.
    /// </summary>
    public class RadarPacketHeader
    {
        #region [Properties]

        /// <summary>
        /// 시작 프레임
        /// </summary>
        public byte StartFrame { get; set; }

        /// <summary>
        /// 송신 ID
        /// </summary>
        public byte SendId { get; set; }

        /// <summary>
        /// 수신 ID
        /// </summary>
        public byte ReceiveId { get; set; }

        /// <summary>
        /// 명령 코드
        /// </summary>
        public byte Command { get; set; }

        /// <summary>
        /// Packet 번호
        /// </summary>
        public uint PacketNumber { get; set; }

        /// <summary>
        /// Packet 전체 길이
        /// </summary>
        public uint PacketLength { get; set; }

        #endregion
    }

}
