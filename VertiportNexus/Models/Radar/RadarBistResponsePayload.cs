namespace VertiportNexus.Models.Radar
{
    /// <summary>
    /// [IF-CSE-CSR-002] Radar BIST Response Payload 모델
    /// 
    /// [CSE]에서 [CSR]로 전달하는 BIST 결과 정보를 보관한다.
    /// 
    /// 현재 구현 우선순위는 [IF-CSR-CSE-001] Tracking 연동이며,
    /// BIST는 기존 Parser / Builder / Mock Test 참조 구조 유지를 위해
    /// 모델을 보존한다.
    /// </summary>
    public class RadarBistResponsePayload
    {
        #region [Time Properties]

        /// <summary>
        /// BIST 응답 시간
        /// </summary>
        public long TimeStamp { get; set; }

        #endregion

        #region [BIST Result Properties]

        /// <summary>
        /// BIST 종류
        /// </summary>
        public byte BistType { get; set; }

        /// <summary>
        /// BIST 수신 / 처리 결과
        /// </summary>
        public byte RecvResult { get; set; }

        #endregion

        #region [Camera Properties]

        /// <summary>
        /// Camera 종류
        /// </summary>
        public uint CameraType { get; set; }

        #endregion

        #region [Position Properties]

        /// <summary>
        /// Camera 설치 위도
        /// </summary>
        public double Latitude { get; set; }

        /// <summary>
        /// Camera 설치 경도
        /// </summary>
        public double Longitude { get; set; }

        /// <summary>
        /// Camera 설치 고도
        /// </summary>
        public double Height { get; set; }

        #endregion

        #region [Attitude Properties]

        /// <summary>
        /// Camera 방위각
        /// </summary>
        public float Azimuth { get; set; }

        /// <summary>
        /// Camera Roll 값
        /// </summary>
        public float Roll { get; set; }

        /// <summary>
        /// Camera Pitch 값
        /// </summary>
        public float Pitch { get; set; }

        /// <summary>
        /// Camera Yaw 값
        /// </summary>
        public float Yaw { get; set; }

        #endregion

        #region [Reserved Properties]

        /// <summary>
        /// Reserved
        /// 
        /// ICD 확장 또는 정렬용 예비 필드이다.
        /// 현재 CSE 제어 로직에서는 사용하지 않는다.
        /// </summary>
        public uint Reserved { get; set; }

        #endregion
    }

}
