using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.XR.Management;
using UnityEngine;

using Wave.XR.Loader;

namespace Wave.Profiler.RenderDoc
{
	class RendeDocBuildProcessor : IPreprocessBuildWithReport, IPostprocessBuildWithReport
	{
		const string key = "RenderDocBPKey";
		const string menuPath = "Wave/Profiler/Build with RenderDoc";
		static bool isBuildWithRenderDoc = false;

		[InitializeOnLoadMethod]
		public static void OnLoadCheck()
		{
			isBuildWithRenderDoc = EditorPrefs.GetBool(key, false);
			Menu.SetChecked(menuPath, isBuildWithRenderDoc);
		}

		[MenuItem(menuPath)]
		public static void BuildWithRenderDoc()
		{
			isBuildWithRenderDoc = !EditorPrefs.GetBool(key, false);
			EditorPrefs.SetBool(key, isBuildWithRenderDoc);
			Menu.SetChecked(menuPath, isBuildWithRenderDoc);
		}

		public int callbackOrder { get { return 0; } }

		public static void CreateDirectoryRecursivly(string path)
		{
			if (!Directory.Exists(path))
			{
				CreateDirectoryRecursivly(Path.GetDirectoryName(path));
				Directory.CreateDirectory(path);
			}
			else
			{
				return;
			}
		}

		static bool CheckIsBuildingWave()
		{
			var androidGenericSettings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Android);
			if (androidGenericSettings == null)
				return false;

			var androidXRMSettings = androidGenericSettings.AssignedSettings;
			if (androidXRMSettings == null)
				return false;

			var loaders = androidXRMSettings.loaders;
			foreach (var loader in loaders)
			{
				if (loader.GetType() == typeof(WaveXRLoader))
				{
					return true;
				}
			}
			return false;
		}

		//[MenuItem("Wave/Do Copy")] // for test
		private static void DoCopy()
		{
			bool build32 = PlayerSettings.Android.targetArchitectures == AndroidArchitecture.ARMv7;
			bool build64 = PlayerSettings.Android.targetArchitectures == AndroidArchitecture.ARM64;
			bool copied = false;

			if (build32 && build64)
			{
				Debug.LogError("RenderDoC: Can't build 64bit and 32bit together.  Forced use 64bit.");
				build32 = false;
			}

			try
			{
				AssetDatabase.StartAssetEditing();
				string src = "", dest = "";
				if (build32)
				{
					src = "Packages/com.htc.upm.wave.xrsdk/Runtime/Android/armeabi-v7a/libVkLayer_GLES_RenderDoc.so";
					dest = "Assets/Plugins/Android/libVkLayer_GLES_RenderDoc.so";
				} else if (build64)
				{
					src = "Packages/com.htc.upm.wave.xrsdk/Runtime/Android/arm64-v8a/libVkLayer_GLES_RenderDoc.so";
					dest = Path.Combine("Assets/Plugins/Android/libVkLayer_GLES_RenderDoc.so");
				}

				CreateDirectoryRecursivly(Path.GetDirectoryName(dest));
				if (File.Exists(src) && !File.Exists(dest))
				{
					src = Path.GetFullPath(src);
					dest = Path.GetFullPath(dest);
					Debug.Log("Build with RenderDoc.\nCopy library from\n" + src + "\nto\n" + dest);
					FileUtil.CopyFileOrDirectory(src, dest);
					copied = true;
				}
			}
			finally
			{
				AssetDatabase.StopAssetEditing();
				if (copied)
					AssetDatabase.Refresh(ImportAssetOptions.ImportRecursive);
			}
		}

		public void OnPreprocessBuild(BuildReport report)
		{
			if (report.summary.platform != BuildTarget.Android
				|| !isBuildWithRenderDoc
				|| !CheckIsBuildingWave())
				return;

			DoCopy();
		}

		public void OnPostprocessBuild(BuildReport report)
		{
			if (report.summary.platform != BuildTarget.Android
				|| !isBuildWithRenderDoc
				|| !CheckIsBuildingWave())
				return;

			bool build32 = PlayerSettings.Android.targetArchitectures == AndroidArchitecture.ARMv7;
			bool build64 = PlayerSettings.Android.targetArchitectures == AndroidArchitecture.ARM64;
			bool deleted = false;

			try
			{
				AssetDatabase.StartAssetEditing();
				string target = "Assets/Plugins/Android/libVkLayer_GLES_RenderDoc.so";
				if (File.Exists(target))
				{
					AssetDatabase.DeleteAsset(target);
					deleted = true;
				}
			}
			finally
			{
				AssetDatabase.StopAssetEditing();
				if (deleted)
					AssetDatabase.Refresh();
			}
		}
	}

}
