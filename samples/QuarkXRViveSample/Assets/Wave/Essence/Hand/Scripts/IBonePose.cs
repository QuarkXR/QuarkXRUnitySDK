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

namespace Wave.Essence.Hand
{
	public abstract class IBonePose : MonoBehaviour
	{
		private static BonePoseImpl m_Instance = null;
		public static BonePoseImpl Instance
		{
			get
			{
				if (m_Instance == null)
					m_Instance = new BonePoseImpl();
				return m_Instance;
			}
		}

		public virtual void Awake()
		{
			if (m_Instance == null)
				m_Instance = new BonePoseImpl();
		}

		public RigidTransform GetBoneTransform(BonePoseImpl.Bones bone_type)
		{
			return Instance.GetBoneTransform(bone_type);
		}

		public bool IsBonePoseValid(BonePoseImpl.Bones bone_type)
		{
			return Instance.IsBonePoseValid(bone_type);
		}

		public bool IsHandPoseValid(HandManager.HandType hand)
		{
			return Instance.IsHandPoseValid(hand);
		}

		public float GetBoneConfidence(BonePoseImpl.Bones bone_type)
		{
			return Instance.GetBoneConfidence(bone_type);
		}

		public float GetHandConfidence(HandManager.HandType hand)
		{
			return Instance.GetHandConfidence(hand);
		}
	}
}
