namespace VertiportNexus.Models.CSE
{
    /// <summary>
    /// [CSE] Interface ID
    /// 
    /// [GUIS] / [CSE] 간 JSON 메시지의
    /// Interface 식별값을 정의한다.
    /// </summary>
    internal static class CseInterfaceId
    {
        #region [GUIS -> CSE Interface IDs]

        /// <summary>
        /// [GUIS -> CSE] 탐지 요청
        /// </summary>
        public const string DetectOn =
            "IF-GUIS-CSE-001";

        /// <summary>
        /// [GUIS -> CSE] 탐지 해제 요청
        /// </summary>
        public const string DetectOff =
            "IF-GUIS-CSE-002";

        /// <summary>
        /// [GUIS -> CSE] 탐지 결과 연속 갱신
        /// 
        /// 탐지 상태에서 약 [30Hz] 주기로
        /// 최신 객체 화면 좌표를 수신한다.
        /// </summary>
        public const string DetectConf =
            "IF-GUIS-CSE-003";

        /// <summary>
        /// [GUIS -> CSE] [PTZ] 수동 제어 요청
        /// </summary>
        public const string PtzMove =
            "IF-GUIS-CSE-004";

        /// <summary>
        /// [GUIS -> CSE] 카메라 상태 조회 요청
        /// 
        /// [q.status.req] Queue로 수신된다.
        /// </summary>
        public const string GetState =
            "IF-GUIS-CSE-005";

        #endregion

        #region [CSE -> GUIS Interface IDs]

        /// <summary>
        /// [CSE -> GUIS] 탐지 요청 응답
        /// </summary>
        public const string DetectOnResponse =
            "IF-CSE-GUIS-001";

        /// <summary>
        /// [CSE -> GUIS] 탐지 해제 응답
        /// </summary>
        public const string DetectOffResponse =
            "IF-CSE-GUIS-002";

        /// <summary>
        /// [CSE -> GUIS] 탐지 결과 연속 갱신 응답
        /// </summary>
        public const string DetectConfResponse =
            "IF-CSE-GUIS-003";

        /// <summary>
        /// [CSE -> GUIS] [PTZ] 수동 제어 요청 응답
        /// </summary>
        public const string PtzMoveResponse =
            "IF-CSE-GUIS-004";

        /// <summary>
        /// [CSE -> GUIS] 카메라 상태 조회 응답
        /// 
        /// [q.status.res] Queue로 송신된다.
        /// </summary>
        public const string GetStateResponse =
            "IF-CSE-GUIS-005";

        #endregion
    }

}
