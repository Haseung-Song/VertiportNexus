using System;
using System.Collections.Generic;
using VertiportNexus.Common;
using VertiportNexus.Models.ADS1000;

namespace VertiportNexus.ViewModels.Main
{
    /// <summary>
    /// [Main] 화면 [ViewModel] - TCP Receive Event
    /// [MCB] / [SCB] 수신 Packet 처리와 장비 상태 반영 이벤트를 관리한다.
    /// </summary>
    public partial class MainViewModel
    {
        #region [Receive Event Methods]

        /// <summary>
        /// [MCB] 수신 데이터 처리
        /// 
        /// [TcpClientService]에서 [MCB] 수신 데이터가 들어오면 호출된다.
        /// 실제 파싱은 [Ads1000StatusService]에서 처리한다.
        /// </summary>
        /// <param name="packet">
        /// [MCB] 수신 [Packet]
        /// </param>
        /// <param name="receivedTime">
        /// 수신 시간
        /// </param>
        private void OnMcbMessageReceived(
            byte[] packet,
            DateTime receivedTime)
        {
            ProcessReceivedPacket(
                "MCB",
                packet);
        }

        /// <summary>
        /// [SCB] 수신 데이터 처리
        /// 
        /// [TcpClientService]에서 [SCB] 수신 데이터가 들어오면 호출된다.
        /// 실제 파싱은 [Ads1000StatusService]에서 처리한다.
        /// </summary>
        /// <param name="packet">
        /// [SCB] 수신 [Packet]
        /// </param>
        /// <param name="receivedTime">
        /// 수신 시간
        /// </param>
        private void OnScbMessageReceived(
            byte[] packet,
            DateTime receivedTime)
        {
            ProcessReceivedPacket(
                "SCB",
                packet);
        }

        /// <summary>
        /// [ADS1000] 수신 [Packet] 처리 결과 화면 반영
        /// </summary>
        /// <param name="deviceName">
        /// 수신 장비 이름
        /// </param>
        /// <param name="packet">
        /// 수신 [Packet]
        /// </param>
        private void ProcessReceivedPacket(
            string deviceName,
            byte[] packet)
        {
            List<Ads1000StatusResult> statusResults =
                _ads1000StatusService.ProcessReceivedPackets(
                    deviceName,
                    packet);

            if (statusResults.Count == 0)
                return;

            App.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                foreach (Ads1000StatusResult statusResult in statusResults)
                {
                    if (!statusResult.IsValid)
                        continue;

                    ApplyParsedStatusValue(
                        statusResult.ParsedPacket);

                    if ((DateTime.Now - _lastAds1000StatusLogTime).TotalSeconds >= 3)
                    {
                        _lastAds1000StatusLogTime =
                            DateTime.Now;

                        Console.WriteLine(
                            $"[ADS1000] Pan   : {CurrentPan:F2}");

                        Console.WriteLine(
                            $"[ADS1000] Tilt  : {CurrentTilt:F2}");

                        Console.WriteLine(
                            $"[ADS1000] Zoom  : {CurrentZoom:F0}");

                        Console.WriteLine(
                            $"[ADS1000] Focus : {CurrentFocus:F0}");

                        ConsoleLogHelper.PrintLine();
                    }

                }

            }));

        }
        #endregion
    }

}
