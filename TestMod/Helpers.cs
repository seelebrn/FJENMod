using BepInEx;
using LitJson;
using System.IO;
using System.Text.RegularExpressions;

namespace FromJianghuENMod
{
    public static class Helpers
    {
        public static readonly Regex cjkCharRegex = new(@"\p{IsCJKUnifiedIdeographs}");
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

        public static void Serialize<T>(T obj, string path)
        {
            if (obj == null)
            {
                return;
            }
            string json = JsonMapper.ToJson(obj);
            File.WriteAllText(path, json);
        }
        public static T Deserialize<T>(string path)
        {
            if (!File.Exists(path))
            {
                return default;
            }
            string json = File.ReadAllText(path);
            return JsonMapper.ToObject<T>(json);
        }
    }
}
