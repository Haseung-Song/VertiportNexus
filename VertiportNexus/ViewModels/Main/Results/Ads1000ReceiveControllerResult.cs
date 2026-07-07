using System.Collections.Generic;
using VertiportNexus.Models.ADS1000;

namespace VertiportNexus.ViewModels.Main
{
    /// <summary>
    /// [ADS1000 Receive] Controller 처리 결과
    /// </summary>
    internal sealed class Ads1000ReceiveControllerResult : ControllerResult
    {
        #region [Properties]

        /// <summary>
        /// 파싱된 [ADS1000] 상태 Packet 목록
        /// </summary>
        internal IReadOnlyList<Ads1000ParsedPacket> ParsedPackets { get; private set; }

        #endregion

        #region [Constructor]

        /// <summary>
        /// [ADS1000 Receive] Controller 처리 결과 생성
        /// </summary>
        private Ads1000ReceiveControllerResult(
            bool isSuccess,
            string message,
            IReadOnlyList<Ads1000ParsedPacket> parsedPackets)
            : base(
                  isSuccess,
                  message)
        {
            ParsedPackets =
                parsedPackets;
        }

        #endregion

        #region [Factory Methods]

        /// <summary>
        /// 수신 처리 성공 결과 생성
        /// </summary>
        internal static Ads1000ReceiveControllerResult Success(
            IReadOnlyList<Ads1000ParsedPacket> parsedPackets)
        {
            return new Ads1000ReceiveControllerResult(
                true,
                "ADS1000 Packet Parsed",
                parsedPackets);
        }

        /// <summary>
        /// 수신 처리 실패 결과 생성
        /// </summary>
        internal new static Ads1000ReceiveControllerResult Failed(
            string message)
        {
            return new Ads1000ReceiveControllerResult(
                false,
                message,
                new List<Ads1000ParsedPacket>());
        }

        #endregion
    }
}
