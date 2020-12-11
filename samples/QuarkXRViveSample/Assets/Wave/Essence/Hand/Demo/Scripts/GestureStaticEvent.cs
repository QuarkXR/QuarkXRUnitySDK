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
	sealed class GestureStaticEvent : MonoBehaviour {
		const string LOG_TAG = "Wave.Essence.Hand.Demo.GestureStaticEvent";
		private void DEBUG(string msg)
		{
			if (Log.EnableDebugLog)
				Log.d (LOG_TAG, m_Hand + ", " + msg, true);
		}

		private readonly string[] s_HandGestures = {
			"Invalid",	//WVR_HandGestureType_Invalid         = 0,    /**< The gesture is invalid. */
			"Unknown",	//WVR_HandGestureType_Unknown         = 1,    /**< Unknow gesture type. */
			"Fist",		//WVR_HandGestureType_Fist            = 2,    /**< Represent fist gesture. */
			"Five",		//WVR_HandGestureType_Five            = 3,    /**< Represent five gesture. */
			"OK",		//WVR_HandGestureType_OK              = 4,    /**< Represent ok gesture. */
			"Thumbup",	//WVR_HandGestureType_ThumbUp         = 5,    /**< Represent thumb up gesture. */
			"IndexUp"	//WVR_HandGestureType_IndexUp         = 6,    /**< Represent index up gesture. */
		};

		[SerializeField]
		private HandManager.HandType m_Hand = HandManager.HandType.RIGHT;
		public HandManager.HandType Hand { get { return m_Hand; } set { m_Hand = value; } }

		private Text m_Text = null;

		private WVR_HandGestureType m_HandGesture = WVR_HandGestureType.WVR_HandGestureType_Invalid;
		private void onStaticGestureHandle(params object[] args)
		{
			m_HandGesture = (WVR_HandGestureType)args[0];
		}

		#region MonoBehaviour Overrides
		void Start()
		{
			m_Text = gameObject.GetComponent<Text>();
		}

		private bool mEnabled = false;
		void OnEnable()
		{
			if (!mEnabled)
			{
				if (m_Hand == HandManager.HandType.LEFT)
					GeneralEvent.Listen (HandManager.HAND_STATIC_GESTURE_LEFT, onStaticGestureHandle);
				if (m_Hand == HandManager.HandType.RIGHT)
					GeneralEvent.Listen (HandManager.HAND_STATIC_GESTURE_RIGHT, onStaticGestureHandle);
				mEnabled = true;
			}
		}

		void OnDisable()
		{
			if (mEnabled)
			{
				if (m_Hand == HandManager.HandType.LEFT)
					GeneralEvent.Remove (HandManager.HAND_STATIC_GESTURE_LEFT, onStaticGestureHandle);
				if (m_Hand == HandManager.HandType.RIGHT)
					GeneralEvent.Remove (HandManager.HAND_STATIC_GESTURE_RIGHT, onStaticGestureHandle);
				mEnabled = false;
			}
		}

		void Update()
		{
			if (m_Text == null || HandManager.Instance == null)
				return;

			m_Text.text = m_Hand + " Gesture: " + s_HandGestures[(int)m_HandGesture];
		}
		#endregion
	}
}
