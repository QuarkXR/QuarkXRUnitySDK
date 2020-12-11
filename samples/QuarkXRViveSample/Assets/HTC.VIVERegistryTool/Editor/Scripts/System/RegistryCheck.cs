using HTC.VIVERegistryTool.Editor.Configs;
using HTC.VIVERegistryTool.Editor.UI;
using HTC.VIVERegistryTool.Editor.Utils;
using UnityEditor;

namespace HTC.VIVERegistryTool.Editor.System
{
    [InitializeOnLoad]
    public class RegistryCheck
    {
        static RegistryCheck()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            EditorApplication.update += OnUpdate;
        }

        private static void OnUpdate()
        {
            if (EditorApplication.isUpdating)
            {
                return;
            }

            if (!RegistrySettings.Instance())
            {
                return;
            }

            if (RegistrySettings.Instance().AutoCheckEnabled && !ManifestUtils.CheckRegistryExists(RegistrySettings.Instance().Registry))
            { 
                RegistryUpdaterWindow.Open();
            }

            EditorApplication.update -= OnUpdate;
        }
    }
}