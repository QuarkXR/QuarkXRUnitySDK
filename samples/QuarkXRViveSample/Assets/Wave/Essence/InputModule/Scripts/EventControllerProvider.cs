// "Wave SDK 
// © 2020 HTC Corporation. All Rights Reserved.
//
// Unless otherwise required by copyright law and practice,
// upon the execution of HTC SDK license agreement,
// HTC grants you access to and use of the Wave SDK(s).
// You shall fully comply with all of HTC’s SDK license agreement terms and
// conditions signed by you and all SDK and API requirements,
// specifications, and documentation provided by HTC to You."

using System.Collections.Generic;
using UnityEngine;
using Wave.Native;

namespace Wave.Essence.InputModule
{
	sealed class EventControllerProvider
	{
		const string LOG_TAG = "Wave.Essence.InputModule.EventControllerProvider";
		private void DEBUG(string msg)
		{
			if (Log.EnableDebugLog)
				Log.d(LOG_TAG, msg, true);
		}

		class ControllerStorage
		{
			public XR_Hand hand { get; set; }
			public GameObject controller { get; set; }

			public ControllerStorage(XR_Hand type, GameObject con)
			{
				hand = type;
				controller = con;
			}
		}
		private List<ControllerStorage> mControllers = new List<ControllerStorage>();
		public static XR_Hand[] ControllerTypes = new XR_Hand[] {
			XR_Hand.Dominant,
			XR_Hand.NonDominant
		};

		private static EventControllerProvider m_Instance = null;
		public static EventControllerProvider Instance
		{
			get
			{
				if (m_Instance == null)
					m_Instance = new EventControllerProvider();
				return m_Instance;
			}
		}

		private EventControllerProvider()
		{
			for (int i = 0; i < ControllerTypes.Length; i++)
				mControllers.Add(new ControllerStorage(ControllerTypes[i], null));
		}

		public void SetEventController(XR_Hand hand, GameObject con)
		{
			DEBUG("SetEventController() " + hand + ", controller: " + (con != null ? con.name : "null"));

			for (int i = 0; i < ControllerTypes.Length; i++)
			{
				if (ControllerTypes[i] == hand)
				{
					mControllers[i].controller = con;
					break;
				}
			}
		}

		public GameObject GetEventController(XR_Hand hand)
		{
			int index = 0;
			for (int i = 0; i < ControllerTypes.Length; i++)
			{
				if (ControllerTypes[i] == hand)
				{
					index = i;
					break;
				}
			}

			return mControllers[index].controller;
		}
	}
}
