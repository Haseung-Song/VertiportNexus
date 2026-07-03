using OpenCvSharp;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace VertiportNexus.Converters
{
    public static class MatToBitmapSourceConverter
    {
        /// <summary>
        /// [OpenCV] [Mat] 데이터를 [WPF] [BitmapSource]로 변환
        /// </summary>
        /// <param name="frame">
        /// 변환할 [OpenCV] 영상 Frame
        /// </param>
        /// <returns>
        /// [WPF] 화면 Binding에 사용할 [BitmapSource]
        /// </returns>
        public static BitmapSource Convert(
            Mat frame)
        {
            if (frame == null ||
                frame.Empty())
            {
                return null;
            }

            int channelCount =
                frame.Channels();

            PixelFormat pixelFormat;

            // [OpenCV] 영상 채널 수에 따른 [WPF] PixelFormat 선택
            //
            // [1 Channel]
            // Gray 영상
            //
            // [3 Channel]
            // OpenCV 기본 BGR 영상
            //
            // [4 Channel]
            // BGRA 영상
            if (channelCount == 1)
            {
                pixelFormat =
                    PixelFormats.Gray8;
            }
            else if (channelCount == 3)
            {
                pixelFormat =
                    PixelFormats.Bgr24;
            }
            else if (channelCount == 4)
            {
                pixelFormat =
                    PixelFormats.Bgra32;
            }
            else
            {
                return null;
            }

            int stride =
                (int)
                    frame.Step();

            int bufferSize =
                stride
                * frame.Height;

            // [OpenCV] [Mat] 데이터를
            // [WPF]에서 표시 가능한 [BitmapSource]로 변환한다.
            //
            // [Width] / [Height]
            // 영상 해상도
            //
            // [PixelFormat]
            // 영상 색상 포맷
            //
            // [Data]
            // [Mat] 원본 영상 버퍼
            //
            // [Stride]
            // 한 줄당 메모리 크기(Byte)
            BitmapSource bitmapSource =
                BitmapSource.Create(
                    frame.Width,
                    frame.Height,
                    96,
                    96,
                    pixelFormat,
                    null,
                    frame.Data,
                    bufferSize,
                    stride);

            // [BitmapSource] Thread 고정
            //
            // RTSP 수신 Thread에서 생성한 영상을
            // UI Thread Binding에서 사용할 수 있도록 Freeze 처리한다.
            bitmapSource.Freeze();

            return bitmapSource;
        }

    }

}
