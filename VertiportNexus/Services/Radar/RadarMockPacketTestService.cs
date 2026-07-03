using System;
using System.Collections.Generic;
using VertiportNexus.Common;
using VertiportNexus.Common.Constants;

namespace VertiportNexus.Services.Radar
{
    /// <summary>
    /// [Radar] Mock Packet 테스트 서비스
    /// 
    /// 실제 Radar 장비 연동 전,
    /// [CSR] → [CSE] 수신 Packet을 임시 생성하여
    /// Parser / Handler / Builder 흐름을 검증한다.
    /// </summary>
    internal class RadarMockPacketTestService
    {
        #region [Fields]

        /// <summary>
        /// [Radar] Command 처리 서비스
        /// </summary>
        private readonly RadarCommandHandler _radarCommandHandler;

        /// <summary>
        /// 테스트 Packet 번호
        /// </summary>
        private uint _testPacketNumber =
            1;

        #endregion

        #region [Constructor]

        /// <summary>
        /// [RadarMockPacketTestService] 생성자
        /// </summary>
        internal RadarMockPacketTestService(
            RadarCommandHandler radarCommandHandler)
        {
            _radarCommandHandler =
                radarCommandHandler
                ?? throw new ArgumentNullException(
                    nameof(radarCommandHandler));
        }

        #endregion

        #region [Public Methods]

        /// <summary>
        /// [Radar] Mock Packet 통합 테스트 실행
        /// </summary>
        public void RunAllTests()
        {
            Console.WriteLine("[RADAR][MOCK] Test Start");

            RunTrackingRequestTest();

            RunBistRequestTest();

            ConsoleLogHelper.PrintLine();
            Console.WriteLine("[RADAR][MOCK] Test End");
            ConsoleLogHelper.PrintLine();
        }

        /// <summary>
        /// [IF-CSR-CSE-001] 추적 요청 테스트
        /// </summary>
        public void RunTrackingRequestTest()
        {
            ConsoleLogHelper.PrintLine();
            Console.WriteLine("[RADAR][MOCK] Tracking Request Test Start");

            byte[] requestPacket =
                BuildTrackingRequestPacket();

            PrintHexData(
                "[RADAR][MOCK] Tracking Request Packet",
                requestPacket);

            byte[] responsePacket =
                _radarCommandHandler
                    .Handle(
                        requestPacket);

            PrintHexData(
                "[RADAR][MOCK] Tracking Response Packet",
                responsePacket);

            ConsoleLogHelper.PrintLine();

            Console.WriteLine("[RADAR][MOCK] Tracking Request Test End");
            ConsoleLogHelper.PrintLine();
        }

        /// <summary>
        /// [IF-CSR-CSE-002] BIST 요청 테스트
        /// 
        /// 최종 ICD 정리 후 삭제 예정인 임시 테스트이다.
        /// </summary>
        public void RunBistRequestTest()
        {
            Console.WriteLine("[RADAR][MOCK] BIST Request Test Start");

            byte[] requestPacket =
                BuildBistRequestPacket();

            PrintHexData(
                "[RADAR][MOCK] BIST Request Packet",
                requestPacket);

            byte[] responsePacket =
                _radarCommandHandler
                    .Handle(
                        requestPacket);

            PrintHexData(
                "[RADAR][MOCK] BIST Response Packet",
                responsePacket);

            ConsoleLogHelper.PrintLine();

            Console.WriteLine("[RADAR][MOCK] BIST Request Test End");
            ConsoleLogHelper.PrintLine();
        }

        /// <summary>
        /// [Radar] Tracking Request Mock Packet 생성
        /// 
        /// UDP Loopback 테스트에서 사용할
        /// [IF-CSR-CSE-001] Tracking Request Packet을 생성한다.
        /// </summary>
        /// <returns>
        /// Tracking Request Mock Packet
        /// </returns>
        public byte[] CreateTrackingRequestPacket()
        {
            return BuildTrackingRequestPacket();
        }

        /// <summary>
        /// [Radar] BIST Request Mock Packet 생성
        /// 
        /// UDP Loopback 테스트에서 사용할
        /// [IF-CSR-CSE-002] BIST Request Packet을 생성한다.
        /// </summary>
        /// <returns>
        /// BIST Request Mock Packet
        /// </returns>
        public byte[] CreateBistRequestPacket()
        {
            return BuildBistRequestPacket();
        }

        #endregion

        #region [Mock Packet Build Methods]

        /// <summary>
        /// [IF-CSR-CSE-001] 추적 요청 Packet 생성
        /// </summary>
        private byte[] BuildTrackingRequestPacket()
        {
            List<byte> subData =
                new List<byte>();

            AddInt64(
                subData,
                DateTimeOffset.Now.ToUnixTimeMilliseconds());

            // [PT MOVE]
            //
            // 0: Release
            // 1: On
            AddByte(
                subData,
                1);

            // [표적 ID]
            AddUInt16(
                subData,
                1);

            // [방위각] [rad]
            //
            // 테스트 기준:
            // 10도 = 0.1745329 rad
            AddFloat(
                subData,
                0.1745329f);

            // [고각] [rad]
            //
            // 테스트 기준:
            // 5도 = 0.0872665 rad
            AddFloat(
                subData,
                0.0872665f);

            // [거리] [m]
            AddFloat(
                subData,
                1500.0f);

            // [속도 X]
            AddFloat(
                subData,
                0.0f);

            // [속도 Y]
            AddFloat(
                subData,
                0.0f);

            // [속도 Z]
            AddFloat(
                subData,
                0.0f);

            // [ECEF X]
            AddDouble(
                subData,
                0.0);

            // [ECEF Y]
            AddDouble(
                subData,
                0.0);

            // [ECEF Z]
            AddDouble(
                subData,
                0.0);

            // [Reserved]
            AddUInt32(
                subData,
                0);

            return BuildPacket(
                RadarPacketConstants.COMMAND_TRACKING_REQUEST,
                subData.ToArray());
        }

        /// <summary>
        /// [IF-CSR-CSE-002] BIST 요청 Packet 생성
        /// </summary>
        private byte[] BuildBistRequestPacket()
        {
            List<byte> subData =
                new List<byte>();

            AddInt64(
                subData,
                DateTimeOffset.Now.ToUnixTimeMilliseconds());

            // [BIST Type]
            //
            // 1: BIST
            AddByte(
                subData,
                1);

            // [Comport Number]
            AddUInt32(
                subData,
                4);

            // [CBIST Interval]
            AddUInt32(
                subData,
                1000);

            return BuildPacket(
                RadarPacketConstants.COMMAND_BIST_REQUEST,
                subData.ToArray());
        }

        /// <summary>
        /// 공통 Radar Packet 생성
        /// </summary>
        private byte[] BuildPacket(
            byte command,
            byte[] subData)
        {
            List<byte> packet =
                new List<byte>();

            // [Header] 시작 프레임
            packet.Add(
                RadarPacketConstants.START_FRAME);

            // [Header] 송신 ID
            //
            // Mock Packet은 [CSR] → [CSE] 수신 상황을 가정한다.
            packet.Add(
                RadarPacketConstants.CSR_ID);

            // [Header] 수신 ID
            packet.Add(
                RadarPacketConstants.CSE_ID);

            // [Header] 명령 코드
            packet.Add(
                command);

            // [Header] Packet 번호
            packet.AddRange(
                BitConverter.GetBytes(
                    _testPacketNumber++));

            uint packetLength =
                (uint)
                (
                    RadarPacketConstants.HEADER_LENGTH
                    + subData.Length
                    + RadarPacketConstants.TAIL_LENGTH
                );

            // [Header] Packet 전체 길이
            packet.AddRange(
                BitConverter.GetBytes(
                    packetLength));

            // [SubData]
            packet.AddRange(
                subData);

            // [Tail] Checksum
            packet.Add(
                CalculateChecksum(
                    subData));

            // [Tail] 종료 프레임
            packet.Add(
                RadarPacketConstants.END_FRAME);

            return packet
                .ToArray();
        }

        #endregion

        #region [Checksum Methods]

        /// <summary>
        /// Checksum 계산
        /// 
        /// ICD 기준 Header / Tail을 제외한
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

        #region [Byte Add Methods]

        /// <summary>
        /// [byte] 값 추가
        /// </summary>
        private void AddByte(
            List<byte> bytes,
            byte value)
        {
            bytes
                .Add(
                    value);
        }

        /// <summary>
        /// [ushort] 값 추가
        /// </summary>
        private void AddUInt16(
            List<byte> bytes,
            ushort value)
        {
            bytes
                .AddRange(
                    BitConverter.GetBytes(
                        value));
        }

        /// <summary>
        /// [uint] 값 추가
        /// </summary>
        private void AddUInt32(
            List<byte> bytes,
            uint value)
        {
            bytes
                .AddRange(
                    BitConverter.GetBytes(
                        value));
        }

        /// <summary>
        /// [long] 값 추가
        /// </summary>
        private void AddInt64(
            List<byte> bytes,
            long value)
        {
            bytes
                .AddRange(
                    BitConverter.GetBytes(
                        value));
        }

        /// <summary>
        /// [float] 값 추가
        /// </summary>
        private void AddFloat(
            List<byte> bytes,
            float value)
        {
            bytes
                .AddRange(
                    BitConverter.GetBytes(
                        value));
        }

        /// <summary>
        /// [double] 값 추가
        /// </summary>
        private void AddDouble(
            List<byte> bytes,
            double value)
        {
            bytes
                .AddRange(
                    BitConverter.GetBytes(
                        value));
        }

        #endregion

        #region [Log Methods]

        /// <summary>
        /// [byte[]] HEX 로그 출력
        /// </summary>
        private void PrintHexData(
            string title,
            byte[] data)
        {
            if (data == null ||
                data.Length == 0)
            {
                Console.WriteLine(title + " : Empty");
                return;
            }

            Console.WriteLine(
                title);

            Console.WriteLine();

            Console.WriteLine(
                BitConverter
                    .ToString(
                        data)
                    .Replace(
                        "-",
                        " "));

        }
        #endregion
    }

}
