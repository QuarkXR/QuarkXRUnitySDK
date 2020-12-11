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
	public class HandBeamProvider
	{
		private const string LOG_TAG = "Wave.Essence.Hand.HandBeamProvider";
		private void DEBUG(string msg)
		{
			if (Log.EnableDebugLog)
				Log.d(LOG_TAG, msg, true);
		}

		private class BeamStorage
		{
			public HandManager.HandType Hand { get; set; }
			public GameObject Beam { get; set; }

			public BeamStorage(HandManager.HandType type, GameObject beam)
			{
				Hand = type;
				Beam = beam;
			}
		}

		private List<BeamStorage> m_HandBeams = new List<BeamStorage>();
		private readonly HandManager.HandType[] s_HandTypes = new HandManager.HandType[] {
			HandManager.HandType.RIGHT,
			HandManager.HandType.LEFT
		};

		private static HandBeamProvider m_Instance = null;
		public static HandBeamProvider Instance
		{
			get
			{
				if (m_Instance == null)
					m_Instance = new HandBeamProvider();
				return m_Instance;
			}
		}

		private HandBeamProvider()
		{
			for (int i = 0; i < s_HandTypes.Length; i++)
				m_HandBeams.Add(new BeamStorage(s_HandTypes[i], null));
		}

		public void SetHandBeam(HandManager.HandType hand, GameObject beam)
		{
			DEBUG("SetHandBeam() " + hand + ", beam: " + (beam != null ? beam.name : "null"));

			for (int i = 0; i < s_HandTypes.Length; i++)
			{
				if (s_HandTypes[i] == hand)
				{
					m_HandBeams[i].Beam = beam;
					break;
				}
			}
		}

		public GameObject GetHandBeam(HandManager.HandType hand)
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

			return m_HandBeams[index].Beam;
		}
	}
}
