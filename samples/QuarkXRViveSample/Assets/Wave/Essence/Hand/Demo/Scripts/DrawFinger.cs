// "WaveVR SDK 
// © 2020 HTC Corporation. All Rights Reserved.
//
// Unless otherwise required by copyright law and practice,
// upon the execution of HTC SDK license agreement,
// HTC grants you access to and use of the WaveVR SDK(s).
// You shall fully comply with all of HTC’s SDK license agreement terms and
// conditions signed by you and all SDK and API requirements,
// specifications, and documentation provided by HTC to You."

using UnityEngine;

namespace Wave.Essence.Hand.Demo
{
	[DisallowMultipleComponent]
	public class DrawFinger : MonoBehaviour
	{
		[SerializeField]
		private GameObject m_TargetBone = null;
		public GameObject TargetBone { get { return m_TargetBone; } set { m_TargetBone = value; } }

		[SerializeField]
		private float m_FingerWidth = 0.001f;
		public float FingerWidth { get { return m_FingerWidth; } set { m_FingerWidth = value; } }

		[SerializeField]
		private Color m_FingerColor = Color.red;
		public Color FingerColor { get { return m_FingerColor; } set { m_FingerColor = value; } }

		private LineRenderer finger = null;

		private Vector3 startPos = Vector3.zero;
		private Vector3 endPos = Vector3.zero;

		void Start()
		{
			if (m_TargetBone != null)
			{
				finger = this.gameObject.AddComponent<LineRenderer>();
#if UNITY_2019_1_OR_NEWER
				finger.material = new Material(Shader.Find("Legacy Shaders/Particles/Additive"));
#else
				finger.material = new Material (Shader.Find ("Particles/Additive"));
#endif

#if UNITY_5_6_OR_NEWER
				finger.positionCount = 2;
#else
				finger.SetVertexCount (2);
#endif
			}
		}

		void Update()
		{
			if (finger == null)
				return;
			if (m_TargetBone.GetComponent<BonePose>() == null)
				return;

			finger.enabled = m_TargetBone.GetComponent<BonePose>().Valid;

			startPos = transform.position;
			endPos = m_TargetBone.transform.position;

			finger.startColor = m_FingerColor;
			finger.endColor = m_FingerColor;
			finger.startWidth = m_FingerWidth;
			finger.endWidth = m_FingerWidth;
			finger.SetPosition(0, startPos);
			finger.SetPosition(1, endPos);
		}
	}
}
