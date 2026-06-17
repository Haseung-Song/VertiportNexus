
namespace VertiportNexus.Models.ADS1000
{
    /// <summary>
    /// [ADS1000] 수신 [Packet] 파싱 결과 모델
    /// </summary>
    public class Ads1000ParsedPacket
    {
        #region [Header]

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

        #region [Body]

        /// <summary>
        /// 수신 [Data]
        /// </summary>
        public byte[] Data { get; set; }

        /// <summary>
        /// 수신 [Checksum]
        /// </summary>
        public byte Checksum { get; set; }

        #endregion

        #region [Result]

        /// <summary>
        /// [Packet] 유효 여부
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// 파싱 결과 설명
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 오류 메시지
        /// </summary>
        public string ErrorMessage { get; set; }

        #endregion

        #region [Parsed Value]

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
