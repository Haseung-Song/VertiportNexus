namespace VertiportNexus.Models.Radar
{
    /// <summary>
    /// [Radar] Packet Tail 모델
    /// 
    /// Radar Packet의 Checksum 및
    /// 종료 Frame 정보를 보관한다.
    /// </summary>
    public class RadarPacketTail
    {
        #region [Checksum Properties]

        /// <summary>
        /// Checksum
        /// 
        /// Packet 무결성 검증에 사용하는 값이다.
        /// 현재 Radar Packet 구조에서는 Header / Tail을 제외한
        /// SubData 기준 XOR 값으로 처리한다.
        /// </summary>
        public byte Checksum { get; set; }

        #endregion

        #region [Frame Properties]

        /// <summary>
        /// 종료 프레임
        /// 
        /// Radar Packet 종료 여부를 판단하기 위한
        /// 고정 Frame 값이다.
        /// </summary>
        public byte EndFrame { get; set; }

        #endregion
    }

}
