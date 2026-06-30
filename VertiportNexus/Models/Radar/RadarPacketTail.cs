namespace VertiportNexus.Models.Radar
{
    /// <summary>
    /// [Radar] Packet Tail
    /// 
    /// Packet 무결성 확인을 위한 Checksum과
    /// 종료 프레임 정보를 보관한다.
    /// </summary>
    public class RadarPacketTail
    {
        #region [Properties]

        /// <summary>
        /// Checksum
        /// 
        /// Header와 Tail을 제외한 SubData 전체 XOR 값이다.
        /// </summary>
        public byte Checksum { get; set; }

        /// <summary>
        /// 종료 프레임
        /// </summary>
        public byte EndFrame { get; set; }

        #endregion
    }

}
