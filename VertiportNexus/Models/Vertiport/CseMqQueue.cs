namespace VertiportNexus.Models.Vertiport
{
    /// <summary>
    /// [CSE] [MQ] Queue 이름 정의
    /// 
    /// ICD 문서 기준
    /// [GUIS] / [CSE] 간 Request / Response Queue를 정의한다.
    /// </summary>
    public static class CseMqQueue
    {
        /// <summary>
        /// [GUIS] → [CSE] 명령 요청 Queue
        /// 
        /// 카메라 제어 명령을 수신한다.
        /// </summary>
        public const string CommandRequest =
            "q.command.req";

        /// <summary>
        /// [CSE] → [GUIS] 명령 응답 Queue
        /// 
        /// 카메라 제어 명령 처리 결과를 송신한다.
        /// </summary>
        public const string CommandResponse =
            "q.command.res";

        /// <summary>
        /// [GUIS] → [CSE] 상태 조회 요청 Queue
        /// 
        /// 카메라 상태 조회 요청을 수신한다.
        /// </summary>
        public const string StatusRequest =
            "q.status.req";

        /// <summary>
        /// [CSE] → [GUIS] 상태 조회 응답 Queue
        /// 
        /// 카메라 상태 조회 결과를 송신한다.
        /// </summary>
        public const string StatusResponse =
            "q.status.res";
    }

}
