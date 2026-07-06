using System;

namespace VertiportNexus.Common
{
    /// <summary>
    /// [Console] 로그 출력 공통 Helper
    /// </summary>
    public static class ConsoleLogHelper
    {
        /// <summary>
        /// [Console] 로그 출력 동기화 객체
        ///
        /// 여러 비동기 수신 Task에서 동시에 Console 로그를 출력할 경우,
        /// 로그 내용이 서로 섞이지 않도록 출력 단위를 보호한다.
        /// </summary>
        private static readonly object _consoleLock =
            new object();

        /// <summary>
        /// [Console] 로그 구분선
        /// </summary>
        public const string LogLine =
            "=======================================================================================================================";

        /// <summary>
        /// [Console] 로그 구분선 출력
        /// </summary>
        public static void PrintLine()
        {
            lock (_consoleLock)
            {
                Console.WriteLine(LogLine);
            }

        }

        /// <summary>
        /// [Console] 로그 메시지 출력
        /// </summary>
        public static void PrintLine(
            string message)
        {
            lock (_consoleLock)
            {
                Console.WriteLine(message);
            }

        }

        /// <summary>
        /// [Console] 로그 블록 출력
        ///
        /// 구분선 / 제목 / 내용 출력이 서로 섞이지 않도록
        /// 하나의 출력 단위로 묶어 출력한다.
        /// </summary>
        public static void PrintBlock(
            string title,
            string content = null)
        {
            lock (_consoleLock)
            {
                Console.WriteLine(LogLine);
                Console.WriteLine(title);

                if (!string.IsNullOrWhiteSpace(content))
                {
                    Console.WriteLine(content);
                }

            }

        }

    }

}
