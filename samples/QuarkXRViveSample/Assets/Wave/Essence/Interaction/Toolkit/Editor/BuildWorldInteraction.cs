using UnityEditor;
using UnityEngine;
using Wave.XR;
using Wave.XR.Settings;

namespace Wave.Essence.Interaction.Toolkit.Editor
{
	public class BuildWorldInteraction
	{
		public struct BuildSettingsBackup
		{
			public string companyName;
			public string productName;
			public string applicationIdentifier;
			public AndroidArchitecture targetArchitectures;
		}

		static BuildSettingsBackup BackupBuildSettings()
		{
			BuildSettingsBackup backup = new BuildSettingsBackup
			{
				companyName = PlayerSettings.companyName,
				productName = PlayerSettings.productName,
				applicationIdentifier = PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android),
				targetArchitectures = PlayerSettings.Android.targetArchitectures,
			};
			return backup;
		}

		static void RestorePlayerSettings(BuildSettingsBackup backup)
		{
			PlayerSettings.companyName = backup.companyName;
			PlayerSettings.productName = backup.productName;
			PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, backup.applicationIdentifier);
			PlayerSettings.Android.targetArchitectures = backup.targetArchitectures;
		}

		static void SetPlayerSettings(bool is32bits = false)
		{
			PlayerSettings.companyName = "HTC Corp.";
			PlayerSettings.productName = "WorldInteraction";
			PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.htc.upm.wave.worldinteraction");
			if (!is32bits)
				PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
			else
				PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7;
		}

		static string[] GetScenes()
		{
			return new string[] {
				"Assets/Wave/Essence/Interaction/Toolkit/Demo/WorldInteraction.unity",
			};
		}

		static void BuildAndroid(string path, bool is32bits = false)
		{
			var backup = BackupBuildSettings();
			SetPlayerSettings(is32bits);

			try
			{
				BuildPipeline.BuildPlayer(GetScenes(), path, BuildTarget.Android, BuildOptions.None);
			}
			finally
			{
				RestorePlayerSettings(backup);
			}
		}

		//[MenuItem("Wave/Build WorldInteraction/32 bits", priority = 303)]
		public static void BuildApkX32()
		{
			if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
				return;
			string path = Application.dataPath + "/../WorldInteraction_x32.apk";

			WaveXRSettings settings;
			EditorBuildSettings.TryGetConfigObject(Constants.k_SettingsKey, out settings);
			settings.preferedStereoRenderingPath = WaveXRSettings.StereoRenderingPath.SinglePass;
			settings.useDoubleWidth = false;

			BuildAndroid(path, false);
		}

		//[MenuItem("Wave/Build WorldInteraction/64 bits", priority = 303)]
		public static void BuildApkX64()
		{
			if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
				return;
			string path = Application.dataPath + "/../WorldInteraction_x64.apk";

			WaveXRSettings settings;
			EditorBuildSettings.TryGetConfigObject(Constants.k_SettingsKey, out settings);
			settings.preferedStereoRenderingPath = WaveXRSettings.StereoRenderingPath.SinglePass;
			settings.useDoubleWidth = false;

			BuildAndroid(path, true);
		}

	}
}
