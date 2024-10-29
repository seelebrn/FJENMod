using System.Collections.Generic;
using System.IO;
using AssetsTools.NET;
using AssetsTools.NET.Extra;
using FromJianghuENMod;
using UnityEngine;

namespace TestMod
{
    internal class Dump
    {
        static public List<string> untranslated = new();
        public static void LoadAssetsFile(string filePath)
        {
            AssetsManager manager = new();
            manager.LoadClassPackage(Path.Combine(BepInEx.Paths.PluginPath, "classdata.tpk"));

            AssetsFileInstance afileInst = manager.LoadAssetsFile(filePath, true);
            AssetsFile afile = afileInst.file;

            manager.LoadClassDatabaseFromPackage(afile.Metadata.UnityVersion);
            manager.MonoTempGenerator = new MonoCecilTempGenerator(BepInEx.Paths.ManagedPath);
            foreach (AssetFileInfo goInfo in afile.GetAssetsOfType(AssetClassID.MonoBehaviour))
            {
                try
                {
                    AssetTypeValueField texBase = manager.GetBaseField(afileInst, goInfo);
                    string text = texBase["m_Text"].AsString;
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
            AssetsManager manager = new();

            BundleFileInstance bunInst = manager.LoadBundleFile(filePath, true);
            AssetsFileInstance afileInst = manager.LoadAssetsFileFromBundle(bunInst, 0, false);
            manager.MonoTempGenerator = new MonoCecilTempGenerator(BepInEx.Paths.ManagedPath);

            AssetsFile afile = afileInst.file;

            foreach (AssetFileInfo goInfo in afile.GetAssetsOfType(AssetClassID.MonoBehaviour))
            {
                try
                {
                    AssetTypeValueField texBase = manager.GetBaseField(afileInst, goInfo);
                    string text = texBase["m_Text"].AsString;
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
