// "Wave SDK 
// © 2020 HTC Corporation. All Rights Reserved.
//
// Unless otherwise required by copyright law and practice,
// upon the execution of HTC SDK license agreement,
// HTC grants you access to and use of the Wave SDK(s).
// You shall fully comply with all of HTC’s SDK license agreement terms and
// conditions signed by you and all SDK and API requirements,
// specifications, and documentation provided by HTC to You."

using UnityEngine;
using UnityEngine.UI;
using Wave.Native;

namespace Wave.Essence.Samples.CameraTextureTest
{
	[RequireComponent(typeof(Text))]
	public class CameraTextureTest_Permission : MonoBehaviour
	{
		private static string LOG_TAG = "CameraTextureTest_Permission";

		private PermissionManager pmInstance = null;
		private Text textField;
		private bool permission_granted = false;
		// Use this for initialization
		void Start()
		{
			Log.d(LOG_TAG, "get instance at start");
			pmInstance = PermissionManager.instance;
			textField = GetComponent<Text>();
			permission_granted = pmInstance.isPermissionGranted("android.permission.CAMERA");
			if (permission_granted)
			{
				textField.text = "";
			}
			else
			{
				textField.text = "Warning : \n This APP was not granted android.permission.CAMERA yet. \n The camera will not start.";
			}
		}

		// Update is called once per frame
		void Update()
		{

		}

		void OnApplicationPause(bool pauseStatus)
		{
			permission_granted = pmInstance.isPermissionGranted("android.permission.CAMERA");
			if (permission_granted)
			{
				textField.text = "";
			}
			else
			{
				textField.text = "Warning : \n This APP was not granted android.permission.CAMERA yet. \n The camera will not start.";
			}
		}
	}
}
