using BepInEx;
using System.IO;
using System.Text.RegularExpressions;

namespace FromJianghuENMod
{
    public static class Helpers
    {
        public static readonly Regex cjkCharRegex = new Regex(@"\p{IsCJKUnifiedIdeographs}");
        public static bool IsChinese(string s)
        {
            return cjkCharRegex.IsMatch(s);
        }
        public static void DeleteFileIfExists(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        public static bool DoesMatchAny(this string s, params string[] patterns)
        {
            foreach (string pattern in patterns)
            {
                if (Regex.IsMatch(s, pattern))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
