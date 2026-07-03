namespace VertiportNexus.Models.Camera
{
    /// <summary>
    /// 내부 카메라 제어 명령 모델
    /// 
    /// [CSE] 명령을 장비 제어 서비스에서 사용할 수 있는
    /// 공통 카메라 제어 명령 형태로 변환한 결과이다.
    /// </summary>
    public class CameraCommand
    {
        #region [Command Properties]

        /// <summary>
        /// 내부 명령 종류
        /// </summary>
        public string CommandType { get; set; } =
            string.Empty;

        /// <summary>
        /// 제어 모드
        /// 
        /// 예)
        /// absolute, relative, continuous
        /// </summary>
        public string Mode { get; set; } =
            string.Empty;

        /// <summary>
        /// 제어 명령
        /// 
        /// [continuous] 모드에서
        /// Pan / Tilt 이동 방향 또는 정지 명령을 구분한다.
        /// </summary>
        public string Command { get; set; } =
            string.Empty;

        #endregion

        #region [Pan / Tilt Properties]

        /// <summary>
        /// [Pan] 제어값
        /// </summary>
        public double? Pan { get; set; }

        /// <summary>
        /// [Tilt] 제어값
        /// </summary>
        public double? Tilt { get; set; }

        #endregion

        #region [Zoom / Focus Properties]

        /// <summary>
        /// [Zoom] 배율값
        /// 
        /// 외부 명령 기준 [Zoom] 값은
        /// ADS1000 위치값이 아닌 실제 카메라 배율 기준으로 사용한다.
        /// 
        /// 예)
        /// 2.0  = 2배 Zoom
        /// 33.0 = 33배 Zoom
        /// 66.0 = 66배 Zoom
        /// </summary>
        public double? Zoom { get; set; }

        /// <summary>
        /// [Zoom] 위치값
        /// 
        /// ADS1000 장비 직접 제어용 [Zoom] 위치값이다.
        /// 
        /// 범위:
        /// 0 ~ 1000
        /// </summary>
        public double? ZoomPosition { get; set; }

        /// <summary>
        /// [Focus] 제어값
        /// </summary>
        public double? Focus { get; set; }

        #endregion

        #region [Source Properties]

        /// <summary>
        /// 원본 메시지 [ID]
        /// </summary>
        public string SourceMsgId { get; set; } =
            string.Empty;

        #endregion
    }

}
