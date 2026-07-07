namespace VertiportNexus.ViewModels.Main
{
    /// <summary>
    /// [ADS1000 Status Apply] Controller 처리 결과
    /// </summary>
    internal sealed class Ads1000StatusApplyControllerResult : ControllerResult
    {
        #region [Properties]

        internal double? CurrentPan { get; private set; }
        internal double? CurrentPanAccumulated { get; private set; }
        internal bool? HasPanAccumulatedStatus { get; private set; }
        internal double? CurrentTilt { get; private set; }
        internal double? CurrentZoom { get; private set; }
        internal double? CurrentFocus { get; private set; }

        #endregion

        #region [Constructor]

        private Ads1000StatusApplyControllerResult(
            bool isSuccess,
            string message)
            : base(
                  isSuccess,
                  message)
        {
        }

        #endregion

        #region [Factory Methods]

        internal static Ads1000StatusApplyControllerResult Success(
            string message,
            double? currentPan = null,
            double? currentPanAccumulated = null,
            bool? hasPanAccumulatedStatus = null,
            double? currentTilt = null,
            double? currentZoom = null,
            double? currentFocus = null)
        {
            return new Ads1000StatusApplyControllerResult(
                true,
                message)
            {
                CurrentPan = currentPan,
                CurrentPanAccumulated = currentPanAccumulated,
                HasPanAccumulatedStatus = hasPanAccumulatedStatus,
                CurrentTilt = currentTilt,
                CurrentZoom = currentZoom,
                CurrentFocus = currentFocus
            };
        }

        internal new static Ads1000StatusApplyControllerResult Failed(
            string message)
        {
            return new Ads1000StatusApplyControllerResult(
                false,
                message);
        }

        #endregion
    }
}
