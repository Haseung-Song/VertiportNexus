using System;
using System.Collections.Generic;
using VertiportNexus.Models.ADS1000;
using VertiportNexus.Services.ADS1000;
using VertiportNexus.Common;

namespace VertiportNexus.ViewModels.Main
{
    /// <summary>
    /// [ADS1000 Receive] Controller
    /// 
    /// TCP로 수신된 [ADS1000] byte[] 데이터를 파싱하고,
    /// 화면 상태 반영에 필요한 Parsed Packet 결과만 반환한다.
    /// </summary>
    internal sealed class Ads1000ReceiveController
    {
        #region [Service Fields]

        /// <summary>
        /// [ADS1000] 상태 Packet 처리 서비스
        /// </summary>
        private readonly Ads1000StatusService _statusService;

        #endregion

        #region [Constructor]

        /// <summary>
        /// [ADS1000 Receive] Controller 생성
        /// </summary>
        internal Ads1000ReceiveController(
            Ads1000StatusService statusService)
        {
            _statusService =
                statusService;
        }

        #endregion

        #region [Receive Methods]

        /// <summary>
        /// [ADS1000] 수신 Packet 처리
        /// </summary>
        internal Ads1000ReceiveControllerResult ProcessReceivedPacket(
            string deviceName,
            byte[] packet)
        {
            try
            {
                List<Ads1000StatusResult> statusResults =
                    _statusService
                        .ProcessReceivedPackets(
                            deviceName,
                            packet);

                List<Ads1000ParsedPacket> parsedPackets =
                    new List<Ads1000ParsedPacket>();

                if (statusResults != null)
                {
                    foreach (Ads1000StatusResult statusResult in statusResults)
                    {
                        if (statusResult == null ||
                            !statusResult.IsValid ||
                            statusResult.ParsedPacket == null)
                        {
                            continue;
                        }

                        parsedPackets
                            .Add(
                                statusResult.ParsedPacket);
                    }

                }

                return Ads1000ReceiveControllerResult
                    .Success(
                        parsedPackets);
            }
            catch (Exception ex)
            {
                ConsoleLogHelper.Error(
                    "[ADS1000][RECEIVE] Receive Failed : " + ex.Message);

                return Ads1000ReceiveControllerResult
                    .Failed(
                        "ADS1000 Receive Failed : " + ex.Message);
            }

        }
        #endregion
    }

}
