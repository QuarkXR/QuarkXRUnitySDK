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

namespace Wave.Essence.InputModule.Demo
{
	public class ScrollViewButtonHandler : MonoBehaviour
	{
		private ControllerInputModule m_ControllerInputModule = null;
		private void OnEnable()
		{
			m_ControllerInputModule = EventSystem.current.gameObject.GetComponent<ControllerInputModule>();
		}

		public void OnFixedMode()
		{
			if (m_ControllerInputModule != null)
				m_ControllerInputModule.BeamMode = ControllerInputModule.BeamModes.Fixed;
		}

		public void OnFlexibleMode()
		{
			if (m_ControllerInputModule != null)
				m_ControllerInputModule.BeamMode = ControllerInputModule.BeamModes.Flexible;
		}

		public void OnMouseMode()
		{
			if (m_ControllerInputModule != null)
				m_ControllerInputModule.BeamMode = ControllerInputModule.BeamModes.Mouse;
		}
	}
}
