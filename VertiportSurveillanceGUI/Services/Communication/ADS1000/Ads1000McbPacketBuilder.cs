using System;
using System.Text;

namespace VertiportSurveillanceGUI.Services.ADS1000
{
    /// <summary>
    /// [ADS1000] [MCB] [Packet] 생성 클래스
    /// 
    /// [MCB]는 [Pan] / [Tilt] 모터 제어를 담당한다.
    /// 
    /// 기본 [Packet] 구조:
    /// [0] Sync[0]  = 0xAA
    /// [1] Sync[1]  = 0xAA
    /// [2] Cmd1     = 0x01 또는 0x02
    /// [3] Length   = Data 길이
    /// [4~] Data    = ASCII 명령 문자열
    /// [마지막] Checksum = Data 전체 [XOR]
    /// </summary>
    public class Ads1000McbPacketBuilder
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
        /// [Pan] 모터 [Cmd1]
        /// </summary>
        private const byte CMD1_PAN = 0x01;

        /// <summary>
        /// [Tilt] 모터 [Cmd1]
        /// </summary>
        private const byte CMD1_TILT = 0x02;

        /// <summary>
        /// 모터 엔코더 해상도
        /// 
        /// 문서 기준:
        /// 속도 = 2^19 / 360 * 각속도
        /// 위치 = 2^19 / 360 * 각도
        /// </summary>
        private const double ENCODER_RESOLUTION = 524288.0;

        /// <summary>
        /// [MCB] 기본 가속도
        ///
        /// [ADS1000] Pan / Tilt 제어 시
        /// 목표 속도까지 도달하는 가속도를 설정한다.
        ///
        /// 낮은 값 사용 시 응답 속도가 느려질 수 있으므로
        /// 실장비 테스트 기준 [30000]을 사용한다.
        /// </summary>
        private const int DEFAULT_ACCELERATION = 30000;

        #endregion

        #region [Fields]

        /// <summary>
        /// [Checksum] 계산 객체
        /// </summary>
        private readonly Ads1000Checksum _checksum;

        #endregion

        #region [Constructor]

        /// <summary>
        /// [Ads1000McbPacketBuilder] 생성자
        /// </summary>
        public Ads1000McbPacketBuilder()
        {
            _checksum = new Ads1000Checksum();
        }

        #endregion

        #region [Pan Packet]

        /// <summary>
        /// [Pan] 속도 제어 [Packet] 생성
        /// </summary>
        public byte[] BuildPanSpeedPacket(
            double degreePerSecond)
        {
            return BuildSpeedPacket(
                CMD1_PAN,
                degreePerSecond);
        }

        /// <summary>
        /// [Pan] 정지 [Packet] 생성
        /// 
        /// 모터 드라이버 정지 명령 [ST;]를 사용한다.
        /// </summary>
        public byte[] BuildPanStopPacket()
        {
            return BuildTextPacket(
                CMD1_PAN,
                "ST;");
        }

        #endregion

        #region [Tilt Packet]

        /// <summary>
        /// [Tilt] 속도 제어 [Packet] 생성
        /// </summary>
        public byte[] BuildTiltSpeedPacket(
            double degreePerSecond)
        {
            return BuildSpeedPacket(
                CMD1_TILT,
                degreePerSecond);
        }

        /// <summary>
        /// [Tilt] 정지 [Packet] 생성
        /// 
        /// 모터 드라이버 정지 명령 [ST;]를 사용한다.
        /// </summary>
        public byte[] BuildTiltStopPacket()
        {
            return BuildTextPacket(
                CMD1_TILT,
                "ST;");
        }

        #endregion

        #region [Common Packet Builder]

        /// <summary>
        /// [MCB] 속도 제어 [Packet] 생성
        /// 
        /// 생성 문자열:
        /// JV=속도;AC=가속도;BG;
        /// </summary>
        private byte[] BuildSpeedPacket(
            byte cmd1,
            double degreePerSecond)
        {
            int motorSpeed =
                Convert.ToInt32(
                    ENCODER_RESOLUTION / 360.0 * degreePerSecond);

            //Console.WriteLine(
            //    "[PT SPEED] DegreePerSecond : " +
            //    degreePerSecond);

            //Console.WriteLine(
            //    "[PT SPEED] MotorSpeed : " +
            //    motorSpeed);

            string commandText =
                "JV="
                + motorSpeed
                + ";AC="
                + DEFAULT_ACCELERATION
                + ";BG;";

            return BuildTextPacket(
                cmd1,
                commandText);
        }

        /// <summary>
        /// [MCB] ASCII 명령 [Packet] 생성
        /// </summary>
        private byte[] BuildTextPacket(
            byte cmd1,
            string commandText)
        {
            byte[] data =
                Encoding.ASCII.GetBytes(
                    commandText);

            byte checksum =
                _checksum.Calculate(
                    data);

            byte[] packet =
                new byte[2 + 1 + 1 + data.Length + 1];

            packet[0] = SYNC_0;
            packet[1] = SYNC_1;
            packet[2] = cmd1;
            packet[3] = (byte)data.Length;

            Array.Copy(
                data,
                0,
                packet,
                4,
                data.Length);

            packet[packet.Length - 1] =
                checksum;

            return packet;
        }
        #endregion
    }

}
