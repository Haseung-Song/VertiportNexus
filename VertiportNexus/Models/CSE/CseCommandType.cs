namespace VertiportNexus.Models.Vertiport
{
    /// <summary>
    /// [CSE] 명령 메시지 종류
    /// 
    /// [ICD] 기준 [msg_type] 문자열을
    /// 코드에서 구분하기 위한 상수 클래스이다.
    /// </summary>
    public static class CseCommandType
    {
        #region [Detect Command Types]

        /// <summary>
        /// 탐지 활성화 요청
        /// </summary>
        public const string DetectEnable =
            "detect_enable";

        /// <summary>
        /// 탐지 활성화 취소 요청
        /// </summary>
        public const string DetectDisable =
            "detect_disable";

        /// <summary>
        /// 탐지 요청
        /// </summary>
        public const string DetectOn =
            "detect_on";

        /// <summary>
        /// 탐지 정지 요청
        /// </summary>
        public const string DetectOff =
            "detect_off";

        /// <summary>
        /// 탐지 계속 요청
        /// </summary>
        public const string DetectContinue =
            "detect_cont";

        #endregion

        #region [PTZ Command Types]

        /// <summary>
        /// [PTZ] 제어 요청
        /// </summary>
        public const string PtzMove =
            "ptz_move";

        /// <summary>
        /// [PTZ] 제어 정지 요청
        /// </summary>
        public const string PtzStop =
            "ptz_stop";

        /// <summary>
        /// [PTZ] 제어 모드 설정
        /// </summary>
        public const string PtzMode =
            "ptz_mode";

        #endregion

        #region [Image Command Types]

        /// <summary>
        /// 영상 설정 요청
        /// </summary>
        public const string SetImage =
            "set_image";

        /// <summary>
        /// 영상 반전 설정 요청
        /// </summary>
        public const string SetFlip =
            "set_flip";

        #endregion

        #region [Status Command Types]

        /// <summary>
        /// 카메라 설정 조회 요청
        /// </summary>
        public const string GetConfig =
            "get_conf";

        /// <summary>
        /// 카메라 상태 조회 요청
        /// </summary>
        public const string GetState =
            "get_state";

        #endregion
    }

}
