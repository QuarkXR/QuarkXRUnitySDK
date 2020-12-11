// "Wave SDK 
// © 2020 HTC Corporation. All Rights Reserved.
//
// Unless otherwise required by copyright law and practice,
// upon the execution of HTC SDK license agreement,
// HTC grants you access to and use of the Wave SDK(s).
// You shall fully comply with all of HTC’s SDK license agreement terms and
// conditions signed by you and all SDK and API requirements,
// specifications, and documentation provided by HTC to You."

using System;
using System.Collections.Generic;
using UnityEngine.UI;
using Wave.Native;

namespace Wave.Essence.InputModule
{
	public static class EventCanvasProvider
	{
		const string LOG_TAG = "Wave.Essence.InputModule.EventCanvasProvider";
		private static void DEBUG(string msg)
		{
			if (Log.EnableDebugLog)
				Log.d(LOG_TAG, msg, true);
		}

		private static Dictionary<int, GraphicRaycaster> m_RaycasterMap = new Dictionary<int, GraphicRaycaster>();
		/// <summary>
		/// After a raycaster is registered to the provider, an index number of the raycaster will return.
		/// </summary>
		public static int RegisterEventCanvas(in GraphicRaycaster raycaster)
		{
			int index = 0;
			do
			{
				index = GetRandomNumber();
			} while (m_RaycasterMap.ContainsKey(index));

			m_RaycasterMap.Add(index, raycaster);

			return index;
		}
		public static void RemoveEventCanvas(int index)
		{
			if (m_RaycasterMap.ContainsKey(index))
				m_RaycasterMap.Remove(index);
		}

		private static List<GraphicRaycaster> m_RaycasterList = new List<GraphicRaycaster>();
		public static GraphicRaycaster[] GetEventCanvas()
		{
			m_RaycasterList.Clear();
			foreach(GraphicRaycaster value in m_RaycasterMap.Values)
				m_RaycasterList.Add(value);

			return m_RaycasterList.ToArray();
		}

		private static Random m_Random = new Random();
		private static int GetRandomNumber()
		{
			return m_Random.Next();
		}
	}
}
