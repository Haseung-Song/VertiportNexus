namespace VertiportNexus.Models.ADS1000
{
    /// <summary>
    /// [ADS1000] 수신 [Packet] 파싱 결과 모델
    /// 
    /// [MCB] / [SCB]에서 수신한 Packet의 Header / Body / Checksum 정보와
    /// Pan / Tilt / Zoom / Focus 상태값 파싱 결과를 보관한다.
    /// </summary>
    public class Ads1000ParsedPacket
    {
        #region [Header Properties]

        /// <summary>
        /// 첫 번째 [Sync]
        /// </summary>
        public byte Sync0 { get; set; }

        /// <summary>
        /// 두 번째 [Sync]
        /// </summary>
        public byte Sync1 { get; set; }

        /// <summary>
        /// 명령 / 응답 구분 코드
        /// </summary>
        public byte Cmd1 { get; set; }

        /// <summary>
        /// [Data] 길이
        /// </summary>
        public int Length { get; set; }

        #endregion

        #region [Body Properties]

        /// <summary>
        /// 수신 [Data]
        /// </summary>
        public byte[] Data { get; set; } =
            new byte[0];

        /// <summary>
        /// 수신 [Checksum]
        /// </summary>
        public byte Checksum { get; set; }

        #endregion

        #region [Result Properties]

        /// <summary>
        /// [Packet] 유효 여부
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// 파싱 결과 설명
        /// </summary>
        public string Description { get; set; } =
            string.Empty;

        /// <summary>
        /// 오류 메시지
        /// </summary>
        public string ErrorMessage { get; set; } =
            string.Empty;

        #endregion

        #region [Value Flag Properties]

        /// <summary>
        /// [Pan] 값 포함 여부
        /// </summary>
        public bool HasPanValue { get; set; }

        /// <summary>
        /// [Tilt] 값 포함 여부
        /// </summary>
        public bool HasTiltValue { get; set; }

        /// <summary>
        /// [Zoom] 값 포함 여부
        /// </summary>
        public bool HasZoomValue { get; set; }

        /// <summary>
        /// [Focus] 값 포함 여부
        /// </summary>
        public bool HasFocusValue { get; set; }

        #endregion

        #region [Parsed Value Properties]

        /// <summary>
        /// [Pan] 현재 값
        /// </summary>
        public double PanValue { get; set; }

        /// <summary>
        /// [Tilt] 현재 값
        /// </summary>
        public double TiltValue { get; set; }

        /// <summary>
        /// [Zoom] 현재 값
        /// </summary>
        public double ZoomValue { get; set; }

        /// <summary>
        /// [Focus] 현재 값
        /// </summary>
        public double FocusValue { get; set; }

        #endregion
    }

}
