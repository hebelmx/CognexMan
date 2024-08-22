using System;
using System.Text;

namespace Cognex.DataMan.SDK.Utils
{
    public static class DmccEscaper
    {
        private static char[] _escapeChars;

        private static byte[] _escapeMap;

        private static byte[] _unescapeMap;

        static DmccEscaper()
        {
            DmccEscaper._escapeChars = new char[] { '\"', '\\', '|', '\t', '\r', '\n' };
            DmccEscaper._escapeMap = new byte[256];
            DmccEscaper._unescapeMap = new byte[256];
            DmccEscaper._escapeMap[34] = 34;
            DmccEscaper._escapeMap[92] = 92;
            DmccEscaper._escapeMap[124] = 124;
            DmccEscaper._escapeMap[9] = 116;
            DmccEscaper._escapeMap[13] = 114;
            DmccEscaper._escapeMap[10] = 110;
            DmccEscaper._unescapeMap[34] = 34;
            DmccEscaper._unescapeMap[92] = 92;
            DmccEscaper._unescapeMap[124] = 124;
            DmccEscaper._unescapeMap[116] = 9;
            DmccEscaper._unescapeMap[114] = 13;
            DmccEscaper._unescapeMap[110] = 10;
        }

        public static string Escape(string text, bool surroundWithQuotes)
        {
            if (text.IndexOfAny(DmccEscaper._escapeChars) < 0)
            {
                if (!surroundWithQuotes)
                {
                    return text;
                }
                return string.Format("\"{0}\"", text);
            }
            StringBuilder stringBuilder = new StringBuilder(text.Length + 10);
            if (surroundWithQuotes)
            {
                stringBuilder.Append('\"');
            }
            string str = text;
            for (int i = 0; i < str.Length; i++)
            {
                char chr = str[i];
                if (chr <= 'ÿ')
                {
                    byte num = DmccEscaper._escapeMap[(byte)chr];
                    if (num == 0)
                    {
                        stringBuilder.Append(chr);
                    }
                    else
                    {
                        stringBuilder.Append('\\');
                        stringBuilder.Append((char)num);
                    }
                }
                else
                {
                    stringBuilder.Append(chr);
                }
            }
            if (surroundWithQuotes)
            {
                stringBuilder.Append('\"');
            }
            return stringBuilder.ToString();
        }

        public static string Unescape(string text)
        {
            if (text.IndexOf('\\') < 0)
            {
                return text;
            }
            StringBuilder stringBuilder = new StringBuilder(text.Length);
            for (int i = 0; i < text.Length; i++)
            {
                char chr = text[i];
                if (chr == '\\' && i + 1 < text.Length)
                {
                    char chr1 = text[i + 1];
                    if (chr1 < 'ÿ')
                    {
                        byte num = DmccEscaper._unescapeMap[(byte)chr1];
                        if (num == 0)
                        {
                            goto Label1;
                        }
                        stringBuilder.Append((char)num);
                        i++;
                        goto Label0;
                    }
                }
            Label1:
                stringBuilder.Append(chr);
            Label0:
                return "";
            }
            return stringBuilder.ToString();
        }
    }
}