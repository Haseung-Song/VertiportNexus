namespace VertiportNexus.Models.ADS1000
{
    /// <summary>
    /// [ADS1000] 상태 [Packet] 처리 결과
    /// 
    /// [ADS1000] 수신 [Packet] 파싱 결과와
    /// 화면 표시용 상태 문자열을 보관한다.
    /// </summary>
    public class Ads1000StatusResult
    {
        #region [Properties]

        /// <summary>
        /// 수신 장비 이름
        /// </summary>
        public string DeviceName { get; }

        /// <summary>
        /// 수신 [Packet] [HEX] 문자열
        /// </summary>
        public string PacketText { get; }

        /// <summary>
        /// 파싱 성공 여부
        /// </summary>
        public bool IsValid { get; }

        /// <summary>
        /// [ADS1000] 파싱 [Packet]
        /// </summary>
        public Ads1000ParsedPacket ParsedPacket { get; }

        /// <summary>
        /// 화면 표시용 [TCP] 상태 문자열
        /// </summary>
        public string StatusText { get; }

        /// <summary>
        /// 화면 표시용 [Camera] 상태 문자열
        /// </summary>
        public string CameraStatusText { get; }

        #endregion

        #region [Constructor]

        /// <summary>
        /// [Ads1000StatusResult] 생성자
        /// </summary>
        private Ads1000StatusResult(
            string deviceName,
            string packetText,
            bool isValid,
            Ads1000ParsedPacket parsedPacket,
            string statusText,
            string cameraStatusText)
        {
            DeviceName =
                deviceName ?? string.Empty;

            PacketText =
                packetText ?? string.Empty;

            IsValid =
                isValid;

            ParsedPacket =
                parsedPacket;

            StatusText =
                statusText ?? string.Empty;

            CameraStatusText =
                cameraStatusText ?? string.Empty;
        }

        #endregion

        #region [Factory Methods]

        /// <summary>
        /// 정상 파싱 결과 생성
        /// 
        /// [Checksum] 검증 및 [Packet] 파싱이 모두 성공한 경우 사용한다.
        /// </summary>
        /// <param name="deviceName">
        /// 수신 장비 이름
        /// </param>
        /// <param name="packetText">
        /// 수신 [Packet] [HEX] 문자열
        /// </param>
        /// <param name="parsedPacket">
        /// [ADS1000] 파싱 [Packet]
        /// </param>
        /// <returns>
        /// 정상 파싱 처리 결과
        /// </returns>
        public static Ads1000StatusResult CreateValid(
            string deviceName,
            string packetText,
            Ads1000ParsedPacket parsedPacket)
        {
            string cameraStatusText =
                parsedPacket == null
                    ? string.Empty
                    : parsedPacket.Description;

            return new Ads1000StatusResult(
                deviceName,
                packetText,
                true,
                parsedPacket,
                deviceName + " Packet Parsed",
                cameraStatusText);
        }

        /// <summary>
        /// 비정상 파싱 결과 생성
        /// 
        /// [Packet] 검증 또는 파싱 과정에서 오류가 발생한 경우 사용한다.
        /// </summary>
        /// <param name="deviceName">
        /// 수신 장비 이름
        /// </param>
        /// <param name="packetText">
        /// 수신 [Packet] [HEX] 문자열
        /// </param>
        /// <param name="parsedPacket">
        /// [ADS1000] 파싱 [Packet]
        /// </param>
        /// <returns>
        /// 비정상 파싱 처리 결과
        /// </returns>
        public static Ads1000StatusResult CreateInvalid(
            string deviceName,
            string packetText,
            Ads1000ParsedPacket parsedPacket)
        {
            return new Ads1000StatusResult(
                deviceName,
                packetText,
                false,
                parsedPacket,
                deviceName + " Packet Invalid",
                string.Empty);
        }

        #endregion
    }

}
