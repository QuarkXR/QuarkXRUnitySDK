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
	public class RaycastResultProvider
	{
		private const string LOG_TAG = "Wave.RaycastResultProvider";
		private void DEBUG(string msg)
		{
			if (Log.EnableDebugLog)
				Log.d(LOG_TAG, msg);
		}

		class RaycastResultStorage
		{
			public XR_Hand hand { get; set; }
			public GameObject resultObject { get; set; }
			public Vector3 worldPosition { get; set; }

			public RaycastResultStorage(XR_Hand type, GameObject target, Vector3 position)
			{
				this.resultObject = null;
				this.worldPosition = Vector3.zero;
			}
		}

		private List<RaycastResultStorage> raycastResults = new List<RaycastResultStorage>();
		private XR_Hand[] resultHandList = new XR_Hand[] {
			XR_Hand.Dominant,
			XR_Hand.NonDominant
		};

		private static RaycastResultProvider m_Instance = null;
		public static RaycastResultProvider Instance
		{
			get
			{
				if (m_Instance == null)
					m_Instance = new RaycastResultProvider();
				return m_Instance;
			}
		}

		private RaycastResultProvider()
		{
			for (int i = 0; i < resultHandList.Length; i++)
				raycastResults.Add(new RaycastResultStorage(resultHandList[i], null, Vector3.zero));
		}

		public void SetRaycastResult(XR_Hand hand, GameObject resultObject, Vector3 worldPosition)
		{
			for (int i = 0; i < resultHandList.Length; i++)
			{
				if (resultHandList[i] == hand)
				{
					raycastResults[i].resultObject = resultObject;
					raycastResults[i].worldPosition = worldPosition;
					break;
				}
			}
		}

		public GameObject GetRaycastResultObject(XR_Hand hand)
		{
			int index = 0;
			for (int i = 0; i < resultHandList.Length; i++)
			{
				if (resultHandList[i] == hand)
				{
					index = i;
					break;
				}
			}

			return raycastResults[index].resultObject;
		}

		public Vector3 GetRaycastResultWorldPosition(XR_Hand hand)
		{
			int index = 0;
			for (int i = 0; i < resultHandList.Length; i++)
			{
				if (resultHandList[i] == hand)
				{
					index = i;
					break;
				}
			}

			return raycastResults[index].worldPosition;
		}
	}
}
