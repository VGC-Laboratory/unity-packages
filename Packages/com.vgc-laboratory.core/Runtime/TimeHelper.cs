using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using VRC.SDKBase;

namespace VGC.Core.Runtime
{
    public enum TimeDisplayFormat
    {
        Normal,        // 秒（切り上げ）
        Sprite,        // Sprite秒
        MinuteSecond,  // mm:ss
        MinuteSecondSprite, // mm:ss Sprite
        WithMilliseconds,   // ss.mmm
    }
    
    public static class TimeHelper
    {
        [PublicAPI]
        public static void CalculateRemain(double syncedStartTime, double duration, out double remainTime)
        {
            double now = Networking.GetServerTimeInSeconds();
            var elapsedTime = Networking.CalculateServerDeltaTime(now, syncedStartTime);
            remainTime = duration - elapsedTime;
            if (remainTime < 0) remainTime = 0;
        }
        
        [PublicAPI]
        public static void ShowCountDownTime(
            double syncedStartTime,
            double duration,
            TMP_Text tmp,
            out double remainTime,
            bool showZero = false)
        {
            ShowCountDownTime(
                syncedStartTime,
                duration,
                tmp,
                out remainTime,
                TimeDisplayFormat.Normal,
                showZero);
        }

        [PublicAPI]
        public static void ShowCountDownTime(
            double syncedStartTime,
            double duration,
            TMP_Text tmp,
            out double remainTime,
            TimeDisplayFormat format,
            bool showZero = false)
        {
            CalculateRemain(syncedStartTime, duration, out remainTime);
            if (!tmp) return;

            if (remainTime <= 0 && !showZero)
            {
                tmp.text = "";
                return;
            }

            float remain = (float)remainTime;

            switch (format)
            {
                case TimeDisplayFormat.Normal:
                {
                    int t = Mathf.CeilToInt(remain);
                    tmp.text = t.ToString();
                    break;
                }

                case TimeDisplayFormat.Sprite:
                {
                    int t = Mathf.CeilToInt(remain);
                    tmp.text = SpriteNumberFormatter.ToSpriteNumberStringUnsigned(t);
                    break;
                }

                case TimeDisplayFormat.MinuteSecond:
                {
                    int t = Mathf.CeilToInt(remain);
                    int min = t / 60;
                    int sec = t % 60;

                    tmp.text = $"{min:00}:{sec:00}";
                    break;
                }

                case TimeDisplayFormat.MinuteSecondSprite:
                {
                    int t = Mathf.CeilToInt(remain);
                    int min = t / 60;
                    int sec = t % 60;
                    
                    tmp.text =
                        SpriteNumberFormatter.ToSpriteNumberStringUnsigned(min) +
                        $"<sprite index={SpriteNumberFormatter.SpriteColonIndex}>" +
                        SpriteNumberFormatter.ToSpriteNumberStringUnsigned(sec);
                    break;
                }

                case TimeDisplayFormat.WithMilliseconds:
                {
                    int sec = Mathf.FloorToInt(remain);
                    int ms = (int)((remain - sec) * 1000);

                    tmp.text = $"{sec:00}.{ms:000}";
                    break;
                }
            }
        }
    }
}