namespace VertiportNexus.Models.Radar
{
    /// <summary>
    /// [IF-CSR-CSE-002] Radar BIST Request Payload 모델
    /// 
    /// [CSR]에서 [CSE]로 전달하는 BIST 요청 정보를 보관한다.
    /// 
    /// 현재 구현 우선순위는 [IF-CSR-CSE-001] Tracking 연동이며,
    /// BIST는 기존 Parser / Builder / Mock Test 참조 구조 유지를 위해
    /// 모델을 보존한다.
    /// </summary>
    public class RadarBistRequestPayload
    {
        #region [Time Properties]

        /// <summary>
        /// BIST 요청 시간
        /// </summary>
        public long TimeStamp { get; set; }

        #endregion

        #region [BIST Properties]

        /// <summary>
        /// BIST 종류
        /// </summary>
        public byte BistType { get; set; }

        /// <summary>
        /// 통신 Port 번호
        /// </summary>
        public uint ComportNumber { get; set; }

        /// <summary>
        /// CBIST 수행 주기
        /// </summary>
        public uint CbistInterval { get; set; }

        #endregion
    }

}
