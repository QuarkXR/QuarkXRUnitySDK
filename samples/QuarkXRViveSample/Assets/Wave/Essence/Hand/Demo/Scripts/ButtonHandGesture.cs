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
using Wave.Essence.Events;
using Wave.Native;

namespace Wave.Essence.Hand.Demo
{
	[DisallowMultipleComponent]
	[RequireComponent(typeof(Button))]
	sealed class ButtonHandGesture : MonoBehaviour
	{
		private Button m_Button = null;
		private Text m_ButtonText = null;

		void Start()
		{
			m_Button = gameObject.GetComponent<Button>();
			m_ButtonText = gameObject.GetComponentInChildren<Text>();
		}

		HandManager.HandGestureStatus hand_gesture_status = HandManager.HandGestureStatus.NOT_START;
		void Update()
		{
			hand_gesture_status = HandManager.Instance.GetHandGestureStatus();
			if (m_ButtonText != null && m_Button != null)
			{
				if (hand_gesture_status == HandManager.HandGestureStatus.AVAILABLE)
				{
					m_Button.interactable = true;
					m_ButtonText.text = "Disable Hand Gesture";
				}
				else if (
				  hand_gesture_status == HandManager.HandGestureStatus.NOT_START ||
				  hand_gesture_status == HandManager.HandGestureStatus.START_FAILURE)
				{
					m_Button.interactable = true;
					m_ButtonText.text = "Enable Hand Gesture";
				}
				else
				{
					m_Button.interactable = false;
					m_ButtonText.text = "Processing Gesture";
				}
			}
		}

		void OnEnable()
		{
			GeneralEvent.Listen(HandManager.HAND_GESTURE_STATUS, OnGestureStatus);
		}

		void OnDisable()
		{
			GeneralEvent.Remove(HandManager.HAND_GESTURE_STATUS, OnGestureStatus);
		}

		private void OnGestureStatus(params object[] args)
		{
			HandManager.HandGestureStatus status = (HandManager.HandGestureStatus)args[0];
			Log.d("ButtonHandGesture", "Hand gesture status: " + status, true);
		}

		public void EnableHandGesture()
		{
			if (hand_gesture_status == HandManager.HandGestureStatus.AVAILABLE)
				HandManager.Instance.EnableHandGesture = false;
			else
				HandManager.Instance.EnableHandGesture = true;
		}
	}
}
