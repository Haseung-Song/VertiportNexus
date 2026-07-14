using System;
using System.Collections.Generic;
using System.Globalization;

namespace VertiportNexus.UdpTestClient.Protocol
{
    internal sealed class RadarTestPacketBuilder
    {
        private const byte START_FRAME = 0xAA;
        private const byte END_FRAME = 0xFE;

        private const byte CSR_ID = 0xB1;
        private const byte CSE_ID = 0xA3;

        private const byte TRACKING_REQUEST_COMMAND = 0x17;

        private uint _packetNumber = 1;

        public byte[] BuildTrackingRequest(
            ushort targetId,
            float azimuthRadian,
            float elevationRadian)
        {
            List<byte> payload = new List<byte>();

            AddUInt64(
                payload,
                CreateTimestamp());

            // pt_move: 미사용 필드지만 패킷 위치 유지를 위해 포함
            payload.Add(1);

            AddUInt16(
                payload,
                targetId);

            AddFloat(
                payload,
                azimuthRadian);

            AddFloat(
                payload,
                elevationRadian);

            // distance
            AddFloat(payload, 1500.0f);

            // vx, vy, vz
            AddFloat(payload, 0.0f);
            AddFloat(payload, 0.0f);
            AddFloat(payload, 0.0f);

            // ecefX, ecefY, ecefZ
            AddDouble(payload, 0.0);
            AddDouble(payload, 0.0);
            AddDouble(payload, 0.0);

            // reserved
            AddUInt32(payload, 0);

            if (payload.Count != 63)
            {
                throw new InvalidOperationException(
                    "Tracking Request payload must be 63 bytes.");
            }

            return BuildPacket(
                TRACKING_REQUEST_COMMAND,
                payload.ToArray());
        }

        private byte[] BuildPacket(
            byte command,
            byte[] payload)
        {
            List<byte> packet = new List<byte>();

            packet.Add(START_FRAME);
            packet.Add(CSR_ID);
            packet.Add(CSE_ID);
            packet.Add(command);

            AddUInt32(
                packet,
                _packetNumber++);

            // 현재 운영 Parser 모델 기준 SubData 길이
            AddUInt32(
                packet,
                (uint)payload.Length);

            packet.AddRange(payload);

            packet.Add(
                CalculateChecksum(payload));

            packet.Add(END_FRAME);

            return packet.ToArray();
        }

        private static byte CalculateChecksum(
            byte[] payload)
        {
            byte checksum = 0;

            foreach (byte value in payload)
            {
                checksum ^= value;
            }

            return checksum;
        }

        private static ulong CreateTimestamp()
        {
            return ulong.Parse(
                DateTime.Now.ToString(
                    "yyyyMMddHHmmssffff",
                    CultureInfo.InvariantCulture),
                CultureInfo.InvariantCulture);
        }

        private static void AddUInt16(
            ICollection<byte> target,
            ushort value)
        {
            foreach (byte item in BitConverter.GetBytes(value))
            {
                target.Add(item);
            }
        }

        private static void AddUInt32(
            ICollection<byte> target,
            uint value)
        {
            foreach (byte item in BitConverter.GetBytes(value))
            {
                target.Add(item);
            }
        }

        private static void AddUInt64(
            ICollection<byte> target,
            ulong value)
        {
            foreach (byte item in BitConverter.GetBytes(value))
            {
                target.Add(item);
            }
        }

        private static void AddFloat(
            ICollection<byte> target,
            float value)
        {
            foreach (byte item in BitConverter.GetBytes(value))
            {
                target.Add(item);
            }
        }

        private static void AddDouble(
            ICollection<byte> target,
            double value)
        {
            foreach (byte item in BitConverter.GetBytes(value))
            {
                target.Add(item);
            }
        }
    }
}