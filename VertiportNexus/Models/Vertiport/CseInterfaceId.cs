namespace VertiportNexus.Models.Vertiport
{
    /// <summary>
    /// [CSE] 인터페이스 ID
    /// 
    /// ICD 문서 기준
    /// [IF-GUIS-CSE-001] ~ [IF-GUIS-CSE-012] 명령을 구분한다.
    /// </summary>
    public static class CseInterfaceId
    {
        /// <summary>
        /// 카메라 제어 - 탐지 활성화
        /// </summary>
        public const string DetectEnable =
            "IF-GUIS-CSE-001";

        /// <summary>
        /// 카메라 제어 - 탐지 활성화 취소
        /// </summary>
        public const string DetectDisable =
            "IF-GUIS-CSE-002";

        /// <summary>
        /// 카메라 제어 - 탐지
        /// </summary>
        public const string DetectOn =
            "IF-GUIS-CSE-003";

        /// <summary>
        /// 카메라 제어 - 탐지 해제
        /// </summary>
        public const string DetectOff =
            "IF-GUIS-CSE-004";

        /// <summary>
        /// 카메라 제어 - 탐지 계속
        /// </summary>
        public const string DetectContinue =
            "IF-GUIS-CSE-005";

        /// <summary>
        /// 카메라 제어 - PTZ 제어
        /// </summary>
        public const string PtzMove =
            "IF-GUIS-CSE-006";

        /// <summary>
        /// 카메라 제어 - PTZ 제어 해제
        /// </summary>
        public const string PtzStop =
            "IF-GUIS-CSE-007";

        /// <summary>
        /// 카메라 제어 - PTZ 제어 모드
        /// </summary>
        public const string PtzMode =
            "IF-GUIS-CSE-008";

        /// <summary>
        /// 카메라 제어 - 영상 설정
        /// </summary>
        public const string SetImage =
            "IF-GUIS-CSE-009";

        /// <summary>
        /// 카메라 제어 - 영상 플립
        /// </summary>
        public const string SetFlip =
            "IF-GUIS-CSE-010";

        /// <summary>
        /// 카메라 상태 - 설정 조회
        /// </summary>
        public const string GetConfig =
            "IF-GUIS-CSE-011";

        /// <summary>
        /// 카메라 상태 - PTZ 조회
        /// </summary>
        public const string GetPtzState =
            "IF-GUIS-CSE-012";
    }

}
