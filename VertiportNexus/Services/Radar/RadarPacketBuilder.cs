using System;
using System.Collections.Generic;
using System.Text;
using VertiportNexus.Common.Constants;
using VertiportNexus.Models.Radar;

namespace VertiportNexus.Services.Radar
{
    /// <summary>
    /// [Radar] Packet 생성 서비스
    ///
    /// Radar Response Payload를
    /// Header / SubData / Tail 구조의
    /// Packet으로 생성한다.
    /// </summary>
    internal class RadarPacketBuilder
    {
        #region [Fields]

        /// <summary>
        /// Packet 번호
        /// </summary>
        private uint _packetNumber =
            1;

        #endregion

        #region [Tracking Response]

        /// <summary>
        /// [IF-CSE-CSR-001]
        /// Tracking Response Packet 생성
        /// </summary>
        public byte[] BuildTrackingResponsePacket(
            RadarTrackingResponsePayload payload)
        {
            if (payload == null)
            {
                Console.WriteLine(
                    "[RADAR][BUILDER] Tracking Response Failed : Payload is null");

                return null;
            }

            byte[] subData =
                BuildTrackingResponseSubData(
                    payload);

            return BuildPacket(
                RadarPacketConstants.COMMAND_TRACKING_RESPONSE,
                subData);
        }

        #endregion

        #region [BIST Response]

        /// <summary>
        /// [IF-CSE-CSR-002]
        /// BIST Response Packet 생성
        /// </summary>
        public byte[] BuildBistResponsePacket(
            RadarBistResponsePayload payload)
        {
            if (payload == null)
            {
                Console.WriteLine(
                    "[RADAR][BUILDER] BIST Response Failed : Payload is null");

                return null;
            }

            byte[] subData =
                BuildBistResponseSubData(
                    payload);

            return BuildPacket(
                RadarPacketConstants.COMMAND_BIST_RESPONSE,
                subData);
        }

        #endregion

        #region [Packet Build]

        /// <summary>
        /// Packet 생성
        /// </summary>
        private byte[] BuildPacket(
            byte command,
            byte[] subData)
        {
            List<byte> packet =
                new List<byte>();

            //-----------------------------------
            // Header
            //-----------------------------------

            packet.Add(
                RadarPacketConstants.START_FRAME);

            packet.Add(
                RadarPacketConstants.CSE_ID);

            packet.Add(
                RadarPacketConstants.CSR_ID);

            packet.Add(
                command);

            packet.AddRange(
                BitConverter.GetBytes(
                    _packetNumber++));

            uint packetLength =
                (uint)
                (
                    RadarPacketConstants.HEADER_LENGTH +
                    subData.Length +
                    RadarPacketConstants.TAIL_LENGTH
                );

            packet.AddRange(
                BitConverter.GetBytes(
                    packetLength));

            //-----------------------------------
            // SubData
            //-----------------------------------

            packet.AddRange(
                subData);

            //-----------------------------------
            // Checksum
            //-----------------------------------

            packet.Add(
                CalculateChecksum(
                    subData));

            //-----------------------------------
            // Tail
            //-----------------------------------

            packet.Add(
                RadarPacketConstants.END_FRAME);

            return packet.ToArray();
        }

        #endregion

        #region [Tracking Payload]

        /// <summary>
        /// Tracking Payload 생성
        /// </summary>
        private byte[] BuildTrackingResponseSubData(
            RadarTrackingResponsePayload payload)
        {
            List<byte> bytes =
                new List<byte>();

            bytes.AddRange(
                BitConverter.GetBytes(
                    payload.TimeStamp));

            bytes.AddRange(
                BitConverter.GetBytes(
                    payload.Id));

            bytes.Add(
                payload.TrackResult);

            bytes.AddRange(
                BitConverter.GetBytes(
                    payload.Azimuth));

            bytes.AddRange(
                BitConverter.GetBytes(
                    payload.Elevation));

            byte[] recognition =
                new byte[
                    RadarPacketConstants.RECOGNITION_INFO_LENGTH];

            Encoding.ASCII.GetBytes(
                payload.RecognitionInfo ?? "")
                .CopyTo(
                    recognition,
                    0);

            bytes.AddRange(
                recognition);

            bytes.AddRange(
                BitConverter.GetBytes(
                    payload.Reserved));

            return bytes.ToArray();
        }

        #endregion

        #region [BIST Payload]

        /// <summary>
        /// BIST Payload 생성
        /// </summary>
        private byte[] BuildBistResponseSubData(
            RadarBistResponsePayload payload)
        {
            List<byte> bytes =
                new List<byte>();

            bytes.AddRange(
                BitConverter.GetBytes(
                    payload.TimeStamp));

            bytes.Add(
                payload.BistType);

            bytes.Add(
                payload.RecvResult);

            bytes.AddRange(
                BitConverter.GetBytes(
                    payload.CameraType));

            bytes.AddRange(
                BitConverter.GetBytes(
                    payload.Latitude));

            bytes.AddRange(
                BitConverter.GetBytes(
                    payload.Longitude));

            bytes.AddRange(
                BitConverter.GetBytes(
                    payload.Height));

            bytes.AddRange(
                BitConverter.GetBytes(
                    payload.Azimuth));

            bytes.AddRange(
                BitConverter.GetBytes(
                    payload.Roll));

            bytes.AddRange(
                BitConverter.GetBytes(
                    payload.Pitch));

            bytes.AddRange(
                BitConverter.GetBytes(
                    payload.Yaw));

            bytes.AddRange(
                BitConverter.GetBytes(
                    payload.Reserved));

            return bytes.ToArray();
        }

        #endregion

        #region [Checksum]

        /// <summary>
        /// Checksum 계산
        ///
        /// Header와 Tail을 제외한
        /// SubData 전체를 XOR 연산한다.
        /// </summary>
        private byte CalculateChecksum(
            byte[] subData)
        {
            byte checksum =
                0;

            foreach (byte value in subData)
            {
                checksum ^=
                    value;
            }

            return checksum;
        }
        #endregion
    }

}
