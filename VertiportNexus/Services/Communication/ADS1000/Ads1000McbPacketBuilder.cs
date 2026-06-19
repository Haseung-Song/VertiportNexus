using System;
using System.Text;
using VertiportNexus.Common;

namespace VertiportNexus.Services.ADS1000
{
    /// <summary>
    /// [ADS1000] [MCB] [Packet] 생성 클래스
    /// 
    /// [MCB]는 [Pan] / [Tilt] 모터 제어를 담당한다.
    /// 
    /// 기본 [Packet] 구조:
    /// 
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
        /// [MCB] 기본 가속도
        ///
        /// [ADS1000] Pan / Tilt 제어 시
        /// 목표 속도까지 도달하는 가속도를 설정한다.
        ///
        /// 낮은 값 사용 시 응답 속도가 느려질 수 있으므로
        /// 실장비 테스트 기준 [50000]을 사용한다.
        /// </summary>
        private const int DEFAULT_ACCELERATION = 50000;

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

        #region [Position Packet]

        /// <summary>
        /// [Pan] 절대 위치 이동 [Packet]
        /// </summary>
        public byte[] BuildPanAbsolutePositionPacket(
            double angle)
        {
            return BuildAbsolutePositionPacket(
                CMD1_PAN,
                angle);
        }

        /// <summary>
        /// [Tilt] 절대 위치 이동 [Packet]
        /// </summary>
        public byte[] BuildTiltAbsolutePositionPacket(
            double angle)
        {
            return BuildAbsolutePositionPacket(
                CMD1_TILT,
                angle);
        }

        /// <summary>
        /// [Pan] 상대 위치 이동 [Packet]
        /// </summary>
        public byte[] BuildPanRelativePositionPacket(
            double angle)
        {
            return BuildRelativePositionPacket(
                CMD1_PAN,
                angle);
        }

        /// <summary>
        /// [Tilt] 상대 위치 이동 [Packet]
        /// </summary>
        public byte[] BuildTiltRelativePositionPacket(
            double angle)
        {
            return BuildRelativePositionPacket(
                CMD1_TILT,
                angle);
        }

        /// <summary>
        /// [Pan] 현재 위치를 [0]으로 설정
        ///
        /// 모터 Disable 후
        /// 현재 Encoder 값을 [0]으로 설정한 뒤
        /// 다시 Enable 한다.
        ///
        /// MO=0;
        /// PX=0;
        /// MO=1;
        /// </summary>
        public byte[] BuildPanSetZeroPacket()
        {
            return BuildTextPacket(
                CMD1_PAN,
                "MO=0;PX=0;MO=1;");
        }

        /// <summary>
        /// [Tilt] 현재 위치를 [0]으로 설정
        /// 
        /// 모터 Disable 후
        /// 현재 Encoder 값을 [0]으로 설정한 뒤
        /// 다시 Enable 한다.
        ///
        /// MO=0;
        /// PX=0;
        /// MO=1;
        /// </summary>
        public byte[] BuildTiltSetZeroPacket()
        {
            return BuildTextPacket(
                CMD1_TILT,
                "MO=0;PX=0;MO=1;");
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
                    Ads1000Constants.MOTOR_ENCODER_RESOLUTION / 360.0 * degreePerSecond);

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
        /// [MCB] 절대 위치 이동 [Packet]
        /// 
        /// ADS3000 프로토콜 기준:
        /// PA = 위치;
        /// SP = 속도;
        /// BG;
        /// 
        /// 지정한 목표 위치까지
        /// 설정된 속도로 이동한다.
        /// </summary>
        private byte[] BuildAbsolutePositionPacket(
            byte cmd1,
            double angle)
        {
            int motorPosition =
                Convert.ToInt32(
                    Ads1000Constants.MOTOR_ENCODER_RESOLUTION
                    / 360.0
                    * angle);

            int motorSpeed =
                Convert.ToInt32(
                    Ads1000Constants.MOTOR_ENCODER_RESOLUTION
                    / 360.0
                    * Ads1000Constants.DEFAULT_POSITION_SPEED);

            string commandText =
                "PA="
                + motorPosition
                + ";SP="
                + motorSpeed
                + ";BG;";

            return BuildTextPacket(
                cmd1,
                commandText);
        }

        /// <summary>
        /// [MCB] 상대 위치 이동 [Packet]
        /// 
        /// ADS3000 프로토콜 기준:
        /// PR = 위치;
        /// SP = 속도;
        /// BG;
        /// 
        /// 현재 위치 기준으로
        /// 지정한 각도만큼 상대 이동한다.
        /// </summary>
        private byte[] BuildRelativePositionPacket(
            byte cmd1,
            double angle)
        {
            int motorPosition =
                Convert.ToInt32(
                    Ads1000Constants.MOTOR_ENCODER_RESOLUTION
                    / 360.0
                    * angle);

            int motorSpeed =
                Convert.ToInt32(
                    Ads1000Constants.MOTOR_ENCODER_RESOLUTION
                    / 360.0
                    * Ads1000Constants.DEFAULT_POSITION_SPEED);

            string commandText =
                "PR="
                + motorPosition
                + ";SP="
                + motorSpeed
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
