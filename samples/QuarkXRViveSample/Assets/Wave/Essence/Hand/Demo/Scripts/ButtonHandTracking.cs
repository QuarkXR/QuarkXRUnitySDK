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
	[RequireComponent(typeof(Button))]
	sealed class ButtonHandTracking : MonoBehaviour
	{
		private Button m_Button = null;
		private Text m_ButtonText = null;

		// Use this for initialization
		void Start()
		{
			m_Button = gameObject.GetComponent<Button>();
			m_ButtonText = gameObject.GetComponentInChildren<Text>();
		}

		HandManager.HandTrackingStatus hand_tracking_status = HandManager.HandTrackingStatus.NOT_START;
		void Update()
		{
			hand_tracking_status = HandManager.Instance.GetHandTrackingStatus();
			if (m_ButtonText != null && m_Button != null)
			{
				if (hand_tracking_status == HandManager.HandTrackingStatus.AVAILABLE)
				{
					m_Button.interactable = true;
					m_ButtonText.text = "Disable Hand Tracking";
				}
				else if (
				  hand_tracking_status == HandManager.HandTrackingStatus.NOT_START ||
				  hand_tracking_status == HandManager.HandTrackingStatus.START_FAILURE)
				{
					m_Button.interactable = true;
					m_ButtonText.text = "Enable Hand Tracking";
				}
				else
				{
					m_Button.interactable = false;
					m_ButtonText.text = "Processing Tracking";
				}
			}
		}

		void OnEnable()
		{
			GeneralEvent.Listen(HandManager.HAND_TRACKING_STATUS, OnTrackingStatus);
		}

		void OnDisable()
		{
			GeneralEvent.Remove(HandManager.HAND_TRACKING_STATUS, OnTrackingStatus);
		}

		private void OnTrackingStatus(params object[] args)
		{
			HandManager.HandTrackingStatus status = (HandManager.HandTrackingStatus)args[0];
			Log.d("ButtonHandTracking", "Hand tracking status: " + status, true);
		}

		public void EnableHandTracking()
		{
			if (hand_tracking_status == HandManager.HandTrackingStatus.AVAILABLE)
				HandManager.Instance.EnableHandTracking = false;
			else
				HandManager.Instance.EnableHandTracking = true;
		}
	}
}
