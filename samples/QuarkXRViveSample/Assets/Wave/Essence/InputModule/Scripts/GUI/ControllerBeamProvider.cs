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
	sealed class ControllerBeamProvider
	{
		const string LOG_TAG = "Wave.Essence.InputModule.ControllerBeamProvider";
		private void DEBUG(string msg)
		{
			if (Log.EnableDebugLog)
				Log.d(LOG_TAG, msg, true);
		}

		private class BeamStorage
		{
			public XR_Hand hand { get; set; }
			public GameObject beamObject { get; set; }

			public BeamStorage(XR_Hand type, GameObject beam)
			{
				hand = type;
				beamObject = beam;
			}
		}
		private List<BeamStorage> controllerBeams = new List<BeamStorage>();
		private XR_Hand[] beamHandList = new XR_Hand[] {
			XR_Hand.Dominant,
			XR_Hand.NonDominant
		};

		private static ControllerBeamProvider m_Instance = null;
		public static ControllerBeamProvider Instance
		{
			get
			{
				if (m_Instance == null)
					m_Instance = new ControllerBeamProvider();
				return m_Instance;
			}
		}

		private ControllerBeamProvider()
		{
			for (int i = 0; i < beamHandList.Length; i++)
				controllerBeams.Add(new BeamStorage(beamHandList[i], null));
		}

		public void SetControllerBeam(XR_Hand hand, GameObject beam)
		{
			DEBUG("SetControllerBeam() " + hand + ", beam: " + (beam != null ? beam.name : "null"));

			for (int i = 0; i < beamHandList.Length; i++)
			{
				if (beamHandList[i] == hand)
				{
					controllerBeams[i].beamObject = beam;
					break;
				}
			}
		}

		public GameObject GetControllerBeam(XR_Hand hand)
		{
			int index = 0;
			for (int i = 0; i < beamHandList.Length; i++)
			{
				if (beamHandList[i] == hand)
				{
					index = i;
					break;
				}
			}

			return controllerBeams[index].beamObject;
		}
	}
}
