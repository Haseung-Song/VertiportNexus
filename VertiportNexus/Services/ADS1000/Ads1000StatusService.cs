using System;
using System.Collections.Generic;
using VertiportNexus.Common;
using VertiportNexus.Models.ADS1000;

namespace VertiportNexus.Services.ADS1000
{
    /// <summary>
    /// [ADS1000] 상태 [Packet] 처리 서비스
    /// 
    /// [MCB] / [SCB]에서 수신한 [Packet]을 파싱하고,
    /// 화면에 반영할 상태값을 추출한다.
    /// 
    /// [MainViewModel]은 수신 결과를 받아
    /// [XAML] 바인딩 속성만 갱신한다.
    /// </summary>
    internal class Ads1000StatusService
    {
        #region [Constants]

        /// <summary>
        /// [ADS1000] 수신 [Packet] 로그 출력 간격 [초]
        /// 
        /// 상태 [Packet] 로그는 지정된 시간 단위로 제한하여 출력한다.
        /// </summary>
        private const int ADS1000_RECEIVE_LOG_INTERVAL_SECONDS =
            3;

        #endregion

        #region [Fields]

        /// <summary>
        /// [ADS1000] 수신 [Packet] 파싱 객체
        /// </summary>
        private readonly Ads1000PacketParser _packetParser;

        /// <summary>
        /// 마지막 [ADS1000] 수신 [Packet] 로그 출력 시간
        /// </summary>
        private DateTime _lastAds1000ReceiveLogTime =
            DateTime.MinValue;

        #endregion

        #region [Constructor]

        /// <summary>
        /// [Ads1000StatusService] 생성자
        /// </summary>
        public Ads1000StatusService()
        {
            _packetParser =
                new Ads1000PacketParser();
        }

        #endregion

        #region [Public Methods]

        /// <summary>
        /// [ADS1000] 단일 수신 [Packet] 처리
        /// </summary>
        public Ads1000StatusResult ProcessReceivedPacket(
            string deviceName,
            byte[] packet)
        {
            string packetText =
                ConvertToHexString(
                    packet);

            Ads1000ParsedPacket parsedPacket =
                _packetParser.Parse(
                    packet);

            bool canPrintLog =
                CanPrintAds1000ReceiveLog();

            PrintParseLog(
                deviceName,
                packetText,
                parsedPacket,
                canPrintLog);

            if (!parsedPacket.IsValid)
            {
                return Ads1000StatusResult.CreateInvalid(
                    deviceName,
                    packetText,
                    parsedPacket);
            }

            return Ads1000StatusResult.CreateValid(
                deviceName,
                packetText,
                parsedPacket);
        }

        /// <summary>
        /// [ADS1000] 수신 데이터 전체 [Packet] 처리
        /// 
        /// [TCP] 수신 데이터에 여러 개의 [ADS1000] [Packet]이 포함될 수 있으므로,
        /// 모든 [Packet]을 분리 / 파싱하여 상태 처리 결과 목록으로 반환한다.
        /// </summary>
        public List<Ads1000StatusResult> ProcessReceivedPackets(
            string deviceName,
            byte[] packet)
        {
            List<Ads1000StatusResult> statusResults =
                new List<Ads1000StatusResult>();

            string packetText =
                ConvertToHexString(
                    packet);

            List<Ads1000ParsedPacket> parsedPackets =
                _packetParser.ParseAll(
                    packet);

            bool canPrintLog =
                CanPrintAds1000ReceiveLog();

            foreach (Ads1000ParsedPacket parsedPacket in parsedPackets)
            {
                PrintParseLog(
                    deviceName,
                    packetText,
                    parsedPacket,
                    canPrintLog);

                if (!parsedPacket.IsValid)
                {
                    statusResults.Add(
                        Ads1000StatusResult.CreateInvalid(
                            deviceName,
                            packetText,
                            parsedPacket));

                    continue;
                }

                statusResults.Add(
                    Ads1000StatusResult.CreateValid(
                        deviceName,
                        packetText,
                        parsedPacket));
            }

            return statusResults;
        }

        #endregion

        #region [Log Methods]

        /// <summary>
        /// [ADS1000] 수신 [Packet] 파싱 로그 출력
        /// </summary>
        private void PrintParseLog(
            string deviceName,
            string packetText,
            Ads1000ParsedPacket parsedPacket,
            bool canPrintLog)
        {
            if (!canPrintLog)
            {
                return;
            }

            Console.WriteLine("[ADS1000][" + deviceName + "] Parse Result");
            Console.WriteLine("[ADS1000][" + deviceName + "] Raw : " + packetText);
            Console.WriteLine("[ADS1000][" + deviceName + "] IsValid : " + parsedPacket.IsValid);

            if (!parsedPacket.IsValid)
            {
                Console.WriteLine("[ADS1000][" + deviceName + "] Error : " + parsedPacket.ErrorMessage);
                ConsoleLogHelper.PrintLine();

                return;
            }

            Console.WriteLine("[ADS1000][" + deviceName + "] Cmd1 : 0x" + parsedPacket.Cmd1.ToString("X2"));
            Console.WriteLine("[ADS1000][" + deviceName + "] Length : " + parsedPacket.Length);
            Console.WriteLine("[ADS1000][" + deviceName + "] Checksum : 0x" + parsedPacket.Checksum.ToString("X2"));
            ConsoleLogHelper.PrintLine();
        }

        /// <summary>
        /// [ADS1000] 수신 [Packet] 로그 출력 가능 여부 확인
        /// </summary>
        private bool CanPrintAds1000ReceiveLog()
        {
            DateTime now =
                DateTime.Now;

            if ((now - _lastAds1000ReceiveLogTime).TotalSeconds <
                ADS1000_RECEIVE_LOG_INTERVAL_SECONDS)
            {
                return false;
            }

            _lastAds1000ReceiveLogTime =
                now;

            return true;
        }

        #endregion

        #region [Utility Methods]

        /// <summary>
        /// [byte[]] 데이터를 [HEX] 문자열로 변환
        /// </summary>
        private string ConvertToHexString(
            byte[] data)
        {
            if (data == null ||
                data.Length == 0)
            {
                return string.Empty;
            }

            return BitConverter
                .ToString(
                    data)
                .Replace(
                    "-",
                    " ");
        }
        #endregion
    }

}
