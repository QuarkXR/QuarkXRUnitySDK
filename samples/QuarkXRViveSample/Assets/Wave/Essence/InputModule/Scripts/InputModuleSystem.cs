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
using Wave.Native;
#if UNITY_EDITOR
using Wave.Essence.Editor;
#endif

namespace Wave.Essence.InputModule
{
	[DisallowMultipleComponent]
	sealed class InputModuleSystem : MonoBehaviour
	{
		const string LOG_TAG = "Wave.Essence.InputModule.InputModuleSystem";
		private void DEBUG(string msg)
		{
			if (Log.EnableDebugLog)
				Log.d(LOG_TAG, msg, true);
		}

		#region Customized Settings
		[Tooltip("True for only one input from one controller. False for double inputs from double controllers.")]
		[SerializeField]
		private bool m_SingleInput = true;
		public bool SingleInput { get { return m_SingleInput; } set { m_SingleInput = value; } }

		[Tooltip("If SingleInput is set, PrimaryInput will be used to decide the main input controller.")]
		/// <summary>
		/// 1. Used by EventControllerSetter to show/hide the controller.
		/// 2. Used by ControllerInputModule to enable/disable the event.
		/// </summary>
		[SerializeField]
		private XR_Hand m_PrimaryInput = XR_Hand.Dominant;
		public XR_Hand PrimaryInput { get { return m_PrimaryInput; } set { m_PrimaryInput = value; } }
		#endregion

		// Use a private m_Instance due to .Net3.5 or newer does NOT support default initializer.
		private static InputModuleSystem m_Instance = null;
		public static InputModuleSystem Instance
		{
			get
			{
				if (m_Instance == null)
				{
					var gameObject = new GameObject("InputModuleSystem");
					m_Instance = gameObject.AddComponent<InputModuleSystem>();
					// This object should survive all scene transitions.
					DontDestroyOnLoad(m_Instance);
				}
				return m_Instance;
			}
		}


		private bool triggerPressed = false;
		private bool IsTriggered(XR_Device device)
		{
			bool value = false;

#if UNITY_EDITOR
			if (Application.isEditor)
			{
				return DummyButton.GetStatus(device, WVR_InputId.WVR_InputId_Alias1_Trigger, WVR_InputType.WVR_InputType_Button, DummyButton.ButtonState.Press);
			}
			else
#endif
			{
				value = WXRDevice.KeyDown(device, XR_BinaryButton.TriggerPress);
			}

			if (triggerPressed != value)
			{
				triggerPressed = value;
				if (triggerPressed)
					return true;
			}

			return false;
		}

		private void SetFocusHand()
		{
			if ((m_PrimaryInput == XR_Hand.Dominant) && IsTriggered(XR_Device.NonDominant))
			{
				m_PrimaryInput = XR_Hand.NonDominant;
				DEBUG("The focus hand is set to " + m_PrimaryInput);
				Interop.WVR_SetFocusedController(WVR_DeviceType.WVR_DeviceType_Controller_Left);
			}
			if ((m_PrimaryInput == XR_Hand.NonDominant) && IsTriggered(XR_Device.Dominant))
			{
				m_PrimaryInput = XR_Hand.Dominant;
				DEBUG("The focus hand is set to " + m_PrimaryInput);
				Interop.WVR_SetFocusedController(WVR_DeviceType.WVR_DeviceType_Controller_Right);
			}
		}

		private void GetFocusHand()
		{
			WVR_DeviceType focus_dev = Interop.WVR_GetFocusedController();
			if (focus_dev == WVR_DeviceType.WVR_DeviceType_Controller_Right)
				m_PrimaryInput = XR_Hand.Dominant;
			if (focus_dev == WVR_DeviceType.WVR_DeviceType_Controller_Left)
				m_PrimaryInput = XR_Hand.NonDominant;
			DEBUG("GetFocusHand() Focus hand: " + m_PrimaryInput);
		}

		#region MonoBehaviour overrides
		private void Awake()
		{
			m_Instance = this;
		}

		void Start()
		{
			Log.i(LOG_TAG, "Start() Check the focus hand .");
			GetFocusHand();

		}

		void OnApplicationPause(bool pauseStatus)
		{
			Log.i(LOG_TAG, "OnApplicationPause() pauseStatus: " + pauseStatus, true);
			if (!pauseStatus)
			{
				Log.i(LOG_TAG, "OnApplicationPause() Check the focus hand in resume.");
				GetFocusHand();
			}
		}

		void Update()
		{
			SetFocusHand();
		}
		#endregion
	}
}
