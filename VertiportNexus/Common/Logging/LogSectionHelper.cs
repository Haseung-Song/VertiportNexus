using Serilog;

namespace VertiportNexus.Common.Logging
{
    /// <summary>
    /// [Log] 구간 구분 출력 Helper
    /// </summary>
    internal static class LogSectionHelper
    {
        #region [Constants]

        /// <summary>
        /// [Log] 구간 구분선
        /// </summary>
        private const string SECTION_LINE =
            "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━";

        #endregion

        #region [Public Methods]

        /// <summary>
        /// [Information] 수준의 Log 구간 구분선 출력
        /// </summary>
        internal static void Information(
            string title)
        {
            Log.Information(
                SECTION_LINE);

            Log.Information(
                "{Title}",
                title);

            Log.Information(
                SECTION_LINE);
        }

        /// <summary>
        /// [Debug] 수준의 Log 구간 구분선 출력
        /// </summary>
        internal static void Debug(
            string title)
        {
            Log.Debug(
                SECTION_LINE);

            Log.Debug(
                "{Title}",
                title);

            Log.Debug(
                SECTION_LINE);
        }
        #endregion
    }

}
