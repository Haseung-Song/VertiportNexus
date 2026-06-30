namespace VertiportNexus.Common.Constants
{
    /// <summary>
    /// [Radar] Packet 상수
    /// 
    /// [CSR] ↔ [CSE] 간 Packet 송수신 시 사용하는
    /// Frame / ID / Command / Length 값을 정의한다.
    /// </summary>
    public static class RadarPacketConstants
    {
        #region [Frame Constants]

        /// <summary>
        /// 시작 프레임
        /// </summary>
        public const byte START_FRAME =
            0xAA;

        /// <summary>
        /// 종료 프레임
        /// </summary>
        public const byte END_FRAME =
            0xFF;

        #endregion

        #region [ID Constants]

        /// <summary>
        /// [CSR] 레이더 서브 시스템 ID
        /// </summary>
        public const byte CSR_ID =
            0xB1;

        /// <summary>
        /// [CSE] 카메라 제어 유닛 ID
        /// </summary>
        public const byte CSE_ID =
            0xA3;

        #endregion

        #region [Length Constants]

        /// <summary>
        /// Header 고정 길이
        /// </summary>
        public const int HEADER_LENGTH =
            12;

        /// <summary>
        /// Tail 고정 길이
        /// </summary>
        public const int TAIL_LENGTH =
            2;

        /// <summary>
        /// 최소 Packet 길이
        /// </summary>
        public const int MIN_PACKET_LENGTH =
            HEADER_LENGTH + TAIL_LENGTH;

        /// <summary>
        /// [RecognitionInfo] 고정 길이
        /// </summary>
        public const int RECOGNITION_INFO_LENGTH =
            40;

        #endregion

        #region [Command Constants]

        /// <summary>
        /// [IF-CSR-CSE-001] EO/IR 추적 요청
        /// </summary>
        public const byte COMMAND_TRACKING_REQUEST =
            23;

        /// <summary>
        /// [IF-CSR-CSE-002] EO/IR BIST 요청
        /// </summary>
        public const byte COMMAND_BIST_REQUEST =
            25;

        /// <summary>
        /// [IF-CSE-CSR-001] EO/IR 추적 결과
        /// </summary>
        public const byte COMMAND_TRACKING_RESPONSE =
            203;

        /// <summary>
        /// [IF-CSE-CSR-002] EO/IR BIST 결과
        /// </summary>
        public const byte COMMAND_BIST_RESPONSE =
            204;

        #endregion

        #region [Result Constants]

        /// <summary>
        /// 실패
        /// </summary>
        public const byte RESULT_FAIL =
            0;

        /// <summary>
        /// 성공
        /// </summary>
        public const byte RESULT_SUCCESS =
            1;

        #endregion

    }

}
