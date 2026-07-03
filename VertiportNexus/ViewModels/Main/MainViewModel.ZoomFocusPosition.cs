using System;

namespace VertiportNexus.ViewModels.Main
{
    /// <summary>
    /// [Main] 화면 [ViewModel] - Zoom / Focus Position
    /// Zoom Position / Zoom Ratio / Focus Position 제어 로직을 관리한다.
    /// </summary>
    public partial class MainViewModel
    {
        #region [Zoom / Focus Position Control Methods]

        /// <summary>
        /// [Zoom] 지정 위치 이동
        /// 
        /// 입력된 [Zoom Position] 값을
        /// [0 ~ 1000] 범위로 보정한 후
        /// [ADS1000] 장비에 위치 이동 명령을 전송한다.
        /// </summary>
        private void SetZoomPosition()
        {
            if (!ZoomPositionValue.HasValue)
            {
                Console.WriteLine(
                    "[UI][POSITION] Zoom Failed : Value is empty");

                return;
            }

            double zoom =
                Clamp(
                    ZoomPositionValue.Value,
                    0,
                    1000);

            Console.WriteLine("[UI][POSITION] Set Zoom Target : " + zoom);
            Console.WriteLine("[UI][POSITION] Current Zoom Before : " + CurrentZoom);

            _ads1000CameraControlService
                .MoveZoomPosition(
                    (ushort)zoom);
        }

        /// <summary>
        /// [Zoom] 배율 기준 위치 이동
        /// 
        /// 입력된 [Zoom Ratio] 값을
        /// 실제 배율 기준으로 보정한 후,
        /// [ADS1000] 장비 위치값 [0 ~ 1000]으로 변환하여 전송한다.
        /// 
        /// 장비 스펙 기준 최대 배율을 [66x] 기준으로 구현한다.
        /// </summary>
        private void SetZoomRatio()
        {
            if (!ZoomRatioValue.HasValue)
            {
                Console.WriteLine(
                    "[UI][ZOOM] Ratio Failed : Value is empty");

                return;
            }

            ushort zoomPosition =
                ConvertZoomRatioToPosition(
                    ZoomRatioValue.Value);

            Console.WriteLine("[UI][ZOOM] Set Ratio Target : " + ZoomRatioValue.Value);
            Console.WriteLine("[UI][ZOOM] Converted Position : " + zoomPosition);
            Console.WriteLine("[UI][ZOOM] Current Zoom Before : " + CurrentZoom);

            _ads1000CameraControlService
                .MoveZoomPosition(
                    zoomPosition);
        }

        /// <summary>
        /// [Zoom] 배율을 [ADS1000] 위치값으로 변환
        /// 
        /// [UI] 또는 [ICD]에서 사용하는 [Zoom] 배율값을
        /// [ADS1000] 제어용 [0 ~ 1000] 위치값으로 변환한다.
        /// 
        /// 변환 기준:
        /// [1x]  = 0
        /// [66x] = 1000
        /// </summary>
        /// <param name="zoomRatio">
        /// Zoom 배율
        /// </param>
        /// <returns>
        /// ADS1000 Zoom 위치값
        /// </returns>
        private ushort ConvertZoomRatioToPosition(
            double zoomRatio)
        {
            const double MIN_ZOOM_RATIO =
                1.0;

            const double MAX_ZOOM_RATIO =
                66.0;

            double clampedZoomRatio =
                Clamp(
                    zoomRatio,
                    MIN_ZOOM_RATIO,
                    MAX_ZOOM_RATIO);

            double zoomPosition =
                (clampedZoomRatio - MIN_ZOOM_RATIO)
                / (MAX_ZOOM_RATIO - MIN_ZOOM_RATIO)
                * 1000.0;

            return (ushort)Math.Round(
                zoomPosition);
        }

        /// <summary>
        /// [Zoom] 위치값을 배율로 변환
        /// 
        /// ADS1000 장비 상태값 [0 ~ 1000]을
        /// 화면 표시용 [Zoom] 배율값 [x1.0 ~ x66.0]으로 변환한다.
        /// 
        /// 변환 기준:
        /// [0]    = [1.0x]
        /// [1000] = [66.0x]
        /// 
        /// 화면 표시 기준으로 소수점 첫째 자리까지 반올림한다.
        /// </summary>
        /// <param name="zoomPosition">
        /// ADS1000 Zoom 위치값
        /// </param>
        /// <returns>
        /// Zoom 배율
        /// </returns>
        private double ConvertZoomPositionToRatio(
            double zoomPosition)
        {
            const double MIN_ZOOM_POSITION =
                0.0;

            const double MAX_ZOOM_POSITION =
                1000.0;

            const double MIN_ZOOM_RATIO =
                1.0;

            const double MAX_ZOOM_RATIO =
                66.0;

            double clampedZoomPosition =
                Clamp(
                    zoomPosition,
                    MIN_ZOOM_POSITION,
                    MAX_ZOOM_POSITION);

            double zoomRatio =
                MIN_ZOOM_RATIO
                + (clampedZoomPosition - MIN_ZOOM_POSITION)
                / (MAX_ZOOM_POSITION - MIN_ZOOM_POSITION)
                * (MAX_ZOOM_RATIO - MIN_ZOOM_RATIO);

            return Math.Round(
                zoomRatio,
                1);
        }

        /// <summary>
        /// [Focus] 지정 위치 이동
        /// 
        /// 입력된 [Focus Position] 값을
        /// [0 ~ 1000] 범위로 보정한 후
        /// [ADS1000] 장비에 위치 이동 명령을 전송한다.
        /// </summary>
        private void SetFocusPosition()
        {
            if (!FocusPositionValue.HasValue)
            {
                Console.WriteLine(
                    "[UI][POSITION] Focus Failed : Value is empty");

                return;
            }

            double focus =
                Clamp(
                    FocusPositionValue.Value,
                    0,
                    1000);

            Console.WriteLine("[UI][POSITION] Set Focus Target : " + focus);
            Console.WriteLine("[UI][POSITION] Current Focus Before : " + CurrentFocus);

            _ads1000CameraControlService
                .MoveFocusPosition(
                    (ushort)focus);
        }
        #endregion
    }

}
