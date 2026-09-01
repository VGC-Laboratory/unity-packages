using System.Text;
using JetBrains.Annotations;

namespace VGC.Core.Runtime
{
    public static class SpriteNumberFormatter
    {
        // [ - ] は sprite index = 10 と仮定
        [PublicAPI]
        public const int SpriteMinusIndex = 10;
        // [ : ] は sprite index = 11 と仮定
        [PublicAPI]
        public const int SpriteColonIndex = 11;
        
        /// <summary>
        /// 非負数専用（最速）
        /// </summary>
        [PublicAPI]
        public static string ToSpriteNumberStringUnsigned(int number)
        {
            // 0は特別処理
            if (number == 0)
            {
                return "<sprite index=0>";
            }

            // 最大10桁(int)想定
            var sb = new StringBuilder(64);

            // 桁を逆順で一旦詰める
            int[] buffer = new int[10];
            int count = 0;

            while (number > 0)
            {
                buffer[count++] = number % 10;
                number /= 10;
            }

            // 正順で出力
            for (int i = count - 1; i >= 0; i--)
            {
                sb.Append("<sprite index=");
                sb.Append(buffer[i]);
                sb.Append('>');
            }

            return sb.ToString();
        }

        /// <summary>
        /// 符号あり（マイナス対応）
        /// </summary>
        [PublicAPI]
        public static string ToSpriteNumberStringSigned(int number)
        {
            if (number >= 0)
            {
                return ToSpriteNumberStringUnsigned(number);
            }
            
            // int.MinValue対策（-反転するとオーバーフロー）
            if (number == int.MinValue)
            {
                // "2147483648" を手動処理
                return "<sprite index=" + SpriteMinusIndex + ">" +
                       ToSpriteNumberStringUnsigned(2147483648u);
            }

            int positive = -number;

            var sb = new StringBuilder(64);
            sb.Append("<sprite index=");
            sb.Append(SpriteMinusIndex);
            sb.Append('>');

            sb.Append(ToSpriteNumberStringUnsigned(positive));
            return sb.ToString();
        }

        /// <summary>
        /// uint版
        /// </summary>
        [PublicAPI]
        public static string ToSpriteNumberStringUnsigned(uint number)
        {
            if (number == 0)
            {
                return "<sprite index=0>";
            }

            var sb = new StringBuilder(64);

            uint[] buffer = new uint[10];
            int count = 0;

            while (number > 0)
            {
                buffer[count++] = number % 10;
                number /= 10;
            }

            for (int i = count - 1; i >= 0; i--)
            {
                sb.Append("<sprite index=");
                sb.Append(buffer[i]);
                sb.Append('>');
            }

            return sb.ToString();
        }
    }
}