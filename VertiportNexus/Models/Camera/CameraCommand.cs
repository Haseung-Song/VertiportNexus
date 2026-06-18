namespace VertiportNexus.Models.Camera
{
    /// <summary>
    /// 내부 카메라 제어 명령 모델
    /// 
    /// [CSE] [JSON] 명령을 장비 제어 서비스에서 사용할 수 있는
    /// 공통 명령 형태로 변환한 결과이다.
    /// </summary>
    public class CameraCommand
    {
        #region [Properties]

        /// <summary>
        /// 내부 명령 종류
        /// </summary>
        public string CommandType { get; set; }

        /// <summary>
        /// 제어 모드
        /// 
        /// 예)
        /// absolute, relative, continuous
        /// </summary>
        public string Mode { get; set; }

        /// <summary>
        /// [Pan] 값
        /// </summary>
        public double? Pan { get; set; }

        /// <summary>
        /// [Tilt] 값
        /// </summary>
        public double? Tilt { get; set; }

        /// <summary>
        /// [Zoom] 값
        /// </summary>
        public double? Zoom { get; set; }

        /// <summary>
        /// [Focus] 값
        /// </summary>
        public double? Focus { get; set; }

        /// <summary>
        /// 원본 메시지 [ID]
        /// </summary>
        public string SourceMsgId { get; set; }

        #endregion
    }

}
