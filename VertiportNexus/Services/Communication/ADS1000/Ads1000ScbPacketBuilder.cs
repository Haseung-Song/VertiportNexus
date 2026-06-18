namespace VertiportNexus.Services.ADS1000
{
    /// <summary>
    /// [ADS1000] [SCB] [Packet] 생성 클래스
    /// 
    /// [SCB]는 주로 [Zoom] / [Focus] / [Auto Focus] /
    /// [Firmware Version Query] 등의 카메라 센서 계열 제어를 담당한다.
    /// 
    /// 기본 [Packet] 구조:
    /// [0] Sync[0]  = 0xAA
    /// [1] Sync[1]  = 0xAA
    /// [2] Cmd1     = 0x00
    /// [3] Length   = 0x05
    /// [4] Cmd2     = 기능 코드
    /// [5] Option   = 옵션
    /// [6] Data[0]  = 0x00
    /// [7] Data[1]  = 값 Low Byte
    /// [8] Data[2]  = 값 High Byte
    /// [9] Checksum = Cmd2 ~ Data 전체 XOR
    /// </summary>
    public class Ads1000ScbPacketBuilder
    {
        #region [Constants]

        /// <summary>
        /// [Packet] 시작 [Sync] 첫 번째 바이트
        /// </summary>
        private const byte SYNC_0 = 0xAA;

        /// <summary>
        /// [Packet] 시작 [Sync] 두 번째 바이트
        /// </summary>
        private const byte SYNC_1 = 0xAA;

        /// <summary>
        /// [SCB] 공통 [Cmd1]
        /// 
        /// 문서상 [SCB] 일반 제어 명령은 [Cmd1] = 0x00을 사용한다.
        /// </summary>
        private const byte SCB_COMMON_CMD1 = 0x00;

        /// <summary>
        /// [SCB] 공통 [Length]
        /// 
        /// [Cmd2] + [Option] + [Data 3byte] = 5byte 기준.
        /// </summary>
        private const byte SCB_COMMON_LENGTH = 0x05;

        /// <summary>
        /// [Zoom] 제어 [Cmd2]
        /// </summary>
        private const byte CMD2_ZOOM = 0x31;

        /// <summary>
        /// [Zoom] 위치 이동 [Cmd2]
        /// </summary>
        private const byte CMD2_ZOOM_POSITION = 0x37;

        /// <summary>
        /// [Focus] 제어 [Cmd2]
        /// </summary>
        private const byte CMD2_FOCUS = 0x33;

        /// <summary>
        /// [Focus] 위치 이동 [Cmd2]
        /// </summary>
        private const byte CMD2_FOCUS_POSITION = 0x39;

        /// <summary>
        /// [Auto Focus] 제어 [Cmd2]
        /// </summary>
        private const byte CMD2_AUTO_FOCUS = 0x35;

        /// <summary>
        /// [Firmware Version Query] [Cmd2]
        /// </summary>
        private const byte CMD2_VERSION_QUERY = 0x11;

        /// <summary>
        /// [Stop] [Option]
        /// </summary>
        private const byte OPTION_STOP = 0x00;

        /// <summary>
        /// [Zoom Tele] / [Focus Far] [Option]
        /// </summary>
        private const byte OPTION_PLUS = 0x01;

        /// <summary>
        /// [Zoom Wide] / [Focus Near] [Option]
        /// </summary>
        private const byte OPTION_MINUS = 0x02;

        /// <summary>
        /// [Auto Focus] [Option]
        /// </summary>
        private const byte OPTION_AUTO_FOCUS = 0xAF;

        /// <summary>
        /// [Zoom] / [Focus] 기본 속도
        /// 
        /// 문서 기준 [1 ~ 9] 범위 사용.
        /// </summary>
        private const ushort DEFAULT_SPEED = 5;

        #endregion

        #region [Fields]

        /// <summary>
        /// [Checksum] 계산 객체
        /// </summary>
        private readonly Ads1000Checksum _checksum;

        #endregion

        #region [Constructor]

        /// <summary>
        /// [Ads1000ScbPacketBuilder] 생성자
        /// </summary>
        public Ads1000ScbPacketBuilder()
        {
            _checksum = new Ads1000Checksum();
        }

        #endregion

        #region [Zoom Packet]

        /// <summary>
        /// [Zoom Tele] [Packet] 생성
        /// 
        /// 카메라 [Zoom In] 동작에 사용한다.
        /// </summary>
        public byte[] BuildZoomTelePacket()
        {
            return BuildCommonPacket(
                CMD2_ZOOM,
                OPTION_PLUS,
                DEFAULT_SPEED);
        }

        /// <summary>
        /// [Zoom Wide] [Packet] 생성
        /// 
        /// 카메라 [Zoom Out] 동작에 사용한다.
        /// </summary>
        public byte[] BuildZoomWidePacket()
        {
            return BuildCommonPacket(
                CMD2_ZOOM,
                OPTION_MINUS,
                DEFAULT_SPEED);
        }

        /// <summary>
        /// [Zoom Stop] [Packet] 생성
        /// 
        /// [Zoom] 연속 동작 정지에 사용한다.
        /// </summary>
        public byte[] BuildZoomStopPacket()
        {
            return BuildCommonPacket(
                CMD2_ZOOM,
                OPTION_STOP,
                DEFAULT_SPEED);
        }

        /// <summary>
        /// [Zoom] 위치 이동 [Packet] 생성
        /// 
        /// 문서 기준:
        /// Cmd2   = 0x37
        /// Option = 0x00
        /// Data   = 0 ~ 1000
        /// </summary>
        public byte[] BuildZoomPositionPacket(
            ushort zoomValue)
        {
            return BuildCommonPacket(
                CMD2_ZOOM_POSITION,
                OPTION_STOP,
                zoomValue);
        }

        #endregion

        #region [Focus Packet]

        /// <summary>
        /// [Focus Near] [Packet] 생성
        /// 
        /// 카메라 [Focus Near] 동작에 사용한다.
        /// </summary>
        public byte[] BuildFocusNearPacket()
        {
            return BuildCommonPacket(
                CMD2_FOCUS,
                OPTION_MINUS,
                DEFAULT_SPEED);
        }

        /// <summary>
        /// [Focus Far] [Packet] 생성
        /// 
        /// 카메라 [Focus Far] 동작에 사용한다.
        /// </summary>
        public byte[] BuildFocusFarPacket()
        {
            return BuildCommonPacket(
                CMD2_FOCUS,
                OPTION_PLUS,
                DEFAULT_SPEED);
        }

        /// <summary>
        /// [Focus Stop] [Packet] 생성
        /// 
        /// [Focus] 연속 동작 정지에 사용한다.
        /// </summary>
        public byte[] BuildFocusStopPacket()
        {
            return BuildCommonPacket(
                CMD2_FOCUS,
                OPTION_STOP,
                DEFAULT_SPEED);
        }

        /// <summary>
        /// [Focus] 위치 이동 [Packet] 생성
        /// 
        /// 문서 기준:
        /// Cmd2   = 0x39
        /// Option = 0x00
        /// Data   = 0 ~ 1000
        /// </summary>
        public byte[] BuildFocusPositionPacket(
            ushort focusValue)
        {
            return BuildCommonPacket(
                CMD2_FOCUS_POSITION,
                OPTION_STOP,
                focusValue);
        }

        #endregion

        #region [Auto Focus Packet]

        /// <summary>
        /// [Auto Focus] [Packet] 생성
        /// 
        /// 문서 기준:
        /// Cmd2   = 0x35
        /// Option = 0xAF
        /// Data   = 0x00 0x00 0x00
        /// </summary>
        public byte[] BuildAutoFocusPacket()
        {
            return BuildCommonPacket(
                CMD2_AUTO_FOCUS,
                OPTION_AUTO_FOCUS,
                0);
        }

        #endregion

        #region [Query Packet]

        /// <summary>
        /// [SCB] [Firmware Version Query] [Packet] 생성
        /// 
        /// [SCB] 통신 연결 및 응답 여부 확인용으로 사용할 수 있다.
        /// </summary>
        public byte[] BuildVersionQueryPacket()
        {
            return BuildCommonPacket(
                CMD2_VERSION_QUERY,
                OPTION_STOP,
                0);
        }

        #endregion

        #region [Common Packet Builder]

        /// <summary>
        /// [SCB] 공통 [Packet] 생성
        /// 
        /// [Zoom] / [Focus] / [Auto Focus] / [Version Query] 등
        /// 동일한 형식의 [SCB] 명령을 생성한다.
        /// </summary>
        /// <param name="cmd2">
        /// 기능 코드
        /// </param>
        /// <param name="option">
        /// 명령 옵션
        /// </param>
        /// <param name="data">
        /// 명령 데이터
        /// 
        /// [Zoom] / [Focus]에서는 속도값으로 사용하고,
        /// [Version Query] / [Auto Focus]에서는 0으로 사용한다.
        /// </param>
        /// <returns>
        /// 최종 [SCB] 송신 [Packet]
        /// </returns>
        private byte[] BuildCommonPacket(
            byte cmd2,
            byte option,
            ushort data)
        {
            /// <summary>
            /// [Data]는 문서 기준 [Little Endian]이다.
            /// 
            /// 예)
            /// data = 5
            /// dataLow  = 0x05
            /// dataHigh = 0x00
            /// </summary>
            byte dataLow =
                (byte)(data & 0xFF);

            byte dataHigh =
                (byte)((data >> 8) & 0xFF);

            /// <summary>
            /// [Checksum] 계산 대상:
            /// Cmd2 + Option + Data[0] + Data[1] + Data[2]
            /// </summary>
            byte checksum =
                _checksum.Calculate(
                    cmd2,
                    option,
                    0x00,
                    dataLow,
                    dataHigh);

            return new byte[]
            {
                SYNC_0,
                SYNC_1,
                SCB_COMMON_CMD1,
                SCB_COMMON_LENGTH,
                cmd2,
                option,
                0x00,
                dataLow,
                dataHigh,
                checksum
            };

        }
        #endregion
    }

}
