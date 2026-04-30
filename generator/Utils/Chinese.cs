using System;
using System.Collections.Generic;
using System.Text;

namespace Generator.Utils
{
    internal static class Chinese
    {

        private static string Numbers = "零一二三四五六七八九十";

        public static string Parse(int value)
        {
            var s = "";
            var _1 = value % 10;
            if (_1 > 0)
            {
                s += Numbers[_1];
            }
            if (value < 10)
            {
                return s;
            }
            value /= 10;
            var _2 = value % 10;
            value /= 10;
            var _3 = value % 10;
            if (_2 > 0)
            {
                if (_3 != 0 || _2 > 1)
                {
                    s = Numbers[_2] + "十" + s;
                }
                else
                {
                    s = "十" + s;
                }
            }
            if (_3 > 0)
            {
                if (_2 == 0)
                {
                    s = Numbers[_3] + "百零" + s;
                }
                else
                {
                    s = Numbers[_3] + "百" + s;
                }
            }
            return s;
        }

    }
}
