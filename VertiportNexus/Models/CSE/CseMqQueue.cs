namespace VertiportNexus.Models.Vertiport
{
    /// <summary>
    /// [CSE] [MQ] Queue 이름 정의
    /// 
    /// [GUIS] / [CSE] 간
    /// Request / Response Queue 이름을 정의한다.
    /// </summary>
    public static class CseMqQueue
    {
        #region [Request Queues]

        /// <summary>
        /// [GUIS -> CSE] 명령 요청 Queue
        /// 
        /// 탐지 / PTZ 제어 명령을 수신한다.
        /// </summary>
        public const string CommandRequest =
            "q.command.req";

        /// <summary>
        /// [GUIS -> CSE] 상태 조회 요청 Queue
        /// 
        /// 카메라 상태 조회 요청을 수신한다.
        /// </summary>
        public const string StatusRequest =
            "q.status.req";

        #endregion

        #region [Response Queues]

        /// <summary>
        /// [CSE -> GUIS] 명령 응답 Queue
        /// 
        /// 탐지 / PTZ 제어 명령 처리 결과를 송신한다.
        /// </summary>
        public const string CommandResponse =
            "q.command.res";

        /// <summary>
        /// [CSE -> GUIS] 상태 조회 응답 Queue
        /// 
        /// 카메라 상태 조회 결과를 송신한다.
        /// </summary>
        public const string StatusResponse =
            "q.status.res";

        #endregion
    }

}
