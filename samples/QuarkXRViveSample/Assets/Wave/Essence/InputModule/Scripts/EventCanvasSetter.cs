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
using UnityEngine.UI;
using Wave.Native;

namespace Wave.Essence.InputModule
{
	/// <summary>
	/// Used to replace the UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster.
	/// Only the canvas registered by EventCanvasSetter can be raycasted.
	/// </summary>
	[DisallowMultipleComponent]
	sealed class EventCanvasSetter : MonoBehaviour
	{
		private const string LOG_TAG = "Wave.Essence.InputModule.EventCanvasSetter";
		private void DEBUG(string msg)
		{
			if (Log.EnableDebugLog)
				Log.d(LOG_TAG, msg, true);
		}

		private List<int> m_CanvasIndex = new List<int>();

		void Update()
		{
			for (int i = 0; i < m_CanvasIndex.Count; i++)
				EventCanvasProvider.RemoveEventCanvas(m_CanvasIndex[i]);
			m_CanvasIndex.Clear();

			GraphicRaycaster[] graphicRaycasters = gameObject.GetComponentsInChildren<GraphicRaycaster>();
			for (int i = 0; i < graphicRaycasters.Length; i++)
				m_CanvasIndex.Add(EventCanvasProvider.RegisterEventCanvas(graphicRaycasters[i]));
		}
	}
}
