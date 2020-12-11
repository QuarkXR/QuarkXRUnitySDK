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
using UnityEngine.SceneManagement;
using System;
using Wave.Native;

namespace Wave.Essence.CameraTexture.Demo.DisableSyncPose
{
	public class CameraTextureTest : MonoBehaviour
	{
		public bool started = false;
		private Texture2D nativeTexture = null;
		private static string LOG_TAG = "CameraTextureTest";
		IntPtr textureid;
		private MeshRenderer meshrenderer;
		private PermissionManager pmInstance = null;
		private bool permission_granted = false;

		// Use this for initialization
		void Start()
		{
			started = false;
			nativeTexture = new Texture2D(1280, 400);
		}

		public void startCamera()
		{
			pmInstance = PermissionManager.instance;
			permission_granted = pmInstance.isPermissionGranted("android.permission.CAMERA");
			if (started == false && permission_granted)
			{
				SceneManager.sceneUnloaded += OnSceneUnloaded;
				CameraTextureManager.StartCameraCompletedDelegate += startCameraeCompleted;
				CameraTextureManager.UpdateCameraCompletedDelegate += updateTextureCompleted;
				CameraTextureManager.instance.startCamera(false);

				textureid = nativeTexture.GetNativeTexturePtr();
				meshrenderer = GetComponent<MeshRenderer>();
				meshrenderer.material.mainTexture = nativeTexture;
				Log.d(LOG_TAG, "startCamera");
			}
			else
			{
				Log.e(LOG_TAG, "startCamera fail, camera is already started or permissionGranted is failed");
			}
		}


		private void OnSceneUnloaded(Scene current)
		{
			Log.d(LOG_TAG, "OnSceneUnloaded and stopCamera: " + current);
			stopCamera();
		}

		public void stopCamera()
		{
			CameraTextureManager.UpdateCameraCompletedDelegate -= updateTextureCompleted;
			CameraTextureManager.StartCameraCompletedDelegate -= startCameraeCompleted;
			started = false;
			Log.d(LOG_TAG, "stopCamera");
			CameraTextureManager.instance.stopCamera();
		}

		void startCameraeCompleted(bool result)
		{
			Log.d(LOG_TAG, "startCameraeCompleted, start? " + result);
			started = result;
		}

		void updateTextureCompleted(IntPtr textureId, bool alreadyUpdate)
		{
			Log.d(LOG_TAG, "updateTextureCompleted, textureid: " + textureId + ", new content: " + alreadyUpdate);
			meshrenderer.material.mainTexture = nativeTexture;
		}

		void OnApplicationPause(bool pauseStatus)
		{
			if (pauseStatus)
			{
				if (started)
				{
					Log.d(LOG_TAG, "Pause(" + pauseStatus + ") and stop camera");
					stopCamera();
				}
			}
		}

		void OnDestroy()
		{
			Log.d(LOG_TAG, "OnDestroy stopCamera");
			stopCamera();
			nativeTexture = null;
		}

		// Update is called once per frame
		void Update()
		{
			if (started)
			{
				CameraTextureManager.instance.updateTexture(textureid);
			}
		}
	}
}
