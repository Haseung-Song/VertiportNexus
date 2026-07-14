namespace VertiportNexus.Common.Constants
{
    /// <summary>
    /// [Radar] Packet 상수
    /// 
    /// [CSR] ↔ [CSE] 간 Radar UDP Packet 송수신 시 사용하는
    /// Frame / ID / Length / Command / Result 기준값을 정의한다.
    /// </summary>
    public static class RadarPacketConstants
    {
        #region [Frame Constants]

        /// <summary>
        /// [Radar] Packet 시작 Frame
        /// </summary>
        public const byte START_FRAME =
            0xAA;

        /// <summary>
        /// [Radar] Packet 종료 Frame
        /// </summary>
        public const byte END_FRAME =
            0xFE;

        #endregion

        #region [ID Constants]

        /// <summary>
        /// [CSR] Radar Sub System ID
        /// </summary>
        public const byte CSR_ID =
            0xB1;

        /// <summary>
        /// [CSE] Camera Control Unit ID
        /// </summary>
        public const byte CSE_ID =
            0xA3;

        #endregion

        #region [Length Constants]

        /// <summary>
        /// [Radar] Header 고정 길이
        /// </summary>
        public const int HEADER_LENGTH =
            12;

        /// <summary>
        /// [Radar] Tail 고정 길이
        /// </summary>
        public const int TAIL_LENGTH =
            2;

        /// <summary>
        /// [Radar] 최소 Packet 길이
        /// 
        /// Header + Tail 기준으로 Packet 유효성 검증 시 사용한다.
        /// </summary>
        public const int MIN_PACKET_LENGTH =
            HEADER_LENGTH + TAIL_LENGTH;

        /// <summary>
        /// [RecognitionInfo] 고정 길이
        /// 
        /// Tracking Request Payload 내 RecognitionInfo 영역 파싱 시 사용한다.
        /// </summary>
        public const int RECOGNITION_INFO_LENGTH =
            40;

        #endregion

        #region [Command Constants]

        /// <summary>
        /// [IF-CSR-CSE-001] Tracking Request Command
        /// 
        /// [CSR]에서 [CSE]로 전달하는 EO / IR 추적 요청 명령이다.
        /// </summary>
        public const byte COMMAND_TRACKING_REQUEST =
            0x17;

        /// <summary>
        /// [IF-CSR-CSE-002] BIST Request Command
        /// 
        /// 현재 최종 연동 범위에서는 Tracking Request 중심으로 사용하지만,
        /// 기존 Handler / Mock Test 참조 구조 유지를 위해 상수는 유지한다.
        /// </summary>
        public const byte COMMAND_BIST_REQUEST =
            0x19;

        /// <summary>
        /// [IF-CSE-CSR-001] Tracking Response Command
        /// 
        /// [CSE]에서 [CSR]로 전달하는 EO / IR 추적 결과 응답 명령이다.
        /// </summary>
        public const byte COMMAND_TRACKING_RESPONSE =
            0xCB;

        /// <summary>
        /// [IF-CSE-CSR-002] BIST Response Command
        /// 
        /// 현재 최종 연동 범위에서는 Tracking Response 중심으로 사용하지만,
        /// 기존 Handler / Mock Test 참조 구조 유지를 위해 상수는 유지한다.
        /// </summary>
        public const byte COMMAND_BIST_RESPONSE =
            0xCC;

        #endregion

        #region [Result Constants]

        /// <summary>
        /// [Radar] 처리 실패 결과값
        /// </summary>
        public const byte RESULT_FAIL =
            0x00;

        /// <summary>
        /// [Radar] 처리 성공 결과값
        /// </summary>
        public const byte RESULT_SUCCESS =
            0x01;

        #endregion
    }

}
