using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR;
using Wave.Native;

namespace Wave.Essence.InputModule
{
	[DisallowMultipleComponent]
	[RequireComponent(typeof(Camera), typeof(PhysicsRaycaster))]
	sealed class EventControllerSetter : MonoBehaviour
	{
		const string LOG_TAG = "Wave.Essence.InputModule.EventControllerSetter";
		private void DEBUG(string msg)
		{
			if (Log.EnableDebugLog)
				Log.d(LOG_TAG, m_ControllerType + " " + msg, true);
		}

		[SerializeField]
		private XR_Hand m_ControllerType = XR_Hand.Dominant;
		public XR_Hand ControllerType { get { return m_ControllerType; } set { m_ControllerType = value; } }

		private GameObject beamObject = null;
		private ControllerBeam m_Beam = null;
		private GameObject pointerObject = null;
		private ControllerPointer m_Pointer = null;

		private List<GameObject> children = new List<GameObject>();
		private int childrenCount = 0;
		private List<bool> childrenStates = new List<bool>();
		private void CheckChildrenObjects()
		{
			if (childrenCount != transform.childCount)
			{
				childrenCount = transform.childCount;
				children.Clear();
				childrenStates.Clear();
				for (int i = 0; i < childrenCount; i++)
				{
					children.Add(transform.GetChild(i).gameObject);
					childrenStates.Add(transform.GetChild(i).gameObject.activeSelf);
					DEBUG("CheckChildrenObjects() " + gameObject.name + " has child: " + children[i].name + ", active? " + childrenStates[i]);
				}
			}
		}
		private void ForceActivateTargetObjects(bool active)
		{
			for (int i = 0; i < children.Count; i++)
			{
				if (children[i] == null)
					continue;

				if (childrenStates[i])
				{
					DEBUG("ForceActivateTargetObjects() " + (active ? "Activate" : "Deactivate") + " " + children[i].name);
					children[i].SetActive(active);
				}
			}
		}

		private bool hasFocus = false;
		private bool m_ControllerActive = true;

		private bool mEnabled = false;
		void OnEnable()
		{
			if (!mEnabled)
			{
				GetComponent<Camera>().enabled = false;

				// Add a beam.
				beamObject = new GameObject("Beam" + m_ControllerType.ToString());
				beamObject.transform.SetParent(transform, false);
				beamObject.transform.localPosition = Vector3.zero;
				beamObject.transform.localRotation = Quaternion.identity;
				beamObject.SetActive(false);
				m_Beam = beamObject.AddComponent<ControllerBeam>();
				m_Beam.BeamType = m_ControllerType;
				beamObject.SetActive(true);

				// Add a pointer.
				pointerObject = new GameObject("Pointer" + m_ControllerType.ToString());
				pointerObject.transform.SetParent(transform, false);
				pointerObject.transform.localPosition = Vector3.zero;
				pointerObject.transform.localRotation = Quaternion.identity;
				pointerObject.SetActive(false);
				m_Pointer = pointerObject.AddComponent<ControllerPointer>();
				m_Pointer.PointerType = m_ControllerType;
				pointerObject.SetActive(true);

				hasFocus = ApplicationScene.IsFocused;

				if (InputModuleSystem.Instance != null)
					Log.i(LOG_TAG, "OnEnable() Loaded InputModuleSystem.");

				EventControllerProvider.Instance.SetEventController(m_ControllerType, gameObject);

				mEnabled = true;
			}
		}

		private void Update()
		{
			CheckChildrenObjects();

			bool active = false;

			if (hasFocus != ApplicationScene.IsFocused)
			{
				hasFocus = ApplicationScene.IsFocused;
				DEBUG("Update() " + (hasFocus ? "Get system focus." : "Focus is captured by system."));
			}

			bool isTracked = WXRDevice.IsTracked((XR_Device)m_ControllerType);

			active = hasFocus && isTracked;

			if (m_ControllerActive != active)
			{
				m_ControllerActive = active;
				DEBUG(active ? "Show controller " + gameObject.name : "Hide controller " + gameObject.name);
				ForceActivateTargetObjects(active);
			}

			if (active)
			{
				XR_Hand another_hand = (m_ControllerType == XR_Hand.Dominant ? XR_Hand.NonDominant : XR_Hand.Dominant);
				bool is_primary = (m_ControllerType == InputModuleSystem.Instance.PrimaryInput);

				if ((EventControllerProvider.Instance.GetEventController(another_hand) == null) ||
					(!InputModuleSystem.Instance.SingleInput)
				)
				{
					// Show the beam if there is only one controller.
					m_Beam.ShowBeam = true;
				}
				else
				{
					if (is_primary)
					{
						m_Beam.ShowBeam = true;
					}
					else
					{
						m_Beam.ShowBeam = false;
						m_Pointer.ShowPointer = false;
					}
				}
			}
		} // Update
	}
}
