using ColossalFramework;
using ColossalFramework.Plugins;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using UnityEngine;

namespace CSL_Traffic
{
    static class FileManager
    {
        static readonly ulong WORKSHOP_ID = 0L;

        public enum Folder
        {
            Textures,
            Roads,
            SmallRoad,
            LargeRoad,
            PedestrianRoad,
            Props,
            UI
        }

        static readonly string MOD_PATH = FindModPath(); 

        static readonly Dictionary<Folder, string> sm_relativeTextureFolderPaths = new Dictionary<Folder, string>()
        {
            {Folder.Textures,       "Textures/"},
            {Folder.Roads,          "Textures/Roads/"},
            {Folder.SmallRoad,      "Textures/Roads/SmallRoad/"},
            {Folder.LargeRoad,      "Textures/Roads/LargeRoad/"},
            {Folder.PedestrianRoad, "Textures/Roads/PedestrianRoad/"},
            {Folder.Props,          "Textures/Props/"},
            {Folder.UI,             "Textures/UI/"},
        };

        static Dictionary<string, byte[]> sm_cachedFiles = new Dictionary<string, byte[]>();

        static string FindModPath()
        {
            PluginManager.PluginInfo plugin = Singleton<PluginManager>.instance.GetPluginsInfo().FirstOrDefault(p => p.name == "Traffic Manager + Improved AI" || p.publishedFileID.AsUInt64 == WORKSHOP_ID);
            if (plugin != null)
                return plugin.modPath;
            else
                Debug.Log("Cannot find plugin path.");

            return null;
        }

        public static bool GetTextureBytes(string fileName, Folder folder, out byte[] bytes)
        {
            return GetTextureBytes(fileName, folder, false, out bytes);
        }

        public static bool GetTextureBytes(string fileName, Folder folder, bool skipCache, out byte[] bytes)
        {
            bytes = null;
            string filePath = GetFilePath(fileName, folder);
            if (filePath == null || !File.Exists(filePath))
            {
#if DEBUG
                Debug.Log("Cannot find texture file at " + filePath);
#endif
                return false;
            }

            if (!skipCache && sm_cachedFiles.TryGetValue(filePath, out bytes))
                return true;

            try
            {
                bytes = File.ReadAllBytes(filePath);
            }
            catch (Exception e)
            {
                Debug.Log("Unexpected " + e.GetType().Name + " reading texture file at " + filePath);
                return false;
            }

            sm_cachedFiles[filePath] = bytes;
            return true;
        }

        public static string GetFilePath(string fileName, Folder folder)
        {
            if (MOD_PATH == null)
                return null;
            
            string relativeFolderPath;
            if (!sm_relativeTextureFolderPaths.TryGetValue(folder, out relativeFolderPath))
                return null;

            string path = MOD_PATH;
            path = Path.Combine(path, relativeFolderPath);
            return Path.Combine(path, fileName);
        }

        public static TextureInfo[] GetTextureIndex()
        {
            TextureInfo[] textureIndex;
            string path = GetFilePath("TextureIndex.xml", Folder.Textures);
            if (path == null)
                return null;

            try
            {
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(TextureInfo[]));
                using (StreamReader streamReader = new StreamReader(path))
                {
                    textureIndex = (TextureInfo[])xmlSerializer.Deserialize(streamReader);
                }
            }
            catch (FileNotFoundException)
            {
                // No texture index
                Debug.Log("No texture index found.");
                return null;
            }
            catch (Exception e)
            {
                Debug.Log("Unexpected " + e.GetType().Name + " loading texture index: " + e.Message + "\n" + e.StackTrace);
                return null;
            }

            return textureIndex;
        }

        public static void ClearCache()
        {
            sm_cachedFiles.Clear();
        }

        public class TextureInfo
        {
            [XmlAttribute]
            public string name;

            // normal
            public string mainTex = "";
            public string aprTex = "";
            public string xysTex = "";
            public string aciTex = "";
            public string lodMainTex = "";
            public string lodAprTex = "";
            public string lodXysTex = "";
            public string lodAciTex = "";

            // bus
            public string mainTexBus = "";
            public string aprTexBus = "";
            public string xysTexBus = "";
            public string aciTexBus = "";
            public string lodMainTexBus = "";
            public string lodAprTexBus = "";
            public string lodXysTexBus = "";
            public string lodAciTexBus = "";

            // busBoth
            public string mainTexBusBoth = "";
            public string aprTexBusBoth = "";
            public string xysTexBusBoth = "";
            public string aciTexBusBoth = "";
            public string lodMainTexBusBoth = "";
            public string lodAprTexBusBoth = "";
            public string lodXysTexBusBoth = "";
            public string lodAciTexBusBoth = "";

            // node
            public string mainTexNode = "";
            public string aprTexNode = "";
            public string xysTexNode = "";
            public string aciTexNode = "";
            public string lodMainTexNode = "";
            public string lodAprTexNode = "";
            public string lodXysTexNode = "";
            public string lodAciTexNode = "";
        }
    }
}
