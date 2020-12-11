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
using System.Collections;
using UnityEngine.EventSystems;
using Wave.Native;

namespace Wave.Essence.InputModule.Demo
{
	[DisallowMultipleComponent]
	public class GazeCubeHandler : MonoBehaviour,
		IPointerDownHandler
	{
		private const string LOG_TAG = "Wave.Essence.InputModule.Demo.GazeCubeHandler";
		private void DEBUG(string msg)
		{
			if (Log.EnableDebugLog)
				Log.d(LOG_TAG, msg, true);
		}

		private void TeleportRandomly()
		{
			Vector3 direction = Random.onUnitSphere;
			direction.y = Mathf.Clamp(direction.y, 0.5f, 1f);
			direction.z = Mathf.Clamp(direction.z, 1f, 2f);
			float distance = 2 * UnityEngine.Random.value + 1.5f;
			transform.localPosition = direction * distance;
		}

		#region Override event handling function
		public void OnPointerDown(PointerEventData eventData)
		{
			DEBUG("OnPointerDown()");
			TeleportRandomly();
		}
		#endregion

		public void OnShowCube()
		{
			Log.d(LOG_TAG, "OnShowButton");
			transform.gameObject.SetActive(true);
		}

		public void OnHideCube()
		{
			Log.d(LOG_TAG, "OnHideButton");
			transform.gameObject.SetActive(false);
		}

		public void OnTrigger()
		{
			TeleportRandomly();
		}
	}
}
