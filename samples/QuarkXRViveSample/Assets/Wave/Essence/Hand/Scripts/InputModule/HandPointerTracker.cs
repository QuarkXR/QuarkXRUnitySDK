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
using UnityEngine.EventSystems;
using Wave.Native;

namespace Wave.Essence.Hand
{
	[DisallowMultipleComponent]
	[RequireComponent(typeof(Camera), typeof(PhysicsRaycaster))]
	sealed class HandPointerTracker : MonoBehaviour
	{
		const string LOG_TAG = "Wave.Essence.Hand.HandPointerTracker";
		private void DEBUG(string msg)
		{
			if (Log.EnableDebugLog)
				Log.d(LOG_TAG, msg, true);
		}
		private static HandPointerTracker m_Instance = null;
		public static HandPointerTracker Instance { get { return m_Instance; } }

		private GameObject pointerObject = null;
		private HandSpotPointer m_HandSpotPointer = null;
		private bool ValidateParameters()
		{
			GameObject new_pointer = HandPointerProvider.Instance.GetHandPointer(HandManager.FocusedHand);
			if (new_pointer != null && !ReferenceEquals(pointerObject, new_pointer))
			{
				pointerObject = new_pointer;
				m_HandSpotPointer = pointerObject.GetComponent<HandSpotPointer>();
			}

			if (pointerObject == null || m_HandSpotPointer == null)
				return false;

			return true;
		}

		void Awake()
		{
			m_Instance = this;
			transform.localPosition = Vector3.zero;
		}

		void Start()
		{
			GetComponent<Camera>().enabled = false;
			DEBUG("Start() " + gameObject.name);
		}

		private Vector3 pointerPosition = Vector3.zero;
		private Vector3 lookDirection = Vector3.zero;
		void Update()
		{
			if (!ValidateParameters())
				return;

			pointerPosition = m_HandSpotPointer.GetPointerPosition();
			lookDirection = pointerPosition - transform.position;
			transform.rotation = Quaternion.LookRotation(lookDirection);
			//Debug.DrawRay (transform.position, lookDirection, Color.red);
		}

		public Camera GetPointerTrackerCamera()
		{
			return GetComponent<Camera>();
		}

		public PhysicsRaycaster GetPhysicsRaycaster()
		{
			return GetComponent<PhysicsRaycaster>();
		}
	}
}
