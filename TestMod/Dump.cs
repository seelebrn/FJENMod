using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AssetsTools.NET;
using AssetsTools.NET.Extra;
using FromJianghuENMod;
using UnityEngine;

namespace TestMod
{
    internal class Dump
    {
        static public List<string> untranslated = new List<string>();
        public static void LoadAssetsFile(string filePath)
        {
            var manager = new AssetsManager();
            manager.LoadClassPackage(Path.Combine(BepInEx.Paths.PluginPath, "classdata.tpk"));

            var afileInst = manager.LoadAssetsFile(filePath, true);
            var afile = afileInst.file;

            manager.LoadClassDatabaseFromPackage(afile.Metadata.UnityVersion);
            manager.MonoTempGenerator = new MonoCecilTempGenerator(BepInEx.Paths.ManagedPath);
            foreach (var goInfo in afile.GetAssetsOfType(AssetClassID.MonoBehaviour))
            {
                
                try
                {
                    var texBase = manager.GetBaseField(afileInst, goInfo);
                    var text = texBase["m_Text"].AsString;
                    Debug.Log($"Found file in " + filePath + " : " + "Text : " + text);
                    if (Helpers.IsChinese(text))
                    {
                        if (!untranslated.Contains(text) && !text.Contains("_"))
                        {
                            untranslated.Add(text);
                        }
                    }
                }
                catch
                {

                }


            }
        }
        public static void LoadAssetBundles(string filePath)
        {
            var manager = new AssetsManager();

            var bunInst = manager.LoadBundleFile(filePath, true);
            var afileInst = manager.LoadAssetsFileFromBundle(bunInst, 0, false);
            manager.MonoTempGenerator = new MonoCecilTempGenerator(BepInEx.Paths.ManagedPath);

            var afile = afileInst.file;

            foreach (var goInfo in afile.GetAssetsOfType(AssetClassID.MonoBehaviour))
            {
                try
                {
                    var texBase = manager.GetBaseField(afileInst, goInfo);
                    var text = texBase["m_Text"].AsString;
                    Debug.Log($"Found file in " + filePath + " : " + "Text : " + text);
                    if(Helpers.IsChinese(text))
                    {
                        if(!untranslated.Contains(text) && !text.Contains("_"))
                        {
                            untranslated.Add(text);
                        }
                    }
                }
                catch
                {

                }

            }
        }

    }
}
