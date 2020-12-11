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
using System;

namespace Wave.Essence.Hand
{
	public class BonePoseImpl
	{
		private const string LOG_TAG = "Wave.Essence.Hand.BonePoseImpl";
		private void DEBUG(string msg)
		{
			if (Log.EnableDebugLog)
				Log.d(LOG_TAG, msg, true);
		}
		public enum Bones
		{
			ROOT = 0,

			LEFT_WRIST,
			LEFT_THUMB_JOINT1,
			LEFT_THUMB_JOINT2,
			LEFT_THUMB_JOINT3,
			LEFT_THUMB_TIP,
			LEFT_INDEX_JOINT1,
			LEFT_INDEX_JOINT2,
			LEFT_INDEX_JOINT3,
			LEFT_INDEX_TIP,
			LEFT_MIDDLE_JOINT1,
			LEFT_MIDDLE_JOINT2,
			LEFT_MIDDLE_JOINT3,
			LEFT_MIDDLE_TIP,
			LEFT_RING_JOINT1,
			LEFT_RING_JOINT2,
			LEFT_RING_JOINT3,
			LEFT_RING_TIP,
			LEFT_PINKY_JOINT1,
			LEFT_PINKY_JOINT2,
			LEFT_PINKY_JOINT3,
			LEFT_PINKY_TIP,
			// Total 21 left bones.

			RIGHT_WRIST,
			RIGHT_THUMB_JOINT1,
			RIGHT_THUMB_JOINT2,
			RIGHT_THUMB_JOINT3,
			RIGHT_THUMB_TIP,
			RIGHT_INDEX_JOINT1,
			RIGHT_INDEX_JOINT2,
			RIGHT_INDEX_JOINT3,
			RIGHT_INDEX_TIP,
			RIGHT_MIDDLE_JOINT1,
			RIGHT_MIDDLE_JOINT2,
			RIGHT_MIDDLE_JOINT3,
			RIGHT_MIDDLE_TIP,
			RIGHT_RING_JOINT1,
			RIGHT_RING_JOINT2,
			RIGHT_RING_JOINT3,
			RIGHT_RING_TIP,
			RIGHT_PINKY_JOINT1,
			RIGHT_PINKY_JOINT2,
			RIGHT_PINKY_JOINT3,
			RIGHT_PINKY_TIP,
			// Total 21 right bones.
		};

		private readonly Bones[] a_LeftBones = new Bones[] {
			Bones.LEFT_WRIST,
			Bones.LEFT_THUMB_JOINT1,
			Bones.LEFT_THUMB_JOINT2,
			Bones.LEFT_THUMB_JOINT3,
			Bones.LEFT_THUMB_TIP,
			Bones.LEFT_INDEX_JOINT1,
			Bones.LEFT_INDEX_JOINT2,
			Bones.LEFT_INDEX_JOINT3,
			Bones.LEFT_INDEX_TIP,
			Bones.LEFT_MIDDLE_JOINT1,
			Bones.LEFT_MIDDLE_JOINT2,
			Bones.LEFT_MIDDLE_JOINT3,
			Bones.LEFT_MIDDLE_TIP,
			Bones.LEFT_RING_JOINT1,
			Bones.LEFT_RING_JOINT2,
			Bones.LEFT_RING_JOINT3,
			Bones.LEFT_RING_TIP,
			Bones.LEFT_PINKY_JOINT1,
			Bones.LEFT_PINKY_JOINT2,
			Bones.LEFT_PINKY_JOINT3,
			Bones.LEFT_PINKY_TIP
		};

		private readonly Bones[] a_RightBones = new Bones[] {
			Bones.RIGHT_WRIST,
			Bones.RIGHT_THUMB_JOINT1,
			Bones.RIGHT_THUMB_JOINT2,
			Bones.RIGHT_THUMB_JOINT3,
			Bones.RIGHT_THUMB_TIP,
			Bones.RIGHT_INDEX_JOINT1,
			Bones.RIGHT_INDEX_JOINT2,
			Bones.RIGHT_INDEX_JOINT3,
			Bones.RIGHT_INDEX_TIP,
			Bones.RIGHT_MIDDLE_JOINT1,
			Bones.RIGHT_MIDDLE_JOINT2,
			Bones.RIGHT_MIDDLE_JOINT3,
			Bones.RIGHT_MIDDLE_TIP,
			Bones.RIGHT_RING_JOINT1,
			Bones.RIGHT_RING_JOINT2,
			Bones.RIGHT_RING_JOINT3,
			Bones.RIGHT_RING_TIP,
			Bones.RIGHT_PINKY_JOINT1,
			Bones.RIGHT_PINKY_JOINT2,
			Bones.RIGHT_PINKY_JOINT3,
			Bones.RIGHT_PINKY_TIP
		};

		private class BoneData
		{
			private RigidTransform rigidTransform = RigidTransform.identity;

			public BoneData()
			{
				rigidTransform = RigidTransform.identity;
			}

			public RigidTransform GetTransform() { return rigidTransform; }
			public Vector3 GetPosition() { return rigidTransform.pos; }
			public void SetPosition(Vector3 in_pos) { rigidTransform.pos = in_pos; }
			public Quaternion GetRotation() { return rigidTransform.rot; }
			public void SetRotation(Quaternion in_rot) { rigidTransform.rot = in_rot; }
		};

		static BoneData[] s_BoneData;

		public BonePoseImpl()
		{
			DEBUG("BonePoseImpl()");
			s_BoneData = new BoneData[Enum.GetNames(typeof(Bones)).Length];
			for (int i = 0; i < Enum.GetNames(typeof(Bones)).Length; i++)
			{
				s_BoneData[i] = new BoneData();
			}
		}

		int trackFrameCount = -1;
		private bool AllowGetTrackingData()
		{
			if (Time.frameCount != trackFrameCount)
			{
				trackFrameCount = Time.frameCount;
				return true;
			}

			return false;
		}

		private bool hasHandTrackingData = false;
		private WVR_HandSkeletonData_t handSkeletonData = new WVR_HandSkeletonData_t();
		private bool validPoseLeft = false, validPoseRight = false;
		public RigidTransform GetBoneTransform(Bones bone_type)
		{
			if (AllowGetTrackingData() && HandManager.Instance != null)
			{
				validPoseLeft = handSkeletonData.left.wrist.IsValidPose;
				validPoseRight = handSkeletonData.right.wrist.IsValidPose;
				hasHandTrackingData = HandManager.Instance.GetHandSkeletonData(ref handSkeletonData);
				if (hasHandTrackingData)
				{
					if (validPoseLeft != handSkeletonData.left.wrist.IsValidPose)
						DEBUG("GetBoneTransform() left pose is " + (handSkeletonData.left.wrist.IsValidPose ? "valid." : "invalid."));

					if (validPoseRight != handSkeletonData.right.wrist.IsValidPose)
						DEBUG("GetBoneTransform() right pose is " + (handSkeletonData.right.wrist.IsValidPose ? "valid." : "invalid."));

					if (handSkeletonData.left.wrist.IsValidPose)
						UpdateLeftHandTrackingData();

					if (handSkeletonData.right.wrist.IsValidPose)
						UpdateRightHandTrackingData();
				}
			}

			return s_BoneData[(int)bone_type].GetTransform();
		}

		public RigidTransform GetBoneTransform(int index, bool isLeft)
		{
			int bone_index = (int)Bones.ROOT;

			if (isLeft)
				bone_index = index + 1;
			else
				bone_index = index + 22;

			return GetBoneTransform((Bones)bone_index);
		}

		public bool IsBonePoseValid(Bones bone_type)
		{
			for (int i = 0; i < a_LeftBones.Length; i++)
			{
				if (a_LeftBones[i] == bone_type)
					return IsHandPoseValid(HandManager.HandType.LEFT);
			}

			for (int i = 0; i < a_RightBones.Length; i++)
			{
				if (a_RightBones[i] == bone_type)
					return IsHandPoseValid(HandManager.HandType.RIGHT);
			}

			return false;
		}

		public bool IsHandPoseValid(HandManager.HandType hand)
		{
			if (HandManager.Instance == null)
				return false;

			return HandManager.Instance.IsHandPoseValid(hand);
		}

		public float GetBoneConfidence(Bones bone_type)
		{
			for (int i = 0; i < a_LeftBones.Length; i++)
			{
				if (a_LeftBones[i] == bone_type)
					return GetHandConfidence(HandManager.HandType.LEFT);
			}

			for (int i = 0; i < a_RightBones.Length; i++)
			{
				if (a_RightBones[i] == bone_type)
					return GetHandConfidence(HandManager.HandType.RIGHT);
			}

			return 0;
		}

		public float GetHandConfidence(HandManager.HandType hand)
		{
			if (HandManager.Instance == null)
				return 0;

			return HandManager.Instance.GetHandConfidence(hand);
		}

		private RigidTransform rtWristLeft = RigidTransform.identity;
		private void UpdateLeftHandTrackingData()
		{
			// Left wrist - LEFT_WRIST
			rtWristLeft.update(handSkeletonData.left.wrist.PoseMatrix);
			Vector3 LEFT_WRIST_Pos = rtWristLeft.pos;
			Quaternion LEFT_WRIST_Rot = rtWristLeft.rot;

			s_BoneData[(int)Bones.LEFT_WRIST].SetPosition(LEFT_WRIST_Pos);
			s_BoneData[(int)Bones.LEFT_WRIST].SetRotation(LEFT_WRIST_Rot);

			// Left thumb joint1 - LEFT_THUMB_JOINT1
			Vector3 LEFT_THUMB_JOINT1_Pos = Coordinate.GetVectorFromGL(handSkeletonData.left.thumb.joint1);
			Quaternion LEFT_THUMB_JOINT1_Rot = Quaternion.LookRotation(LEFT_THUMB_JOINT1_Pos - LEFT_WRIST_Pos);

			s_BoneData[(int)Bones.LEFT_THUMB_JOINT1].SetPosition(LEFT_THUMB_JOINT1_Pos);
			s_BoneData[(int)Bones.LEFT_THUMB_JOINT1].SetRotation(LEFT_THUMB_JOINT1_Rot);

			// Left thumb joint2 - LEFT_THUMB_JOINT2
			Vector3 LEFT_THUMB_JOINT2_Pos = Coordinate.GetVectorFromGL(handSkeletonData.left.thumb.joint2);
			Quaternion LEFT_THUMB_JOINT2_Rot = Quaternion.LookRotation(LEFT_THUMB_JOINT2_Pos - LEFT_THUMB_JOINT1_Pos);

			s_BoneData[(int)Bones.LEFT_THUMB_JOINT2].SetPosition(LEFT_THUMB_JOINT2_Pos);
			s_BoneData[(int)Bones.LEFT_THUMB_JOINT2].SetRotation(LEFT_THUMB_JOINT2_Rot);

			// Left thumb joint3 - LEFT_THUMB_JOINT3
			Vector3 LEFT_THUMB_JOINT3_Pos = Coordinate.GetVectorFromGL(handSkeletonData.left.thumb.joint3);
			Quaternion LEFT_THUMB_JOINT3_Rot = Quaternion.LookRotation(LEFT_THUMB_JOINT3_Pos - LEFT_THUMB_JOINT2_Pos);

			s_BoneData[(int)Bones.LEFT_THUMB_JOINT3].SetPosition(LEFT_THUMB_JOINT3_Pos);
			s_BoneData[(int)Bones.LEFT_THUMB_JOINT3].SetRotation(LEFT_THUMB_JOINT3_Rot);

			// Left thumb tip - LEFT_THUMB_TIP
			Vector3 LEFT_THUMB_TIP_Pos = Coordinate.GetVectorFromGL(handSkeletonData.left.thumb.tip);
			Quaternion LEFT_THUMB_TIP_Rot = Quaternion.LookRotation(LEFT_THUMB_TIP_Pos - LEFT_THUMB_JOINT3_Pos);

			s_BoneData[(int)Bones.LEFT_THUMB_TIP].SetPosition(LEFT_THUMB_TIP_Pos);
			s_BoneData[(int)Bones.LEFT_THUMB_TIP].SetRotation(LEFT_THUMB_TIP_Rot);

			// Left index joint1 - LEFT_INDEX_JOINT1
			Vector3 LEFT_INDEX_JOINT1_Pos = Coordinate.GetVectorFromGL(handSkeletonData.left.index.joint1);
			Quaternion LEFT_INDEX_JOINT1_Rot = Quaternion.LookRotation(LEFT_INDEX_JOINT1_Pos - LEFT_WRIST_Pos);

			s_BoneData[(int)Bones.LEFT_INDEX_JOINT1].SetPosition(LEFT_INDEX_JOINT1_Pos);
			s_BoneData[(int)Bones.LEFT_INDEX_JOINT1].SetRotation(LEFT_INDEX_JOINT1_Rot);

			// Left index joint2 - LEFT_INDEX_JOINT2
			Vector3 LEFT_INDEX_JOINT2_Pos = Coordinate.GetVectorFromGL(handSkeletonData.left.index.joint2);
			Quaternion LEFT_INDEX_JOINT2_Rot = Quaternion.LookRotation(LEFT_INDEX_JOINT2_Pos - LEFT_INDEX_JOINT1_Pos);

			s_BoneData[(int)Bones.LEFT_INDEX_JOINT2].SetPosition(LEFT_INDEX_JOINT2_Pos);
			s_BoneData[(int)Bones.LEFT_INDEX_JOINT2].SetRotation(LEFT_INDEX_JOINT2_Rot);

			// Left index joint3 - LEFT_INDEX_JOINT3
			Vector3 LEFT_INDEX_JOINT3_Pos = Coordinate.GetVectorFromGL(handSkeletonData.left.index.joint3);
			Quaternion LEFT_INDEX_JOINT3_Rot = Quaternion.LookRotation(LEFT_INDEX_JOINT3_Pos, LEFT_INDEX_JOINT2_Pos);

			s_BoneData[(int)Bones.LEFT_INDEX_JOINT3].SetPosition(LEFT_INDEX_JOINT3_Pos);
			s_BoneData[(int)Bones.LEFT_INDEX_JOINT3].SetRotation(LEFT_INDEX_JOINT3_Rot);

			// Left index tip - LEFT_INDEX_TIP
			Vector3 LEFT_INDEX_TIP_Pos = Coordinate.GetVectorFromGL(handSkeletonData.left.index.tip);
			Quaternion LEFT_INDEX_TIP_Rot = Quaternion.LookRotation(LEFT_INDEX_TIP_Pos - LEFT_INDEX_JOINT3_Pos);

			s_BoneData[(int)Bones.LEFT_INDEX_TIP].SetPosition(LEFT_INDEX_TIP_Pos);
			s_BoneData[(int)Bones.LEFT_INDEX_TIP].SetRotation(LEFT_INDEX_TIP_Rot);

			// Left middle joint1 - LEFT_MIDDLE_JOINT1
			Vector3 LEFT_MIDDLE_JOINT1_Pos = Coordinate.GetVectorFromGL(handSkeletonData.left.middle.joint1);
			Quaternion LEFT_MIDDLE_JOINT1_Rot = Quaternion.LookRotation(LEFT_MIDDLE_JOINT1_Pos - LEFT_WRIST_Pos);

			s_BoneData[(int)Bones.LEFT_MIDDLE_JOINT1].SetPosition(LEFT_MIDDLE_JOINT1_Pos);
			s_BoneData[(int)Bones.LEFT_MIDDLE_JOINT1].SetRotation(LEFT_MIDDLE_JOINT1_Rot);

			// Left middle joint2 - LEFT_MIDDLE_JOINT2
			Vector3 LEFT_MIDDLE_JOINT2_Pos = Coordinate.GetVectorFromGL(handSkeletonData.left.middle.joint2);
			Quaternion LEFT_MIDDLE_JOINT2_Rot = Quaternion.LookRotation(LEFT_MIDDLE_JOINT2_Pos - LEFT_MIDDLE_JOINT1_Pos);

			s_BoneData[(int)Bones.LEFT_MIDDLE_JOINT2].SetPosition(LEFT_MIDDLE_JOINT2_Pos);
			s_BoneData[(int)Bones.LEFT_MIDDLE_JOINT2].SetRotation(LEFT_MIDDLE_JOINT2_Rot);

			// Left middle joint3 - LEFT_MIDDLE_JOINT3
			Vector3 LEFT_MIDDLE_JOINT3_Pos = Coordinate.GetVectorFromGL(handSkeletonData.left.middle.joint3);
			Quaternion LEFT_MIDDLE_JOINT3_Rot = Quaternion.LookRotation(LEFT_MIDDLE_JOINT3_Pos - LEFT_MIDDLE_JOINT2_Pos);

			s_BoneData[(int)Bones.LEFT_MIDDLE_JOINT3].SetPosition(LEFT_MIDDLE_JOINT3_Pos);
			s_BoneData[(int)Bones.LEFT_MIDDLE_JOINT3].SetRotation(LEFT_MIDDLE_JOINT3_Rot);

			// Left middle tip - LEFT_MIDDLE_TIP
			Vector3 LEFT_MIDDLE_TIP_Pos = Coordinate.GetVectorFromGL(handSkeletonData.left.middle.tip);
			Quaternion LEFT_MIDDLE_TIP_Rot = Quaternion.LookRotation(LEFT_MIDDLE_TIP_Pos - LEFT_MIDDLE_JOINT3_Pos);

			s_BoneData[(int)Bones.LEFT_MIDDLE_TIP].SetPosition(LEFT_MIDDLE_TIP_Pos);
			s_BoneData[(int)Bones.LEFT_MIDDLE_TIP].SetRotation(LEFT_MIDDLE_TIP_Rot);

			// Left ring joint1 - LEFT_RING_JOINT1
			Vector3 LEFT_RING_JOINT1_Pos = Coordinate.GetVectorFromGL(handSkeletonData.left.ring.joint1);
			Quaternion LEFT_RING_JOINT1_Rot = Quaternion.LookRotation(LEFT_RING_JOINT1_Pos - LEFT_WRIST_Pos);

			s_BoneData[(int)Bones.LEFT_RING_JOINT1].SetPosition(LEFT_RING_JOINT1_Pos);
			s_BoneData[(int)Bones.LEFT_RING_JOINT1].SetRotation(LEFT_RING_JOINT1_Rot);

			// Left ring joint2 - LEFT_RING_JOINT2
			Vector3 LEFT_RING_JOINT2_Pos = Coordinate.GetVectorFromGL(handSkeletonData.left.ring.joint2);
			Quaternion LEFT_RING_JOINT2_Rot = Quaternion.LookRotation(LEFT_RING_JOINT2_Pos - LEFT_RING_JOINT1_Pos);

			s_BoneData[(int)Bones.LEFT_RING_JOINT2].SetPosition(LEFT_RING_JOINT2_Pos);
			s_BoneData[(int)Bones.LEFT_RING_JOINT2].SetRotation(LEFT_RING_JOINT2_Rot);

			// Left ring joint3 - LEFT_RING_JOINT3
			Vector3 LEFT_RING_JOINT3_Pos = Coordinate.GetVectorFromGL(handSkeletonData.left.ring.joint3);
			Quaternion LEFT_RING_JOINT3_Rot = Quaternion.LookRotation(LEFT_RING_JOINT3_Pos - LEFT_RING_JOINT2_Pos);

			s_BoneData[(int)Bones.LEFT_RING_JOINT3].SetPosition(LEFT_RING_JOINT3_Pos);
			s_BoneData[(int)Bones.LEFT_RING_JOINT3].SetRotation(LEFT_RING_JOINT3_Rot);

			// Left ring tip - LEFT_RING_TIP
			Vector3 LEFT_RING_TIP_Pos = Coordinate.GetVectorFromGL(handSkeletonData.left.ring.tip);
			Quaternion LEFT_RING_TIP_Rot = Quaternion.LookRotation(LEFT_RING_TIP_Pos - LEFT_RING_JOINT3_Pos);

			s_BoneData[(int)Bones.LEFT_RING_TIP].SetPosition(LEFT_RING_TIP_Pos);
			s_BoneData[(int)Bones.LEFT_RING_TIP].SetRotation(LEFT_RING_TIP_Rot);

			// Left pinky joint1 - LEFT_PINKY_JOINT1
			Vector3 LEFT_PINKY_JOINT1_Pos = Coordinate.GetVectorFromGL(handSkeletonData.left.pinky.joint1);
			Quaternion LEFT_PINKY_JOINT1_Rot = Quaternion.LookRotation(LEFT_PINKY_JOINT1_Pos - LEFT_WRIST_Pos);

			s_BoneData[(int)Bones.LEFT_PINKY_JOINT1].SetPosition(LEFT_PINKY_JOINT1_Pos);
			s_BoneData[(int)Bones.LEFT_PINKY_JOINT1].SetRotation(LEFT_PINKY_JOINT1_Rot);

			// Left pinky joint2 - LEFT_PINKY_JOINT2
			Vector3 LEFT_PINKY_JOINT2_Pos = Coordinate.GetVectorFromGL(handSkeletonData.left.pinky.joint2);
			Quaternion LEFT_PINKY_JOINT2_Rot = Quaternion.LookRotation(LEFT_PINKY_JOINT2_Pos - LEFT_PINKY_JOINT1_Pos);

			s_BoneData[(int)Bones.LEFT_PINKY_JOINT2].SetPosition(LEFT_PINKY_JOINT2_Pos);
			s_BoneData[(int)Bones.LEFT_PINKY_JOINT2].SetRotation(LEFT_PINKY_JOINT2_Rot);

			// Left pinky joint3 - LEFT_PINKY_JOINT3
			Vector3 LEFT_PINKY_JOINT3_Pos = Coordinate.GetVectorFromGL(handSkeletonData.left.pinky.joint3);
			Quaternion LEFT_PINKY_JOINT3_Rot = Quaternion.LookRotation(LEFT_PINKY_JOINT3_Pos - LEFT_PINKY_JOINT2_Pos);

			s_BoneData[(int)Bones.LEFT_PINKY_JOINT3].SetPosition(LEFT_PINKY_JOINT3_Pos);
			s_BoneData[(int)Bones.LEFT_PINKY_JOINT3].SetRotation(LEFT_PINKY_JOINT3_Rot);

			// Left pinky tip - LEFT_PINKY_TIP
			Vector3 LEFT_PINKY_TIP_Pos = Coordinate.GetVectorFromGL(handSkeletonData.left.pinky.tip);
			Quaternion LEFT_PINKY_TIP_Rot = Quaternion.LookRotation(LEFT_PINKY_TIP_Pos - LEFT_PINKY_JOINT3_Pos);

			s_BoneData[(int)Bones.LEFT_PINKY_TIP].SetPosition(LEFT_PINKY_TIP_Pos);
			s_BoneData[(int)Bones.LEFT_PINKY_TIP].SetRotation(LEFT_PINKY_TIP_Rot);
		}

		private RigidTransform rtWristRight = RigidTransform.identity;
		private void UpdateRightHandTrackingData()
		{
			// Right wrist - RIGHT_WRIST
			rtWristRight.update(handSkeletonData.right.wrist.PoseMatrix);
			Vector3 RIGHT_WRIST_Pos = rtWristRight.pos;
			Quaternion RIGHT_WRIST_Rot = rtWristRight.rot;

			s_BoneData[(int)Bones.RIGHT_WRIST].SetPosition(RIGHT_WRIST_Pos);
			s_BoneData[(int)Bones.RIGHT_WRIST].SetRotation(RIGHT_WRIST_Rot);

			// Right thumb joint1 - RIGHT_THUMB_JOINT1
			Vector3 RIGHT_THUMB_JOINT1_Pos = Coordinate.GetVectorFromGL(handSkeletonData.right.thumb.joint1);
			Quaternion RIGHT_THUMB_JOINT1_Rot = Quaternion.LookRotation(RIGHT_THUMB_JOINT1_Pos - RIGHT_WRIST_Pos);

			s_BoneData[(int)Bones.RIGHT_THUMB_JOINT1].SetPosition(RIGHT_THUMB_JOINT1_Pos);
			s_BoneData[(int)Bones.RIGHT_THUMB_JOINT1].SetRotation(RIGHT_THUMB_JOINT1_Rot);

			// Right thumb joint2 - RIGHT_THUMB_JOINT2
			Vector3 RIGHT_THUMB_JOINT2_Pos = Coordinate.GetVectorFromGL(handSkeletonData.right.thumb.joint2);
			Quaternion RIGHT_THUMB_JOINT2_Rot = Quaternion.LookRotation(RIGHT_THUMB_JOINT2_Pos - RIGHT_THUMB_JOINT1_Pos);

			s_BoneData[(int)Bones.RIGHT_THUMB_JOINT2].SetPosition(RIGHT_THUMB_JOINT2_Pos);
			s_BoneData[(int)Bones.RIGHT_THUMB_JOINT2].SetRotation(RIGHT_THUMB_JOINT2_Rot);

			// Right thumb joint3 - RIGHT_THUMB_JOINT3
			Vector3 RIGHT_THUMB_JOINT3_Pos = Coordinate.GetVectorFromGL(handSkeletonData.right.thumb.joint3);
			Quaternion RIGHT_THUMB_JOINT3_Rot = Quaternion.LookRotation(RIGHT_THUMB_JOINT3_Pos - RIGHT_THUMB_JOINT2_Pos);

			s_BoneData[(int)Bones.RIGHT_THUMB_JOINT3].SetPosition(RIGHT_THUMB_JOINT3_Pos);
			s_BoneData[(int)Bones.RIGHT_THUMB_JOINT3].SetRotation(RIGHT_THUMB_JOINT3_Rot);

			// Right thumb tip - RIGHT_THUMB_TIP
			Vector3 RIGHT_THUMB_TIP_Pos = Coordinate.GetVectorFromGL(handSkeletonData.right.thumb.tip);
			Quaternion RIGHT_THUMB_TIP_Rot = Quaternion.LookRotation(RIGHT_THUMB_TIP_Pos - RIGHT_THUMB_JOINT3_Pos);

			s_BoneData[(int)Bones.RIGHT_THUMB_TIP].SetPosition(RIGHT_THUMB_TIP_Pos);
			s_BoneData[(int)Bones.RIGHT_THUMB_TIP].SetRotation(RIGHT_THUMB_TIP_Rot);

			// Right index joint1 - RIGHT_INDEX_JOINT1
			Vector3 RIGHT_INDEX_JOINT1_Pos = Coordinate.GetVectorFromGL(handSkeletonData.right.index.joint1);
			Quaternion RIGHT_INDEX_JOINT1_Rot = Quaternion.LookRotation(RIGHT_INDEX_JOINT1_Pos - RIGHT_WRIST_Pos);

			s_BoneData[(int)Bones.RIGHT_INDEX_JOINT1].SetPosition(RIGHT_INDEX_JOINT1_Pos);
			s_BoneData[(int)Bones.RIGHT_INDEX_JOINT1].SetRotation(RIGHT_INDEX_JOINT1_Rot);

			// Right index joint2 - RIGHT_INDEX_JOINT2
			Vector3 RIGHT_INDEX_JOINT2_Pos = Coordinate.GetVectorFromGL(handSkeletonData.right.index.joint2);
			Quaternion RIGHT_INDEX_JOINT2_Rot = Quaternion.LookRotation(RIGHT_INDEX_JOINT2_Pos - RIGHT_INDEX_JOINT1_Pos);

			s_BoneData[(int)Bones.RIGHT_INDEX_JOINT2].SetPosition(RIGHT_INDEX_JOINT2_Pos);
			s_BoneData[(int)Bones.RIGHT_INDEX_JOINT2].SetRotation(RIGHT_INDEX_JOINT2_Rot);

			// Right index joint3 - RIGHT_INDEX_JOINT3
			Vector3 RIGHT_INDEX_JOINT3_Pos = Coordinate.GetVectorFromGL(handSkeletonData.right.index.joint3);
			Quaternion RIGHT_INDEX_JOINT3_Rot = Quaternion.LookRotation(RIGHT_INDEX_JOINT3_Pos, RIGHT_INDEX_JOINT2_Pos);

			s_BoneData[(int)Bones.RIGHT_INDEX_JOINT3].SetPosition(RIGHT_INDEX_JOINT3_Pos);
			s_BoneData[(int)Bones.RIGHT_INDEX_JOINT3].SetRotation(RIGHT_INDEX_JOINT3_Rot);

			// Right index tip - RIGHT_INDEX_TIP
			Vector3 RIGHT_INDEX_TIP_Pos = Coordinate.GetVectorFromGL(handSkeletonData.right.index.tip);
			Quaternion RIGHT_INDEX_TIP_Rot = Quaternion.LookRotation(RIGHT_INDEX_TIP_Pos - RIGHT_INDEX_JOINT3_Pos);

			s_BoneData[(int)Bones.RIGHT_INDEX_TIP].SetPosition(RIGHT_INDEX_TIP_Pos);
			s_BoneData[(int)Bones.RIGHT_INDEX_TIP].SetRotation(RIGHT_INDEX_TIP_Rot);

			// Right middle joint1 - RIGHT_MIDDLE_JOINT1
			Vector3 RIGHT_MIDDLE_JOINT1_Pos = Coordinate.GetVectorFromGL(handSkeletonData.right.middle.joint1);
			Quaternion RIGHT_MIDDLE_JOINT1_Rot = Quaternion.LookRotation(RIGHT_MIDDLE_JOINT1_Pos - RIGHT_WRIST_Pos);

			s_BoneData[(int)Bones.RIGHT_MIDDLE_JOINT1].SetPosition(RIGHT_MIDDLE_JOINT1_Pos);
			s_BoneData[(int)Bones.RIGHT_MIDDLE_JOINT1].SetRotation(RIGHT_MIDDLE_JOINT1_Rot);

			// Right middle joint2 - RIGHT_MIDDLE_JOINT2
			Vector3 RIGHT_MIDDLE_JOINT2_Pos = Coordinate.GetVectorFromGL(handSkeletonData.right.middle.joint2);
			Quaternion RIGHT_MIDDLE_JOINT2_Rot = Quaternion.LookRotation(RIGHT_MIDDLE_JOINT2_Pos - RIGHT_MIDDLE_JOINT1_Pos);

			s_BoneData[(int)Bones.RIGHT_MIDDLE_JOINT2].SetPosition(RIGHT_MIDDLE_JOINT2_Pos);
			s_BoneData[(int)Bones.RIGHT_MIDDLE_JOINT2].SetRotation(RIGHT_MIDDLE_JOINT2_Rot);

			// Right middle joint3 - RIGHT_MIDDLE_JOINT3
			Vector3 RIGHT_MIDDLE_JOINT3_Pos = Coordinate.GetVectorFromGL(handSkeletonData.right.middle.joint3);
			Quaternion RIGHT_MIDDLE_JOINT3_Rot = Quaternion.LookRotation(RIGHT_MIDDLE_JOINT3_Pos - RIGHT_MIDDLE_JOINT2_Pos);

			s_BoneData[(int)Bones.RIGHT_MIDDLE_JOINT3].SetPosition(RIGHT_MIDDLE_JOINT3_Pos);
			s_BoneData[(int)Bones.RIGHT_MIDDLE_JOINT3].SetRotation(RIGHT_MIDDLE_JOINT3_Rot);

			// Right middle tip - RIGHT_MIDDLE_TIP
			Vector3 RIGHT_MIDDLE_TIP_Pos = Coordinate.GetVectorFromGL(handSkeletonData.right.middle.tip);
			Quaternion RIGHT_MIDDLE_TIP_Rot = Quaternion.LookRotation(RIGHT_MIDDLE_TIP_Pos - RIGHT_MIDDLE_JOINT3_Pos);

			s_BoneData[(int)Bones.RIGHT_MIDDLE_TIP].SetPosition(RIGHT_MIDDLE_TIP_Pos);
			s_BoneData[(int)Bones.RIGHT_MIDDLE_TIP].SetRotation(RIGHT_MIDDLE_TIP_Rot);

			// Right ring joint1 - RIGHT_RING_JOINT1
			Vector3 RIGHT_RING_JOINT1_Pos = Coordinate.GetVectorFromGL(handSkeletonData.right.ring.joint1);
			Quaternion RIGHT_RING_JOINT1_Rot = Quaternion.LookRotation(RIGHT_RING_JOINT1_Pos - RIGHT_WRIST_Pos);

			s_BoneData[(int)Bones.RIGHT_RING_JOINT1].SetPosition(RIGHT_RING_JOINT1_Pos);
			s_BoneData[(int)Bones.RIGHT_RING_JOINT1].SetRotation(RIGHT_RING_JOINT1_Rot);

			// Right ring joint2 - RIGHT_RING_JOINT2
			Vector3 RIGHT_RING_JOINT2_Pos = Coordinate.GetVectorFromGL(handSkeletonData.right.ring.joint2);
			Quaternion RIGHT_RING_JOINT2_Rot = Quaternion.LookRotation(RIGHT_RING_JOINT2_Pos - RIGHT_RING_JOINT1_Pos);

			s_BoneData[(int)Bones.RIGHT_RING_JOINT2].SetPosition(RIGHT_RING_JOINT2_Pos);
			s_BoneData[(int)Bones.RIGHT_RING_JOINT2].SetRotation(RIGHT_RING_JOINT2_Rot);

			// Right ring joint3 - RIGHT_RING_JOINT3
			Vector3 RIGHT_RING_JOINT3_Pos = Coordinate.GetVectorFromGL(handSkeletonData.right.ring.joint3);
			Quaternion RIGHT_RING_JOINT3_Rot = Quaternion.LookRotation(RIGHT_RING_JOINT3_Pos - RIGHT_RING_JOINT2_Pos);

			s_BoneData[(int)Bones.RIGHT_RING_JOINT3].SetPosition(RIGHT_RING_JOINT3_Pos);
			s_BoneData[(int)Bones.RIGHT_RING_JOINT3].SetRotation(RIGHT_RING_JOINT3_Rot);

			// Right ring tip - RIGHT_RING_TIP
			Vector3 RIGHT_RING_TIP_Pos = Coordinate.GetVectorFromGL(handSkeletonData.right.ring.tip);
			Quaternion RIGHT_RING_TIP_Rot = Quaternion.LookRotation(RIGHT_RING_TIP_Pos - RIGHT_RING_JOINT3_Pos);

			s_BoneData[(int)Bones.RIGHT_RING_TIP].SetPosition(RIGHT_RING_TIP_Pos);
			s_BoneData[(int)Bones.RIGHT_RING_TIP].SetRotation(RIGHT_RING_TIP_Rot);

			// Right pinky joint1 - RIGHT_PINKY_JOINT1
			Vector3 RIGHT_PINKY_JOINT1_Pos = Coordinate.GetVectorFromGL(handSkeletonData.right.pinky.joint1);
			Quaternion RIGHT_PINKY_JOINT1_Rot = Quaternion.LookRotation(RIGHT_PINKY_JOINT1_Pos - RIGHT_WRIST_Pos);

			s_BoneData[(int)Bones.RIGHT_PINKY_JOINT1].SetPosition(RIGHT_PINKY_JOINT1_Pos);
			s_BoneData[(int)Bones.RIGHT_PINKY_JOINT1].SetRotation(RIGHT_PINKY_JOINT1_Rot);

			// Right pinky joint2 - RIGHT_PINKY_JOINT2
			Vector3 RIGHT_PINKY_JOINT2_Pos = Coordinate.GetVectorFromGL(handSkeletonData.right.pinky.joint2);
			Quaternion RIGHT_PINKY_JOINT2_Rot = Quaternion.LookRotation(RIGHT_PINKY_JOINT2_Pos - RIGHT_PINKY_JOINT1_Pos);

			s_BoneData[(int)Bones.RIGHT_PINKY_JOINT2].SetPosition(RIGHT_PINKY_JOINT2_Pos);
			s_BoneData[(int)Bones.RIGHT_PINKY_JOINT2].SetRotation(RIGHT_PINKY_JOINT2_Rot);

			// Right pinky joint3 - RIGHT_PINKY_JOINT3
			Vector3 RIGHT_PINKY_JOINT3_Pos = Coordinate.GetVectorFromGL(handSkeletonData.right.pinky.joint3);
			Quaternion RIGHT_PINKY_JOINT3_Rot = Quaternion.LookRotation(RIGHT_PINKY_JOINT3_Pos - RIGHT_PINKY_JOINT2_Pos);

			s_BoneData[(int)Bones.RIGHT_PINKY_JOINT3].SetPosition(RIGHT_PINKY_JOINT3_Pos);
			s_BoneData[(int)Bones.RIGHT_PINKY_JOINT3].SetRotation(RIGHT_PINKY_JOINT3_Rot);

			// Right pinky tip - RIGHT_PINKY_TIP
			Vector3 RIGHT_PINKY_TIP_Pos = Coordinate.GetVectorFromGL(handSkeletonData.right.pinky.tip);
			Quaternion RIGHT_PINKY_TIP_Rot = Quaternion.LookRotation(RIGHT_PINKY_TIP_Pos - RIGHT_PINKY_JOINT3_Pos);

			s_BoneData[(int)Bones.RIGHT_PINKY_TIP].SetPosition(RIGHT_PINKY_TIP_Pos);
			s_BoneData[(int)Bones.RIGHT_PINKY_TIP].SetRotation(RIGHT_PINKY_TIP_Rot);
		}
	}
}
