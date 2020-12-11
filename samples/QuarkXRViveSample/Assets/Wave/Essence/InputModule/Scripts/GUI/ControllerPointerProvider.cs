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
	sealed class ControllerPointerProvider
	{
		const string LOG_TAG = "Wave.ControllerPointerProvider";
		private void DEBUG(string msg)
		{
			if (Log.EnableDebugLog)
				Log.d(LOG_TAG, msg, true);
		}

		private class PointerStorage
		{
			public XR_Hand hand { get; set; }
			public GameObject pointerObject { get; set; }

			public PointerStorage(XR_Hand type, GameObject pointer)
			{
				hand = type;
				pointerObject = pointer;
			}
		}
		private List<PointerStorage> controllerPointers = new List<PointerStorage>();
		private XR_Hand[] pointerHandList = new XR_Hand[] {
			XR_Hand.Dominant,
			XR_Hand.NonDominant
		};

		private static ControllerPointerProvider instance = null;
		public static ControllerPointerProvider Instance
		{
			get
			{
				if (instance == null)
					instance = new ControllerPointerProvider();
				return instance;
			}
		}

		private ControllerPointerProvider()
		{
			for (int i = 0; i < pointerHandList.Length; i++)
				controllerPointers.Add(new PointerStorage(pointerHandList[i], null));
		}

		public void SetControllerPointer(XR_Hand hand, GameObject pointer)
		{
			DEBUG("SetControllerPointer() " + hand + ", pointer: " + (pointer != null ? pointer.name : "null"));

			for (int i = 0; i < pointerHandList.Length; i++)
			{
				if (pointerHandList[i] == hand)
				{
					controllerPointers[i].pointerObject = pointer;
					break;
				}
			}
		}

		public GameObject GetControllerPointer(XR_Hand hand)
		{
			int index = 0;
			for (int i = 0; i < pointerHandList.Length; i++)
			{
				if (pointerHandList[i] == hand)
				{
					index = i;
					break;
				}
			}

			return controllerPointers[index].pointerObject;
		}
	}
}
