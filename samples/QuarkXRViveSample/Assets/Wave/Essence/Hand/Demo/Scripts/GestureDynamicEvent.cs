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
using Wave.Essence.Events;

namespace Wave.Essence.Hand.Demo
{
	[DisallowMultipleComponent]
	[RequireComponent(typeof(Text))]
	sealed class GestureDynamicEvent : MonoBehaviour
	{
		const string LOG_TAG = "Wave.Essence.Hand.Demo.GestureDynamicEvent";
		private void DEBUG(string msg)
		{
			if (Log.EnableDebugLog)
				Log.d(LOG_TAG, msg, true);
		}

		private Text m_Text = null;
		void Start()
		{
			m_Text = gameObject.GetComponent<Text>();
		}

		private bool mEnabled = false;
		void OnEnable()
		{
			if (!mEnabled)
			{
				GeneralEvent.Listen(HandManager.HAND_DYNAMIC_GESTURE_LEFT, onDynamicGestureHandle);
				GeneralEvent.Listen(HandManager.HAND_DYNAMIC_GESTURE_RIGHT, onDynamicGestureHandle);
				mEnabled = true;
			}
		}

		void OnDisable()
		{
			if (mEnabled)
			{
				GeneralEvent.Remove(HandManager.HAND_DYNAMIC_GESTURE_LEFT, onDynamicGestureHandle);
				GeneralEvent.Remove(HandManager.HAND_DYNAMIC_GESTURE_RIGHT, onDynamicGestureHandle);
				mEnabled = false;
			}
		}

		private void onDynamicGestureHandle(params object[] args)
		{
			WVR_EventType dynamic_gesture = (WVR_EventType)args[0];
			DEBUG("onDynamicGestureHandle() " + dynamic_gesture);
			if (m_Text != null)
				m_Text.text = "Dynamic: " + dynamic_gesture.ToString();
		}
	}
}
