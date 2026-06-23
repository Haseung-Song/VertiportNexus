namespace VertiportNexus.Models.Camera
{
    /// <summary>
    /// 내부 카메라 명령 종류
    /// </summary>
    public static class CameraCommandType
    {
        #region [Command Types]

        public const string PtzMove =
            "PtzMove";

        public const string PtzStop =
            "PtzStop";

        public const string PtzMode =
            "PtzMode";

        public const string ZoomMove =
            "ZoomMove";

        public const string FocusMove =
            "FocusMove";

        public const string GetState =
            "GetState";

        #endregion
    }

}
