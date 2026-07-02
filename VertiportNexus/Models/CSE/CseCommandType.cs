namespace VertiportNexus.Models.CSE
{
    /// <summary>
    /// [CSE] Command Type
    /// 
    /// [GUIS]에서 수신하는 JSON 메시지의
    /// [msg_type] 값을 정의한다.
    /// </summary>
    internal static class CseCommandType
    {
        #region [Detection Command Type]

        /// <summary>
        /// 탐지 요청
        /// </summary>
        public const string DetectOn =
            "detect_on";

        /// <summary>
        /// 탐지 해제 요청
        /// </summary>
        public const string DetectOff =
            "detect_off";

        /// <summary>
        /// 탐지 결과 연속 갱신
        /// 
        /// 탐지 중 약 [30Hz] 주기로 수신된다.
        /// </summary>
        public const string DetectConf =
            "detect_conf";

        #endregion

        #region [PTZ Command Type]

        /// <summary>
        /// PTZ 수동 제어 요청
        /// </summary>
        public const string PtzMove =
            "ptz_move";

        #endregion

        #region [Status Command Type]

        /// <summary>
        /// 카메라 상태 조회 요청
        /// 
        /// [q.status.req] Queue로 수신된다.
        /// </summary>
        public const string GetState =
            "get_state";

        /// <summary>
        /// 카메라 상태 조회 응답
        /// 
        /// [q.status.res] Queue로 송신된다.
        /// </summary>
        public const string GetStateResponse =
            "get_state_res";

        #endregion
    }

}
