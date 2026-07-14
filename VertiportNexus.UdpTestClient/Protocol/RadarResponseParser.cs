using System;
using System.Text;
using VertiportNexus.UdpTestClient.Models;

namespace VertiportNexus.UdpTestClient.Protocol
{
    internal sealed class RadarResponseParser
    {
        private const int HEADER_LENGTH = 12;
        private const int PAYLOAD_LENGTH = 63;
        private const int PACKET_LENGTH = 77;

        public RadarResponse Parse(byte[] packet)
        {
            if (packet == null ||
                packet.Length != PACKET_LENGTH)
            {
                throw new InvalidOperationException(
                    "Response packet length must be 77 bytes.");
            }

            if (packet[0] != 0xAA)
            {
                throw new InvalidOperationException(
                    "Invalid start frame.");
            }

            if (packet[1] != 0xA3 ||
                packet[2] != 0xB1)
            {
                throw new InvalidOperationException(
                    "Invalid sender or receiver ID.");
            }

            if (packet[3] != 0xCB)
            {
                throw new InvalidOperationException(
                    "Invalid response command.");
            }

            if (packet[packet.Length - 1] != 0xFE)
            {
                throw new InvalidOperationException(
                    "Invalid end frame.");
            }

            int offset = 4;

            uint packetNumber =
                BitConverter.ToUInt32(
                    packet,
                    offset);

            offset += 4;

            uint payloadLength =
                BitConverter.ToUInt32(
                    packet,
                    offset);

            offset += 4;

            if (payloadLength != PAYLOAD_LENGTH)
            {
                throw new InvalidOperationException(
                    "Invalid response payload length.");
            }

            int payloadOffset = offset;

            // timestamp
            offset += 8;

            ushort targetId =
                BitConverter.ToUInt16(
                    packet,
                    offset);

            offset += 2;

            byte trackResult =
                packet[offset++];

            float azimuth =
                BitConverter.ToSingle(
                    packet,
                    offset);

            offset += 4;

            float elevation =
                BitConverter.ToSingle(
                    packet,
                    offset);

            offset += 4;

            string recognitionInfo =
                Encoding.ASCII
                    .GetString(
                        packet,
                        offset,
                        40)
                    .TrimEnd('\0');

            byte expectedChecksum =
                packet[packet.Length - 2];

            byte calculatedChecksum = 0;

            for (int index = payloadOffset;
                 index < payloadOffset + PAYLOAD_LENGTH;
                 index++)
            {
                calculatedChecksum ^= packet[index];
            }

            return new RadarResponse
            {
                PacketNumber = packetNumber,
                TargetId = targetId,
                TrackResult = trackResult,
                Azimuth = azimuth,
                Elevation = elevation,
                RecognitionInfo = recognitionInfo,
                IsChecksumValid =
                    expectedChecksum == calculatedChecksum
            };
        }
    }
}