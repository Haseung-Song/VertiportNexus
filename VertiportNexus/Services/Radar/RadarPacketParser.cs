using Serilog;
using System;
using VertiportNexus.Common.Constants;
using VertiportNexus.Models.Radar;

namespace VertiportNexus.Services.Radar
{
    /// <summary>
    /// [Radar] Packet Parser
    /// 
    /// [CSR]에서 수신한 byte 배열을
    /// Header / SubData / Tail 구조로 분리하고,
    /// Command별 Payload 모델로 변환한다.
    /// </summary>
    internal class RadarPacketParser
    {
        #region [Fields]

        /// <summary>
        /// [Radar] Packet Parser 로그
        /// </summary>
        private static readonly ILogger Logger =
            Log.ForContext<RadarPacketParser>();

        #endregion

        #region [Public Methods]

        /// <summary>
        /// [Radar] 공통 Packet 파싱
        /// </summary>
        /// <param name="packetBytes">
        /// 수신 Packet byte 배열
        /// </param>
        /// <returns>
        /// Radar Packet
        /// </returns>
        public RadarPacket ParsePacket(
            byte[] packetBytes)
        {
            if (packetBytes == null ||
                packetBytes.Length < RadarPacketConstants.MIN_PACKET_LENGTH)
            {
                Logger.Warning(
                    "[RADAR][PARSER] Failed : Packet length is invalid. ActualLength={ActualLength}, MinLength={MinLength}",
                    packetBytes == null ? 0 : packetBytes.Length,
                    RadarPacketConstants.MIN_PACKET_LENGTH);

                return null;
            }

            RadarPacketHeader header =
                ParseHeader(
                    packetBytes);

            if (header.StartFrame != RadarPacketConstants.START_FRAME)
            {
                Logger.Warning(
                    "[RADAR][PARSER] Failed : Start Frame is invalid. StartFrame=0x{StartFrame:X2}, ExpectedStartFrame=0x{ExpectedStartFrame:X2}",
                    header.StartFrame,
                    RadarPacketConstants.START_FRAME);

                return null;
            }

            int expectedPacketLength =
                RadarPacketConstants.HEADER_LENGTH
                + (int)header.PacketLength
                + RadarPacketConstants.TAIL_LENGTH;

            if (expectedPacketLength != packetBytes.Length)
            {
                Logger.Warning(
                    "[RADAR][PARSER] Failed : Packet Length mismatch. SubDataLength={SubDataLength}, ExpectedPacketLength={ExpectedPacketLength}, ActualPacketLength={ActualPacketLength}",
                    header.PacketLength,
                    expectedPacketLength,
                    packetBytes.Length);

                return null;
            }

            Logger.Debug(
                "[RADAR][PARSER] Length OK. Header={HeaderLength}, SubData={SubDataLength}, Tail={TailLength}, Total={TotalLength}",
                RadarPacketConstants.HEADER_LENGTH,
                header.PacketLength,
                RadarPacketConstants.TAIL_LENGTH,
                expectedPacketLength);

            RadarPacketTail tail =
                ParseTail(
                    packetBytes);

            Logger.Debug(
                "[RADAR][PARSER] Tail Check. Checksum=0x{Checksum:X2}, EndFrame=0x{EndFrame:X2}, ExpectedEndFrame=0x{ExpectedEndFrame:X2}",
                tail.Checksum,
                tail.EndFrame,
                RadarPacketConstants.END_FRAME);

            if (tail.EndFrame != RadarPacketConstants.END_FRAME)
            {
                Logger.Warning(
                    "[RADAR][PARSER] Failed : End Frame is invalid. EndFrame=0x{EndFrame:X2}, ExpectedEndFrame=0x{ExpectedEndFrame:X2}",
                    tail.EndFrame,
                    RadarPacketConstants.END_FRAME);

                return null;
            }

            byte[] subData =
                ExtractSubData(
                    packetBytes,
                    header.PacketLength);

            byte calculatedChecksum =
                CalculateChecksum(
                    subData);

            if (calculatedChecksum != tail.Checksum)
            {
                Logger.Warning(
                    "[RADAR][PARSER] Failed : Checksum mismatch. ReceivedChecksum=0x{ReceivedChecksum:X2}, CalculatedChecksum=0x{CalculatedChecksum:X2}, SubDataLength={SubDataLength}",
                    tail.Checksum,
                    calculatedChecksum,
                    subData.Length);

                return null;
            }

            Logger.Debug(
                "[RADAR][PARSER] Parse Success. Command=0x{Command:X2}, PacketNumber={PacketNumber}, SubDataLength={SubDataLength}",
                header.Command,
                header.PacketNumber,
                header.PacketLength);

            return new RadarPacket
            {
                Header =
                    header,

                SubData =
                    subData,

                Tail =
                    tail
            };

        }

        /// <summary>
        /// [IF-CSR-CSE-001] 추적 요청 Payload 파싱
        /// </summary>
        public RadarTrackingRequestPayload ParseTrackingRequest(
            byte[] subData)
        {
            if (subData == null ||
                subData.Length < 63)
            {
                Logger.Warning(
                    "[RADAR][PARSER] Failed : Tracking Request SubData length is invalid. ActualLength={ActualLength}, RequiredLength={RequiredLength}",
                    subData == null ? 0 : subData.Length,
                    63);

                return null;
            }

            int offset =
                0;

            RadarTrackingRequestPayload payload =
                new RadarTrackingRequestPayload
                {
                    TimeStamp =
                        ReadInt64(
                            subData,
                            ref offset),

                    PtMove =
                        ReadByte(
                            subData,
                            ref offset),

                    Id =
                        ReadUInt16(
                            subData,
                            ref offset),

                    Azimuth =
                        ReadFloat(
                            subData,
                            ref offset),

                    Elevation =
                        ReadFloat(
                            subData,
                            ref offset),

                    Distance =
                        ReadFloat(
                            subData,
                            ref offset),

                    Vx =
                        ReadFloat(
                            subData,
                            ref offset),

                    Vy =
                        ReadFloat(
                            subData,
                            ref offset),

                    Vz =
                        ReadFloat(
                            subData,
                            ref offset),

                    EcefX =
                        ReadDouble(
                            subData,
                            ref offset),

                    EcefY =
                        ReadDouble(
                            subData,
                            ref offset),

                    EcefZ =
                        ReadDouble(
                            subData,
                            ref offset),

                    Reserved =
                        ReadUInt32(
                            subData,
                            ref offset)
                };

            Logger.Debug(
                "[RADAR][PARSER] Tracking Request Payload Parsed. TargetId={TargetId}, PtMove={PtMove}, Azimuth={Azimuth}, Elevation={Elevation}, Distance={Distance}",
                payload.Id,
                payload.PtMove,
                payload.Azimuth,
                payload.Elevation,
                payload.Distance);

            return payload;
        }

        /// <summary>
        /// [IF-CSR-CSE-002] BIST 요청 Payload 파싱
        /// </summary>
        public RadarBistRequestPayload ParseBistRequest(
            byte[] subData)
        {
            if (subData == null ||
                subData.Length < 17)
            {
                Logger.Warning(
                    "[RADAR][PARSER] Failed : BIST Request SubData length is invalid. ActualLength={ActualLength}, RequiredLength={RequiredLength}",
                    subData == null ? 0 : subData.Length,
                    17);

                return null;
            }

            int offset =
                0;

            RadarBistRequestPayload payload =
                new RadarBistRequestPayload
                {
                    TimeStamp =
                        ReadInt64(
                            subData,
                            ref offset),

                    BistType =
                        ReadByte(
                            subData,
                            ref offset),

                    ComportNumber =
                        ReadUInt32(
                            subData,
                            ref offset),

                    CbistInterval =
                        ReadUInt32(
                            subData,
                            ref offset)
                };

            Logger.Debug(
                "[RADAR][PARSER] BIST Request Payload Parsed. BistType={BistType}, ComportNumber={ComportNumber}, CbistInterval={CbistInterval}",
                payload.BistType,
                payload.ComportNumber,
                payload.CbistInterval);

            return payload;
        }

        #endregion

        #region [Packet Parse Methods]

        /// <summary>
        /// Header 파싱
        /// </summary>
        private RadarPacketHeader ParseHeader(
            byte[] packetBytes)
        {
            int offset =
                0;

            return new RadarPacketHeader
            {
                StartFrame =
                    ReadByte(
                        packetBytes,
                        ref offset),

                SendId =
                    ReadByte(
                        packetBytes,
                        ref offset),

                ReceiveId =
                    ReadByte(
                        packetBytes,
                        ref offset),

                Command =
                    ReadByte(
                        packetBytes,
                        ref offset),

                PacketNumber =
                    ReadUInt32(
                        packetBytes,
                        ref offset),

                PacketLength =
                    ReadUInt32(
                        packetBytes,
                        ref offset)
            };

        }

        /// <summary>
        /// Tail 파싱
        /// </summary>
        private RadarPacketTail ParseTail(
            byte[] packetBytes)
        {
            return new RadarPacketTail
            {
                Checksum =
                    packetBytes[packetBytes.Length - 2],

                EndFrame =
                    packetBytes[packetBytes.Length - 1]
            };

        }

        /// <summary>
        /// SubData 추출
        /// </summary>
        private byte[] ExtractSubData(
            byte[] packetBytes,
            uint packetLength)
        {
            int subDataLength =
                (int)packetLength;

            byte[] subData =
                new byte[subDataLength];

            Buffer.BlockCopy(
                packetBytes,
                RadarPacketConstants.HEADER_LENGTH,
                subData,
                0,
                subDataLength);

            return subData;
        }

        #endregion

        #region [Checksum Methods]

        /// <summary>
        /// Checksum 계산
        /// 
        /// ICD 기준 Header와 Tail을 제외한
        /// SubData 전체를 XOR 연산한다.
        /// </summary>
        private byte CalculateChecksum(
            byte[] subData)
        {
            byte checksum =
                0x00;

            for (int i = 0;
                 i < subData.Length;
                 i++)
            {
                checksum ^=
                    subData[i];
            }

            return checksum;
        }

        #endregion

        #region [Read Methods]

        private byte ReadByte(
            byte[] data,
            ref int offset)
        {
            byte value =
                data[offset];

            offset +=
                1;

            return value;
        }

        private ushort ReadUInt16(
            byte[] data,
            ref int offset)
        {
            ushort value =
                BitConverter.ToUInt16(
                    data,
                    offset);

            offset +=
                2;

            return value;
        }

        private uint ReadUInt32(
            byte[] data,
            ref int offset)
        {
            uint value =
                BitConverter.ToUInt32(
                    data,
                    offset);

            offset +=
                4;

            return value;
        }

        private long ReadInt64(
            byte[] data,
            ref int offset)
        {
            long value =
                BitConverter.ToInt64(
                    data,
                    offset);

            offset +=
                8;

            return value;
        }

        private float ReadFloat(
            byte[] data,
            ref int offset)
        {
            float value =
                BitConverter.ToSingle(
                    data,
                    offset);

            offset +=
                4;

            return value;
        }

        private double ReadDouble(
            byte[] data,
            ref int offset)
        {
            double value =
                BitConverter.ToDouble(
                    data,
                    offset);

            offset +=
                8;

            return value;
        }
        #endregion
    }

}
