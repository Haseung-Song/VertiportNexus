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

        #region [PTZ Mode Types]

        /// <summary>
        /// [PTZ] Zoom 제어 모드
        /// </summary>
        public const string PtzModeZoom =
            "zoom";

        /// <summary>
        /// [PTZ] 연속 이동 모드
        /// </summary>
        public const string PtzModeContinuous =
            "continuous";

        /// <summary>
        /// [PTZ] 상대 이동 모드
        /// 
        /// 최신 ICD 허용값에는 없을 수 있으나,
        /// 기존 ICD 호환을 위해 유지한다.
        /// </summary>
        public const string PtzModeRelative =
            "relative";

        /// <summary>
        /// [PTZ] 절대 이동 모드
        /// </summary>
        public const string PtzModeAbsolute =
            "absolute";

        /// <summary>
        /// [PTZ] 자동 제어 모드
        /// </summary>
        public const string PtzModeAuto =
            "auto";

        /// <summary>
        /// [PTZ] 수동 제어 모드
        /// </summary>
        public const string PtzModeManual =
            "manual";

        #endregion

        #region [PTZ Command Values]

        /// <summary>
        /// [PTZ] 정지 명령
        /// </summary>
        public const string PtzCommandStop =
            "stop";

        /// <summary>
        /// [Pan] 좌측 이동 명령
        /// </summary>
        public const string PtzCommandLeft =
            "left";

        /// <summary>
        /// [Pan] 우측 이동 명령
        /// </summary>
        public const string PtzCommandRight =
            "right";

        /// <summary>
        /// [Tilt] 상향 이동 명령
        /// </summary>
        public const string PtzCommandUp =
            "up";

        /// <summary>
        /// [Tilt] 하향 이동 명령
        /// </summary>
        public const string PtzCommandDown =
            "down";

        /// <summary>
        /// [Pan / Tilt] 좌상향 이동 명령
        /// </summary>
        public const string PtzCommandLeftUp =
            "left_up";

        /// <summary>
        /// [Pan / Tilt] 우상향 이동 명령
        /// </summary>
        public const string PtzCommandRightUp =
            "right_up";

        /// <summary>
        /// [Pan / Tilt] 좌하향 이동 명령
        /// </summary>
        public const string PtzCommandLeftDown =
            "left_down";

        /// <summary>
        /// [Pan / Tilt] 우하향 이동 명령
        /// </summary>
        public const string PtzCommandRightDown =
            "right_down";

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
