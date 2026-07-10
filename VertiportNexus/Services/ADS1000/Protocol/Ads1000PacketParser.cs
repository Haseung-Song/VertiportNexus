using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using VertiportNexus.Common;
using VertiportNexus.Models.ADS1000;

namespace VertiportNexus.Services.ADS1000
{
    /// <summary>
    /// [ADS1000] 수신 [Packet] 파싱 클래스
    /// 
    /// 역할:
    /// 1. [AA AA] [Sync] 확인
    /// 2. [Cmd1] / [Length] / [Data] / [Checksum] 분리
    /// 3. [Checksum] 검증
    /// 4. 응답 종류 설명 문자열 생성
    /// </summary>
    public class Ads1000PacketParser
    {
        #region [Constants]

        /// <summary>
        /// 첫 번째 [Sync]
        /// </summary>
        private const byte SYNC_0 = 0xAA;

        /// <summary>
        /// 두 번째 [Sync]
        /// </summary>
        private const byte SYNC_1 = 0xAA;

        /// <summary>
        /// 최소 패킷 길이
        /// 
        /// Sync0 + Sync1 + Cmd1 + Length + Checksum
        /// </summary>
        private const int MIN_PACKET_LENGTH = 5;

        #endregion

        #region [Fields]

        /// <summary>
        /// [Checksum] 계산 객체
        /// </summary>
        private readonly Ads1000Checksum _checksum;

        #endregion

        #region [Constructor]

        /// <summary>
        /// [Ads1000PacketParser] 생성자
        /// </summary>
        public Ads1000PacketParser()
        {
            _checksum =
                new Ads1000Checksum();
        }

        #endregion

        #region [Parse]

        /// <summary>
        /// [ADS1000] 수신 [Packet] 파싱
        /// </summary>
        /// <param name="packet">
        /// 수신 [Packet]
        /// </param>
        /// <returns>
        /// 파싱 결과
        /// </returns>
        public Ads1000ParsedPacket Parse(
            byte[] packet)
        {
            Ads1000ParsedPacket result =
                new Ads1000ParsedPacket();

            if (packet == null ||
                packet.Length < MIN_PACKET_LENGTH)
            {
                result.IsValid = false;
                result.ErrorMessage = "Packet length is too short";

                return result;
            }

            if (packet[0] != SYNC_0 ||
                packet[1] != SYNC_1)
            {
                result.IsValid = false;
                result.ErrorMessage = "Invalid Sync";

                return result;
            }

            result.Sync0 = packet[0];

            result.Sync1 = packet[1];

            result.Cmd1 = packet[2];

            result.Length = packet[3];

            int expectedLength =
                2 + 1 + 1 + result.Length + 1;

            if (packet.Length < expectedLength)
            {
                result.IsValid = false;
                result.ErrorMessage =
                    "Packet length mismatch. Expected : "
                    + expectedLength
                    + ", Actual : "
                    + packet.Length;

                return result;
            }

            result.Data =
                new byte[result.Length];

            Array.Copy(
                packet,
                4,
                result.Data,
                0,
                result.Length);

            result.Checksum =
                packet[expectedLength - 1];

            byte calculatedChecksum =
                _checksum.Calculate(
                    result.Data);

            if (calculatedChecksum != result.Checksum)
            {
                result.IsValid = false;
                result.ErrorMessage =
                    "Checksum mismatch. Calculated : 0x"
                    + calculatedChecksum.ToString("X2")
                    + ", Received : 0x"
                    + result.Checksum.ToString("X2");

                return result;
            }

            result.IsValid = true;

            // [Cmd1] / [Data] 기준 실제 상태값 파싱
            ParseStatusValue(
                result);

            result.Description =
                GetDescription(
                    result);

            return result;
        }

        /// <summary>
        /// [ADS1000] 수신 데이터 전체 [Packet] 파싱
        /// 
        /// [TCP] 수신 특성상 한 번의 [Read]에
        /// 여러 개의 [AA AA] [Packet]이 붙어서 들어올 수 있으므로,
        /// 수신 버퍼 안의 모든 [Packet]을 분리하여 파싱한다.
        /// </summary>
        /// <param name="receivedData">
        /// [TCP]에서 수신한 전체 데이터
        /// </param>
        /// <returns>
        /// 파싱된 [ADS1000] [Packet] 목록
        /// </returns>
        public List<Ads1000ParsedPacket> ParseAll(
            byte[] receivedData)
        {
            List<Ads1000ParsedPacket> parsedPackets =
                new List<Ads1000ParsedPacket>();

            if (receivedData == null ||
                receivedData.Length < MIN_PACKET_LENGTH)
            {
                return parsedPackets;
            }

            //Console.WriteLine(
            //    "[ADS1000][MCB] ParseAll Received HEX : "
            //    + BitConverter
            //        .ToString(
            //            receivedData)
            //        .Replace(
            //            "-",
            //            " "));

            int index = 0;

            while (index <= receivedData.Length - MIN_PACKET_LENGTH)
            {
                // [AA AA] [Sync] 위치를 찾는다.
                if (receivedData[index] != SYNC_0 ||
                    receivedData[index + 1] != SYNC_1)
                {
                    index++;
                    continue;
                }

                // [Length] 위치까지 접근 가능한지 확인한다.
                if (index + 3 >= receivedData.Length)
                    break;

                byte dataLength = receivedData[index + 3];

                int packetLength =
                    2 + 1 + 1 + dataLength + 1;

                // 수신 버퍼 안에 완성 [Packet]이 모두 들어왔는지 확인한다.
                if (index + packetLength > receivedData.Length)
                    break;

                byte[] singlePacket =
                    new byte[packetLength];

                Array.Copy(
                    receivedData,
                    index,
                    singlePacket,
                    0,
                    packetLength);

                Ads1000ParsedPacket parsedPacket =
                    Parse(
                        singlePacket);

                parsedPackets.Add(
                    parsedPacket);

                index +=
                    packetLength;
            }

            return parsedPackets;
        }

        #endregion

        #region [Parsed Value]

        /// <summary>
        /// [ADS1000] 수신 [Data] 기준 상태값 파싱
        /// </summary>
        private void ParseStatusValue(
            Ads1000ParsedPacket packet)
        {
            if (packet == null ||
                packet.Data == null)
            {
                return;
            }

            if (packet.Cmd1 == 0x01)
            {
                if (TryParseMcbEncoderText(
                    packet.Data,
                    packet.Cmd1,
                    out double panValue))
                {
                    packet.HasPanValue = true;

                    // [Pan] 좌표계 보정
                    //
                    // [MCB] Pan Encoder 응답값은 변환식 자체는 문서 기준과 동일하지만,
                    // [LA Local Agent] 화면 표시 기준과 부호 방향이 반대로 확인되었다.
                    //
                    // 내부 상태값과 화면 표시값은 [LA Local Agent] 기준에 맞추기 위해
                    // [Pan] 값만 부호를 반전하여 사용한다.
                    packet.PanValue =
                        -panValue;
                }

                return;
            }

            if (packet.Cmd1 == 0x02)
            {
                if (TryParseMcbEncoderText(
                    packet.Data,
                    packet.Cmd1,
                    out double tiltValue))
                {
                    packet.HasTiltValue = true;
                    packet.TiltValue = tiltValue;
                }

                return;
            }

            if (packet.Cmd1 == 0xA1 ||
                packet.Cmd1 == 0xA3)
            {
                if (packet.Data.Length < 4)
                    return;

                short zoomValue =
                    BitConverter.ToInt16(
                        packet.Data,
                        0);

                short focusValue =
                    BitConverter.ToInt16(
                        packet.Data,
                        2);

                packet.HasZoomValue = true;
                packet.ZoomValue =
                    zoomValue;

                packet.HasFocusValue = true;
                packet.FocusValue =
                    focusValue;
            }

        }

        /// <summary>
        /// [MCB] [PX;Encoder;] 응답 문자열에서 위치값 파싱
        /// </summary>
        private bool TryParseMcbEncoderText(
            byte[] data,
            byte cmd1,
            out double angleValue)
        {
            angleValue = 0;

            string text =
                ParseAscii(
                    data);

            if (string.IsNullOrWhiteSpace(text))
                return false;

            string[] splitTexts =
                text.Split(
                    ';');

            if (splitTexts.Length < 2)
                return false;

            if (!double.TryParse(
                splitTexts[1],
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out double encoderValue))
            {
                return false;
            }

            //Console.WriteLine(
            //    "[ADS1000][MCB] Raw Encoder Value : Cmd1=0x"
            //    + cmd1.ToString("X2")
            //    + ", Encoder="
            //    + encoderValue.ToString(
            //        CultureInfo.InvariantCulture));

            // Encoder 값 => 각도 변환
            // 
            // [LA Local Agent]와 동일하게
            // Pan / Tilt 축별 Resolution을 분리하여 각도를 계산한다.
            double motorEncoderResolution =
                GetMotorEncoderResolution(
                    cmd1);

            angleValue =
                encoderValue * 360.0 / motorEncoderResolution;

            return true;
        }

        /// <summary>
        /// [MCB] 제어 축 기준 모터 엔코더 해상도 조회
        /// </summary>
        /// <param name="cmd1">
        /// 제어 축 Command
        /// </param>
        /// <returns>
        /// 제어 축별 모터 엔코더 해상도
        /// </returns>
        private double GetMotorEncoderResolution(
            byte cmd1)
        {
            if (cmd1 == 0x01)
            {
                return Ads1000Constants.PAN_MOTOR_ENCODER_RESOLUTION;
            }

            if (cmd1 == 0x02)
            {
                return Ads1000Constants.TILT_MOTOR_ENCODER_RESOLUTION;
            }

            return Ads1000Constants.PAN_MOTOR_ENCODER_RESOLUTION;
        }

        #endregion

        #region [Description]

        /// <summary>
        /// [Cmd1] 기준 수신 [Packet] 설명 생성
        /// </summary>
        private string GetDescription(
            Ads1000ParsedPacket packet)
        {
            if (packet.Cmd1 == 0xA1)
            {
                return "SCB Zoom / Focus Encoder Value";
            }

            if (packet.Cmd1 == 0xA3)
            {
                return "SCB Zoom / Focus Normalized Value";
            }

            if (packet.Cmd1 == 0xA5)
            {
                return "SCB Temperature / Humidity Value";
            }

            if (packet.Cmd1 == 0xCC)
            {
                return "Device Status";
            }

            if (packet.Cmd1 == 0xFF)
            {
                return "Firmware Version : " + ParseAscii(packet.Data);
            }

            if (packet.Cmd1 == 0x01)
            {
                return string.Empty;
            }

            if (packet.Cmd1 == 0x02)
            {
                return string.Empty;
            }

            return "Unknown ADS1000 Packet";
        }

        /// <summary>
        /// [ASCII] 문자열 변환
        /// </summary>
        private string ParseAscii(
            byte[] data)
        {
            if (data == null ||
                data.Length == 0)
            {
                return string.Empty;
            }
            return Encoding.ASCII.GetString(data).TrimEnd('\0');
        }
        #endregion
    }

}
