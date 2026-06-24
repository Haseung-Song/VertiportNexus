namespace VertiportNexus.Models.Camera
{
    /// <summary>
    /// 내부 카메라 명령 종류
    /// 
    /// [CSE] 명령을 장비 제어 서비스에서 사용할 수 있는
    /// 내부 명령 문자열로 구분한다.
    /// </summary>
    public static class CameraCommandType
    {
        #region [PTZ Command Types]

        /// <summary>
        /// [PTZ] 이동 명령
        /// </summary>
        public const string PtzMove =
            "PtzMove";

        /// <summary>
        /// [PTZ] 정지 명령
        /// </summary>
        public const string PtzStop =
            "PtzStop";

        /// <summary>
        /// [PTZ] 제어 모드 설정 명령
        /// </summary>
        public const string PtzMode =
            "PtzMode";

        #endregion

        #region [Zoom / Focus Command Types]

        /// <summary>
        /// [Zoom] 이동 명령
        /// </summary>
        public const string ZoomMove =
            "ZoomMove";

        /// <summary>
        /// [Focus] 이동 명령
        /// </summary>
        public const string FocusMove =
            "FocusMove";

        #endregion

        #region [Status Command Types]

        /// <summary>
        /// 카메라 상태 조회 명령
        /// </summary>
        public const string GetState =
            "GetState";

        #endregion
    }

}
