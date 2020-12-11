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

namespace Wave.Essence.Hand
{
	public class HandPointerProvider
	{
		private const string LOG_TAG = "Wave.Essence.Hand.HandPointerProvider";
		private void DEBUG(string msg)
		{
			if (Log.EnableDebugLog)
				Log.d(LOG_TAG, msg, true);
		}

		private class PointerStorage
		{
			public HandManager.HandType Hand { get; set; }
			public GameObject Pointer { get; set; }

			public PointerStorage(HandManager.HandType type, GameObject pointer)
			{
				Hand = type;
				Pointer = pointer;
			}
		}
		private List<PointerStorage> m_HandPointers = new List<PointerStorage>();
		private readonly HandManager.HandType[] s_HandTypes = new HandManager.HandType[] {
			HandManager.HandType.RIGHT,
			HandManager.HandType.LEFT
		};

		private static HandPointerProvider instance = null;
		public static HandPointerProvider Instance
		{
			get
			{
				if (instance == null)
					instance = new HandPointerProvider();
				return instance;
			}
		}

		private HandPointerProvider()
		{
			for (int i = 0; i < s_HandTypes.Length; i++)
				m_HandPointers.Add(new PointerStorage(s_HandTypes[i], null));
		}

		public void SetHandPointer(HandManager.HandType hand, GameObject pointer)
		{
			DEBUG("SetHandPointer() " + hand + ", pointer: " + (pointer != null ? pointer.name : "null"));

			for (int i = 0; i < s_HandTypes.Length; i++)
			{
				if (s_HandTypes[i] == hand)
				{
					m_HandPointers[i].Pointer = pointer;
					break;
				}
			}
		}

		public GameObject GetHandPointer(HandManager.HandType hand)
		{
			int index = 0;
			for (int i = 0; i < s_HandTypes.Length; i++)
			{
				if (s_HandTypes[i] == hand)
				{
					index = i;
					break;
				}
			}

			return m_HandPointers[index].Pointer;
		}
	}
}
