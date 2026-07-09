using VertiportNexus.ViewModels.Main;

namespace VertiportNexus.Features.Main.Ptz
{
    /// <summary>
    /// [PTZ Control] Workflow 처리 결과
    /// 
    /// [PtzControlWorkflow]에서 수행한 제어 결과를
    /// [MainViewModel] 화면 상태에 반영하기 위해 사용한다.
    /// </summary>
    internal sealed class PtzControlWorkflowResult
    {
        #region [Properties]

        /// <summary>
        /// 처리 성공 여부
        /// </summary>
        internal bool IsSuccess { get; private set; }

        /// <summary>
        /// 화면 상태 표시 문자열
        /// </summary>
        internal string Message { get; private set; }

        /// <summary>
        /// [PTZ] 제어 모드 변경값
        /// </summary>
        internal string PtzControlMode { get; private set; }

        /// <summary>
        /// [Pan] UI Zero Offset 변경값
        /// </summary>
        internal double? PanUiZeroOffset { get; private set; }

        /// <summary>
        /// [Tilt] UI Zero Offset 변경값
        /// </summary>
        internal double? TiltUiZeroOffset { get; private set; }

        /// <summary>
        /// [Pan] 누적 상태값 초기화 필요 여부
        /// </summary>
        internal bool ShouldResetPanAccumulatedStatus { get; private set; }

        #endregion

        #region [Constructor]

        /// <summary>
        /// [PTZ Control] Workflow 처리 결과 생성자
        /// </summary>
        private PtzControlWorkflowResult(
            bool isSuccess,
            string message,
            string ptzControlMode,
            double? panUiZeroOffset,
            double? tiltUiZeroOffset,
            bool shouldResetPanAccumulatedStatus)
        {
            IsSuccess =
                isSuccess;

            Message =
                message;

            PtzControlMode =
                ptzControlMode;

            PanUiZeroOffset =
                panUiZeroOffset;

            TiltUiZeroOffset =
                tiltUiZeroOffset;

            ShouldResetPanAccumulatedStatus =
                shouldResetPanAccumulatedStatus;
        }

        #endregion

        #region [Factory Methods]

        /// <summary>
        /// 성공 결과 생성
        /// </summary>
        /// <param name="message">
        /// 화면 상태 표시 문자열
        /// </param>
        /// <returns>
        /// Workflow 처리 결과
        /// </returns>
        internal static PtzControlWorkflowResult Success(
            string message)
        {
            return new PtzControlWorkflowResult(
                true,
                message,
                null,
                null,
                null,
                false);
        }

        /// <summary>
        /// [PTZ] 제어 모드 변경 성공 결과 생성
        /// </summary>
        /// <param name="message">
        /// 화면 상태 표시 문자열
        /// </param>
        /// <param name="ptzControlMode">
        /// [PTZ] 제어 모드
        /// </param>
        /// <returns>
        /// Workflow 처리 결과
        /// </returns>
        internal static PtzControlWorkflowResult ModeChanged(
            string message,
            string ptzControlMode)
        {
            return new PtzControlWorkflowResult(
                true,
                message,
                ptzControlMode,
                null,
                null,
                false);
        }

        /// <summary>
        /// [Home Position] 완료 결과 생성
        /// </summary>
        /// <param name="message">
        /// 화면 상태 표시 문자열
        /// </param>
        /// <param name="panUiZeroOffset">
        /// [Pan] UI Zero Offset
        /// </param>
        /// <param name="tiltUiZeroOffset">
        /// [Tilt] UI Zero Offset
        /// </param>
        /// <returns>
        /// Workflow 처리 결과
        /// </returns>
        internal static PtzControlWorkflowResult HomePositionCompleted(
            string message,
            double panUiZeroOffset,
            double tiltUiZeroOffset)
        {
            return new PtzControlWorkflowResult(
                true,
                message,
                null,
                panUiZeroOffset,
                tiltUiZeroOffset,
                true);
        }

        /// <summary>
        /// [Pan Zero] 설정 완료 결과 생성
        /// </summary>
        /// <param name="message">
        /// 화면 상태 표시 문자열
        /// </param>
        /// <param name="panUiZeroOffset">
        /// [Pan] UI Zero Offset
        /// </param>
        /// <returns>
        /// Workflow 처리 결과
        /// </returns>
        internal static PtzControlWorkflowResult PanZeroCompleted(
            string message,
            double panUiZeroOffset)
        {
            return new PtzControlWorkflowResult(
                true,
                message,
                null,
                panUiZeroOffset,
                null,
                true);
        }

        /// <summary>
        /// [Tilt Zero] 설정 완료 결과 생성
        /// </summary>
        /// <param name="message">
        /// 화면 상태 표시 문자열
        /// </param>
        /// <param name="tiltUiZeroOffset">
        /// [Tilt] UI Zero Offset
        /// </param>
        /// <returns>
        /// Workflow 처리 결과
        /// </returns>
        internal static PtzControlWorkflowResult TiltZeroCompleted(
            string message,
            double tiltUiZeroOffset)
        {
            return new PtzControlWorkflowResult(
                true,
                message,
                null,
                null,
                tiltUiZeroOffset,
                false);
        }

        /// <summary>
        /// 실패 / 무시 결과 생성
        /// </summary>
        /// <param name="message">
        /// 화면 상태 표시 문자열
        /// </param>
        /// <returns>
        /// Workflow 처리 결과
        /// </returns>
        internal static PtzControlWorkflowResult Ignored(
            string message)
        {
            return new PtzControlWorkflowResult(
                false,
                message,
                null,
                null,
                null,
                false);
        }

        /// <summary>
        /// Controller 결과 변환
        /// </summary>
        /// <param name="result">
        /// Controller 처리 결과
        /// </param>
        /// <returns>
        /// Workflow 처리 결과
        /// </returns>
        internal static PtzControlWorkflowResult FromControllerResult(
            ControllerResult result)
        {
            if (result == null)
            {
                return Ignored(
                    string.Empty);
            }

            return new PtzControlWorkflowResult(
                result.IsSuccess,
                result.Message,
                null,
                null,
                null,
                false);
        }

        /// <summary>
        /// PTZ Controller 결과 변환
        /// </summary>
        /// <param name="result">
        /// PTZ Controller 처리 결과
        /// </param>
        /// <returns>
        /// Workflow 처리 결과
        /// </returns>
        internal static PtzControlWorkflowResult FromPtzControllerResult(
            PtzControllerResult result)
        {
            if (result == null)
            {
                return Ignored(
                    string.Empty);
            }

            return new PtzControlWorkflowResult(
                result.IsSuccess,
                result.Message,
                null,
                null,
                null,
                false);
        }
        #endregion
    }

}
