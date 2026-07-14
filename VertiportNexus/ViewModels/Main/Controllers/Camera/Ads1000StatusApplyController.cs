using System;
using VertiportNexus.Models.ADS1000;
using VertiportNexus.Services.Camera;
using VertiportNexus.Common;

namespace VertiportNexus.ViewModels.Main
{
    /// <summary>
    /// [ADS1000 Status Apply] Controller
    /// 
    /// Parsed Packet을 화면 적용 가능한 상태값으로 변환한다.
    /// 실제 Binding Property 갱신은 [MainViewModel]에서 수행한다.
    /// </summary>
    internal sealed class Ads1000StatusApplyController
    {
        #region [Service Fields]

        /// <summary>
        /// [Camera] 상태 저장 서비스
        /// </summary>
        private readonly CameraStateProvider _cameraStateProvider;

        #endregion

        #region [Constructor]

        /// <summary>
        /// [ADS1000 Status Apply] Controller 생성
        /// </summary>
        internal Ads1000StatusApplyController(
            CameraStateProvider cameraStateProvider)
        {
            _cameraStateProvider =
                cameraStateProvider;
        }

        #endregion

        #region [Status Apply Methods]

        /// <summary>
        /// [ADS1000] 상태 Packet 적용 결과 생성
        /// </summary>
        internal Ads1000StatusApplyControllerResult Apply(
            Ads1000ParsedPacket parsedPacket)
        {
            if (parsedPacket == null)
            {
                return Ads1000StatusApplyControllerResult
                    .Failed(
                        "ADS1000 Parsed Packet Empty");
            }

            try
            {
                double? pan =
                    parsedPacket.HasPanValue
                        ? parsedPacket.PanValue
                        : (double?)null;

                double? tilt =
                    parsedPacket.HasTiltValue
                        ? parsedPacket.TiltValue
                        : (double?)null;

                double? zoom =
                    parsedPacket.HasZoomValue
                        ? parsedPacket.ZoomValue
                        : (double?)null;

                double? focus =
                    parsedPacket.HasFocusValue
                        ? parsedPacket.FocusValue
                        : (double?)null;

                _cameraStateProvider
                    .UpdateState(
                        pan,
                        tilt,
                        zoom,
                        focus);

                return Ads1000StatusApplyControllerResult
                    .Success(
                        "ADS1000 Status Applied",
                        pan,
                        pan,
                        pan.HasValue ? true : (bool?)null,
                        tilt,
                        zoom,
                        focus);
            }
            catch (Exception ex)
            {
                ConsoleLogHelper.Error(
                    "[ADS1000][STATUS] Status Apply Failed : " + ex.Message);

                return Ads1000StatusApplyControllerResult
                    .Failed(
                        "ADS1000 Status Apply Failed : " + ex.Message);
            }

        }
        #endregion
    }

}
