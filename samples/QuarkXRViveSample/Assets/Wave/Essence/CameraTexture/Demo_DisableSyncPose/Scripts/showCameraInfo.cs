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

namespace Wave.Essence.CameraTexture.Demo.DisableSyncPose
{
	[RequireComponent(typeof(Text))]
	public class showCameraInfo : MonoBehaviour
	{
		private static string LOG_TAG = "CameraTextureInfo";

		private Text textField;
		private bool cameraStarted = false;
		private bool isShow = false;
		string obj = "";

		// Use this for initialization
		void Start()
		{
			textField = GetComponent<Text>();
		}

		// Update is called once per frame
		void Update()
		{
			setCameraStarted();
			if (isShow == true)
			{
				if (cameraStarted == true)
				{
					obj = "Camera image fomat : " + CameraTextureManager.instance.getImageFormat() + "\n" + "Camera image type : "
					+ CameraTextureManager.instance.getImageType() + "\n" + "Camera image width : " + CameraTextureManager.instance.getImageWidth() + "\n" +
					"Camera image height : " + CameraTextureManager.instance.getImageHeight();
					textField.text = obj;
				}
				else
				{
					textField.text = "Camera is not started.";
				}
			}
		}

		public void ShowInfo()
		{
			if (!isShow)
			{
				if (cameraStarted == true)
				{
					string obj = "Camera image fomat : " + CameraTextureManager.instance.getImageFormat() + "\n" + "Camera image type : "
					+ CameraTextureManager.instance.getImageType() + "\n" + "Camera imege width : " + CameraTextureManager.instance.getImageWidth() + "\n" +
					"Camera imege height : " + CameraTextureManager.instance.getImageHeight();
					Log.d(LOG_TAG, " ShowInfo" + obj);
					textField.text = obj;
				}
				else
				{
					textField.text = "Camera is not started.";
				}
				isShow = true;
			}
			else
			{
				isShow = false;
				textField.text = "";
			}

		}

		private void setCameraStarted()
		{
			cameraStarted = CameraTextureManager.instance.isStarted;
		}
	}
}
