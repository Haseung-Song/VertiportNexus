using System;
using System.Windows;
using VertiportNexus.Models.ADS1000;
using VertiportNexus.Services.Command;
using VertiportNexus.ViewModels.Main;
using VertiportNexus.ViewModels.Main.Composition;

namespace VertiportNexus.Features.Main.ADS1000
{
    /// <summary>
    /// [ADS1000] 상태 수신 / 반영 Workflow
    /// 
    /// [MainViewModel]에 직접 포함되어 있던
    /// MCB / SCB 수신 Packet 처리와 상태 적용 Controller 호출 흐름을 분리한다.
    /// 
    /// 화면 Binding 속성 반영은 [MainViewModel]에서 수행하고,
    /// 본 Workflow는 Packet 처리 / 상태 적용 결과 생성까지만 담당한다.
    /// </summary>
    internal sealed class Ads1000StatusWorkflow
    {
        #region [Fields]

        /// <summary>
        /// [MainViewModel] 구성 객체
        /// </summary>
        private readonly MainViewModelContext _context;

        #endregion

        #region [Constructor]

        /// <summary>
        /// [ADS1000] 상태 수신 / 반영 Workflow 생성자
        /// </summary>
        /// <param name="context">
        /// [MainViewModel] 구성 객체
        /// </param>
        internal Ads1000StatusWorkflow(
            MainViewModelContext context)
        {
            _context =
                context;
        }

        #endregion

        #region [Receive Methods]

        /// <summary>
        /// [ADS1000] 수신 Packet 처리
        /// 
        /// 수신된 MCB / SCB Packet을 파싱하고,
        /// 파싱된 상태 Packet을 상태 적용 Controller에 전달한 뒤
        /// 적용 결과를 호출자에게 반환한다.
        /// </summary>
        /// <param name="deviceName">
        /// 수신 장비 이름
        /// </param>
        /// <param name="packet">
        /// 수신 Packet
        /// </param>
        /// <param name="applyStatusResult">
        /// 상태 적용 결과 반영 Callback
        /// </param>
        internal void ProcessReceivedPacket(
            string deviceName,
            byte[] packet,
            Action<Ads1000StatusApplyControllerResult> applyStatusResult)
        {
            Ads1000ReceiveControllerResult receiveResult =
                _context
                    .Ads1000ReceiveController
                    .ProcessReceivedPacket(
                        deviceName,
                        packet);

            if (!receiveResult.IsSuccess)
            {
                Console.WriteLine(
                    "[" + deviceName + "][RECEIVE] " + receiveResult.Message);

                return;
            }

            if (receiveResult.ParsedPackets == null)
            {
                return;
            }

            Application
                .Current
                .Dispatcher
                .BeginInvoke(
                    new Action(() =>
                    {
                        foreach (Ads1000ParsedPacket parsedPacket in receiveResult.ParsedPackets)
                        {
                            Ads1000StatusApplyControllerResult applyResult =
                                _context
                                    .Ads1000StatusApplyController
                                    .Apply(
                                        parsedPacket);

                            applyStatusResult
                                ?.Invoke(
                                    applyResult);
                        }

                    }));
        }

        #endregion

        #region [Normalize Methods]

        /// <summary>
        /// [Tilt] 상태값 범위 정규화
        /// 
        /// ADS1000 상태 Packet에서 수신한 Tilt 값을
        /// 장비 물리 제한 기준 [-90 ~ 90] 범위로 보정한다.
        /// 
        /// 장비 Encoder 오차로 인해
        /// [0] 근처의 미세 오차가 발생하는 경우,
        /// 화면 표시 및 상태 응답 기준에서는 [0]으로 보정한다.
        /// </summary>
        /// <param name="tilt">
        /// Tilt 원본 상태값
        /// </param>
        /// <returns>
        /// [-90 ~ 90] 범위로 정규화된 Tilt 상태값
        /// </returns>
        internal static double NormalizeTiltStatus(
            double tilt)
        {
            const double MIN_TILT_DEGREES =
                -90.0;

            const double MAX_TILT_DEGREES =
                90.0;

            const double ZERO_EPSILON =
                0.001;

            double normalizedTilt =
                CameraCommandService.Clamp(
                    tilt,
                    MIN_TILT_DEGREES,
                    MAX_TILT_DEGREES);

            if (Math.Abs(normalizedTilt) <= ZERO_EPSILON)
            {
                return 0.0;
            }

            return NormalizePosition(
                normalizedTilt);
        }

        /// <summary>
        /// [범위 위치 상태값] 미세 오차 보정
        /// 
        /// 장비 상태 Packet에서 수신한 위치값을
        /// 지정한 최소 / 최대 범위로 보정한다.
        /// 
        /// 장비 Encoder 또는 위치 응답에서 발생하는
        /// [0] 근처 또는 정수 위치 근처의 미세 오차는
        /// 화면 표시 및 상태 응답 기준에서 보정한다.
        /// </summary>
        /// <param name="value">
        /// 원본 위치값
        /// </param>
        /// <param name="min">
        /// 최소 위치값
        /// </param>
        /// <param name="max">
        /// 최대 위치값
        /// </param>
        /// <returns>
        /// 범위 및 미세 오차가 보정된 위치값
        /// </returns>
        internal static double NormalizeRangePosition(
            double value,
            double min,
            double max)
        {
            double clampedValue =
                CameraCommandService.Clamp(
                    value,
                    min,
                    max);

            return NormalizePosition(
                clampedValue);
        }

        /// <summary>
        /// [위치 상태값] 미세 오차 보정
        /// 
        /// 장비 Encoder 또는 위치 응답에서 발생하는
        /// [0] 근처 또는 정수 위치 근처의 미세 오차를
        /// 화면 표시 및 상태 응답 기준에서 보정한다.
        /// </summary>
        /// <param name="value">
        /// 원본 위치값
        /// </param>
        /// <returns>
        /// 미세 오차가 보정된 위치값
        /// </returns>
        internal static double NormalizePosition(
            double value)
        {
            const double ZERO_EPSILON =
                0.001;

            const double INTEGER_EPSILON =
                0.001;

            if (Math.Abs(value) <= ZERO_EPSILON)
            {
                return 0.0;
            }

            double roundedValue =
                Math.Round(
                    value);

            if (Math.Abs(value - roundedValue) <= INTEGER_EPSILON)
            {
                return roundedValue;
            }

            return value;
        }
        #endregion
    }

}
