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
using Wave.Native;

namespace Wave.Essence.Hand.Demo
{
	[DisallowMultipleComponent]
	sealed class CubeCollisionHandler : MonoBehaviour
	{
		const string LOG_TAG = "Wave.Essence.Hand.Demo.CubeCollisionHandler";
		private void DEBUG(string msg)
		{
			if (Log.EnableDebugLog)
				Log.d(LOG_TAG, msg, true);
		}
		private Material cubeMatBlue = null, cubeMatRed = null;

		void Start()
		{
			cubeMatBlue = Resources.Load("Materials/BlueCube01") as Material;
			if (cubeMatBlue != null)
				DEBUG("Start() Loaded BlueCube01.");
			cubeMatRed = Resources.Load("Materials/RedCube01") as Material;
			if (cubeMatRed != null)
				DEBUG("Start() Loaded RedCube01.");
		}

		void OnCollisionEnter(Collision other)
		{
			gameObject.GetComponent<MeshRenderer>().material = cubeMatRed;
		}

		void OnCollisionExit(Collision other)
		{
			gameObject.GetComponent<MeshRenderer>().material = cubeMatBlue;
		}
	}
}
