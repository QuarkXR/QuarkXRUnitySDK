using HTC.VIVERegistryTool.Editor.Utils;
using System;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace HTC.VIVERegistryTool.Editor.Configs
{
    public class RegistrySettings : ScriptableObject
    {
        private const string RESOURCES_PATH = "RegistrySettings";

        private static RegistrySettings PrivateInstance;

        public string ProjectManifestPath;
        public bool AutoCheckEnabled = true;
        public RegistryInfo Registry;

        [NonSerialized] public string RegistryHost;
        [NonSerialized] public int RegistryPort;

        public static RegistrySettings Instance()
        {
            if (PrivateInstance == null)
            {
                PrivateInstance = Resources.Load<RegistrySettings>(RESOURCES_PATH);
                if (PrivateInstance)
                {
                    PrivateInstance.Init();
                }
            }

            return PrivateInstance;
        }

        public void SetAutoCheckEnabled(bool enabled)
        {
            AutoCheckEnabled = enabled;
            EditorUtility.SetDirty(this);
        }

        private void Init()
        {
            Match match = Regex.Match(Registry.Url, @"^https?:\/\/(.+?)(?::(\d+))?\/?$");
            RegistryHost = match.Groups[1].Value;

            RegistryPort = 80;
            if (int.TryParse(match.Groups[2].Value, out int port))
            {
                RegistryPort = port;
            }
        }
    }
}
