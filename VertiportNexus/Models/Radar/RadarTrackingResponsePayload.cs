namespace VertiportNexus.Models.Radar
{
    /// <summary>
    /// [IF-CSE-CSR-001] Radar Tracking Response Payload 모델
    /// 
    /// [CSE]에서 [CSR]로 전달하는
    /// Radar Tracking 요청 처리 결과 정보를 보관한다.
    /// </summary>
    public class RadarTrackingResponsePayload
    {
        #region [Time Properties]

        /// <summary>
        /// Radar Tracking 응답 시간
        /// </summary>
        public long TimeStamp { get; set; }

        #endregion

        #region [Tracking Result Properties]

        /// <summary>
        /// 추적 대상 ID
        /// 
        /// Radar Tracking Request에서 수신한 대상 ID를
        /// 응답 Packet에 반영한다.
        /// </summary>
        public ushort Id { get; set; }

        /// <summary>
        /// 추적 처리 결과
        /// 
        /// CSE의 추적 요청 수신 / 처리 결과를
        /// Radar 측으로 반환하기 위한 값이다.
        /// </summary>
        public byte TrackResult { get; set; }

        #endregion

        #region [Angle Properties]

        /// <summary>
        /// 응답 방위각
        /// 
        /// CSE가 처리한 추적 기준 Pan 방향 정보를 반환한다.
        /// </summary>
        public float Azimuth { get; set; }

        /// <summary>
        /// 응답 고각
        /// 
        /// CSE가 처리한 추적 기준 Tilt 방향 정보를 반환한다.
        /// </summary>
        public float Elevation { get; set; }

        #endregion

        #region [Recognition Properties]

        /// <summary>
        /// 인식 정보
        /// 
        /// Radar ICD의 RecognitionInfo 영역에 대응하는 문자열이다.
        /// 고정 길이 Packet 처리 시 Builder에서 Padding / Truncate 기준으로 사용한다.
        /// </summary>
        public string RecognitionInfo { get; set; } =
            string.Empty;

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
