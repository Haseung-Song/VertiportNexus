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

        /// <summary>
        /// [MCB] 위치 이동 중 속도 갱신 명령
        /// 
        /// Absolute / Relative 위치 이동 중
        /// 목표 위치를 다시 송신하지 않고 속도값만 변경하기 위해 사용한다.
        /// 
        /// 위치 이동 Packet은 [SP] 속도값을 사용하므로,
        /// 이동 중 속도 갱신도 [SP] 명령을 기준으로 송신한다.
        /// </summary>
        private const string MOVE_SPEED_COMMAND =
            "SP";

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
        /// [Pan] 이동 중 속도 갱신 [Packet] 생성
        /// 
        /// Absolute / Relative 위치 이동 중
        /// 현재 설정된 [Pan / Tilt Speed] 값만 다시 송신하여
        /// [Pan] 축 이동 속도를 갱신한다.
        /// </summary>
        /// <param name="degreePerSecond">
        /// Pan 이동 속도
        /// </param>
        /// <param name="includeBeginCommand">
        /// [BG] 명령 포함 여부
        /// </param>
        /// <returns>
        /// [Pan] 이동 속도 갱신 Packet
        /// </returns>
        public byte[] BuildPanMoveSpeedPacket(
            double degreePerSecond,
            bool includeBeginCommand)
        {
            return BuildMoveSpeedPacket(
                CMD1_PAN,
                degreePerSecond,
                includeBeginCommand);
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
        /// [Tilt] 이동 중 속도 갱신 [Packet] 생성
        /// 
        /// Absolute / Relative 위치 이동 중
        /// 현재 설정된 [Pan / Tilt Speed] 값만 다시 송신하여
        /// [Tilt] 축 이동 속도를 갱신한다.
        /// </summary>
        /// <param name="degreePerSecond">
        /// Tilt 이동 속도
        /// </param>
        /// <param name="includeBeginCommand">
        /// [BG] 명령 포함 여부
        /// </param>
        /// <returns>
        /// [Tilt] 이동 속도 갱신 Packet
        /// </returns>
        public byte[] BuildTiltMoveSpeedPacket(
            double degreePerSecond,
            bool includeBeginCommand)
        {
            return BuildMoveSpeedPacket(
                CMD1_TILT,
                degreePerSecond,
                includeBeginCommand);
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
        /// 
        /// [Pan] 제어는 [LA Local Agent] 기준 표시 좌표와
        /// [MCB] 모터 명령 좌표의 부호 방향이 반대이므로,
        /// 
        /// 송신 시 [Pan] 값만 부호를 반전하여 전달한다.
        /// </summary>
        public byte[] BuildPanAbsolutePositionPacket(
            double angle,
            double speed)
        {
            return BuildAbsolutePositionPacket(
                CMD1_PAN,
                -angle,
                speed);
        }

        /// <summary>
        /// [Tilt] 절대 위치 이동 [Packet]
        /// </summary>
        public byte[] BuildTiltAbsolutePositionPacket(
            double angle,
            double speed)
        {
            return BuildAbsolutePositionPacket(
                CMD1_TILT,
                angle,
                speed);
        }

        /// <summary>
        /// [Pan] 상대 위치 이동 [Packet]
        /// 
        /// [Pan] 제어는 [LA Local Agent] 기준 표시 좌표와
        /// [MCB] 모터 명령 좌표의 부호 방향이 반대이므로,
        /// 
        /// 송신 시 [Pan] 값만 부호를 반전하여 전달한다.
        /// </summary>
        public byte[] BuildPanRelativePositionPacket(
            double angle,
            double speed)
        {
            return BuildRelativePositionPacket(
                CMD1_PAN,
                -angle,
                speed);
        }

        /// <summary>
        /// [Tilt] 상대 위치 이동 [Packet]
        /// </summary>
        public byte[] BuildTiltRelativePositionPacket(
            double angle,
            double speed)
        {
            return BuildRelativePositionPacket(
                CMD1_TILT,
                angle,
                speed);
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

        /// <summary>
        /// [Pan] Home Position Packet 생성
        /// 
        /// 문서 기준 [Pan Home] 명령 Packet을 생성한다.
        /// 
        /// Cmd1은 [0x01]이고,
        /// Data는 [XQ##START;]를 사용한다.
        /// </summary>
        /// <returns>
        /// Pan Home Packet
        /// </returns>
        public byte[] BuildPanHomePacket()
        {
            return BuildTextPacket(
                0x01,
                "XQ##START;");
        }

        /// <summary>
        /// [Tilt] Home Position Packet 생성
        /// 
        /// 문서 기준 [Tilt Home] 명령 Packet을 생성한다.
        /// 
        /// Cmd1은 [0x02]이고,
        /// Data는 [XQ##START;]를 사용한다.
        /// </summary>
        /// <returns>
        /// Tilt Home Packet
        /// </returns>
        public byte[] BuildTiltHomePacket()
        {
            return BuildTextPacket(
                0x02,
                "XQ##START;");
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
        /// [MCB] 이동 중 속도 갱신 [Packet] 생성
        /// 
        /// Absolute / Relative 위치 이동 중
        /// 목표 위치를 다시 송신하지 않고,
        /// 현재 이동 속도값만 변경하기 위한 Packet을 생성한다.
        /// 
        /// Absolute 이동은 기존 [PA] 목표 위치를 유지한 채
        /// [BG] 명령을 다시 송신하여 속도 변경을 반영한다.
        /// 
        /// Relative 이동은 [BG]를 다시 송신할 경우
        /// 기존 [PR] 상대 이동량이 다시 실행될 수 있으므로
        /// [SP] 명령만 송신한다.
        /// </summary>
        /// <param name="cmd1">
        /// 제어 축 Command
        /// </param>
        /// <param name="degreePerSecond">
        /// 이동 속도
        /// </param>
        /// <param name="includeBeginCommand">
        /// [BG] 명령 포함 여부
        /// </param>
        /// <returns>
        /// 이동 중 속도 갱신 Packet
        /// </returns>
        private byte[] BuildMoveSpeedPacket(
            byte cmd1,
            double degreePerSecond,
            bool includeBeginCommand)
        {
            double positionSpeed =
                ClampPositionSpeed(
                    degreePerSecond);

            int motorSpeed =
                Convert.ToInt32(
                    Ads1000Constants.MOTOR_ENCODER_RESOLUTION
                    / 360.0
                    * positionSpeed);

            string commandText =
                MOVE_SPEED_COMMAND
                + "="
                + motorSpeed
                + ";";

            if (includeBeginCommand)
            {
                commandText +=
                    "BG;";
            }

            return BuildTextPacket(
                cmd1,
                commandText);
        }

        /// <summary>
        /// [MCB] 절대 위치 이동 [Packet]
        /// 
        /// 문서 기준 Data 형식은
        /// [PA=위치;SP=속도;BG;] 이다.
        /// 
        /// 전달받은 각도값을 Encoder 위치값으로 변환하여
        /// 지정한 [CMD1] 축의 절대 위치 이동 Packet을 생성한다.
        /// </summary>
        private byte[] BuildAbsolutePositionPacket(
            byte cmd1,
            double angle,
            double speed)
        {
            double positionSpeed =
                ClampPositionSpeed(
                    speed);

            int motorPosition =
                Convert.ToInt32(
                    Ads1000Constants.MOTOR_ENCODER_RESOLUTION
                    / 360.0
                    * angle);

            int motorSpeed =
                Convert.ToInt32(
                    Ads1000Constants.MOTOR_ENCODER_RESOLUTION
                    / 360.0
                    * positionSpeed);

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
        /// [ADS1000] 프로토콜 기준:
        /// PR = 위치;
        /// SP = 속도;
        /// BG;
        /// 
        /// 현재 위치 기준으로
        /// 지정한 각도만큼 화면에서 설정한 [PT Speed] 값으로 상대 이동한다.
        /// </summary>
        private byte[] BuildRelativePositionPacket(
            byte cmd1,
            double angle,
            double speed)
        {
            double positionSpeed =
                ClampPositionSpeed(
                    speed);

            int motorPosition =
                Convert.ToInt32(
                    Ads1000Constants.MOTOR_ENCODER_RESOLUTION
                    / 360.0
                    * angle);

            int motorSpeed =
                Convert.ToInt32(
                    Ads1000Constants.MOTOR_ENCODER_RESOLUTION
                    / 360.0
                    * positionSpeed);

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
        /// [Pan / Tilt] 위치 이동 속도 범위 보정
        /// 
        /// Pan / Tilt Absolute / Relative 위치 이동 Packet 생성 시
        /// 입력된 속도값이 장비 운용 기준 범위를 벗어나지 않도록 보정한다.
        /// 
        /// UI에서는 최대 속도를 [50 deg/s]로 제한하지만,
        /// 다른 호출 경로에서 직접 속도값이 전달될 수 있으므로
        /// Packet 생성 단계에서도 동일하게 한 번 더 보정한다.
        /// </summary>
        /// <param name="degreePerSecond">
        /// 이동 속도
        /// </param>
        /// <returns>
        /// 보정된 이동 속도
        /// </returns>
        private double ClampPositionSpeed(
            double degreePerSecond)
        {
            const double MIN_SPEED =
                0;

            const double MAX_SPEED =
                50;

            if (degreePerSecond < MIN_SPEED)
            {
                return MIN_SPEED;
            }

            if (degreePerSecond > MAX_SPEED)
            {
                return MAX_SPEED;
            }
            return degreePerSecond;
        }

        /// <summary>
        /// [MCB] ASCII 명령 [Packet] 생성
        /// 
        /// 문서 기준 Packet 구조는
        /// [Sync0] [Sync1] [Cmd1] [Length] [Data] [Checksum] 순서이다.
        /// 
        /// [Checksum]은 ASCII Data 영역만 대상으로
        /// XOR Sum을 계산한다.
        /// </summary>
        /// <param name="cmd1">
        /// Command1
        /// </param>
        /// <param name="commandText">
        /// ASCII 명령 문자열
        /// </param>
        /// <returns>
        /// MCB ASCII 명령 Packet
        /// </returns>
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
