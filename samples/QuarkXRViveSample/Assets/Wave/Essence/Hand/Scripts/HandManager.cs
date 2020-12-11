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
using Wave.Essence.Events;
using System.Threading;
using System;

namespace Wave.Essence.Hand
{
	[DisallowMultipleComponent]
	public sealed class HandManager : MonoBehaviour
	{
		const string LOG_TAG = "Wave.Essence.Hand.HandManager";
		private void DEBUG(string msg)
		{
			if (Log.EnableDebugLog)
				Log.d(LOG_TAG, msg, true);
		}

		public delegate void HandGestureResultDelegate(object sender, bool result);
		public enum HandGestureStatus
		{
			// Initial, can call Start API in this state.
			NOT_START,
			START_FAILURE,

			// Processing, should NOT call API in this state.
			STARTING,
			STOPING,

			// Running, can call Stop API in this state.
			AVAILABLE,

			// Do nothing.
			UNSUPPORT
		}

		public delegate void HandTrackingResultDelegate(object sender, bool result);
		public enum HandTrackingStatus
		{
			// Initial, can call Start API in this state.
			NOT_START,
			START_FAILURE,

			// Processing, should NOT call API in this state.
			STARTING,
			STOPING,

			// Running, can call Stop API in this state.
			AVAILABLE,

			// Do nothing.
			UNSUPPORT
		}


		public static string HAND_STATIC_GESTURE_LEFT = "HAND_STATIC_GESTURE_LEFT";
		public static string HAND_STATIC_GESTURE_RIGHT = "HAND_STATIC_GESTURE_RIGHT";
		public static string HAND_DYNAMIC_GESTURE_LEFT = "HAND_DYNAMIC_GESTURE_LEFT";
		public static string HAND_DYNAMIC_GESTURE_RIGHT = "HAND_DYNAMIC_GESTURE_RIGHT";
		public static string HAND_GESTURE_STATUS = "HAND_GESTURE_STATUS";
		public static string HAND_TRACKING_STATUS = "HAND_TRACKING_STATUS";


		private static HandManager m_Instance = null;
		public static HandManager Instance { get { return m_Instance; } }


		// ------------------- Pointer Related begins -------------------
		#region Pointer variables.
		public enum HandType
		{
			RIGHT = 0,
			LEFT = 1
		};

		public static HandType FocusedHand = HandType.RIGHT;
		#endregion
		// ------------------- Pointer Related ends -------------------


		public enum StaticGestures
		{
			UNKNOWN = 1 << (int)WVR_HandGestureType.WVR_HandGestureType_Unknown,
			FIST = 1 << (int)WVR_HandGestureType.WVR_HandGestureType_Fist,
			FIVE = 1 << (int)WVR_HandGestureType.WVR_HandGestureType_Five,
			OK = 1 << (int)WVR_HandGestureType.WVR_HandGestureType_OK,
			THUMBUP = 1 << (int)WVR_HandGestureType.WVR_HandGestureType_ThumbUp,
			INDEXUP = 1 << (int)WVR_HandGestureType.WVR_HandGestureType_IndexUp,
		}

		private bool m_EnableHandGestureEx = true;
		[SerializeField]
		private bool m_EnableHandGesture = true;
		public bool EnableHandGesture { get { return m_EnableHandGesture; } set { m_EnableHandGesture = value; } }

		private bool m_EnableHandTrackingEx = true;
		[SerializeField]
		private bool m_EnableHandTracking = true;
		public bool EnableHandTracking { get { return m_EnableHandTracking; } set { m_EnableHandTracking = value; } }

		private ulong supportedFeature = 0;

		private WVR_HandGestureType m_HandGestureLeftEx = WVR_HandGestureType.WVR_HandGestureType_Invalid;
		private WVR_HandGestureType m_HandGestureLeft = WVR_HandGestureType.WVR_HandGestureType_Invalid;
		private WVR_HandGestureType m_HandGestureRightEx = WVR_HandGestureType.WVR_HandGestureType_Invalid;
		private WVR_HandGestureType m_HandGestureRight = WVR_HandGestureType.WVR_HandGestureType_Invalid;

		#region MonoBehaviour Overrides
		void Awake()
		{
			if (m_Instance == null)
				m_Instance = this;

			supportedFeature = Interop.WVR_GetSupportedFeatures();
		}

		void Start()
		{
			m_EnableHandGestureEx = m_EnableHandGesture;
			if (m_EnableHandGesture)
			{
				DEBUG("Start() Start hand gesture.");
				StartHandGesture();
			}

			m_EnableHandTrackingEx = m_EnableHandTracking;
			if (m_EnableHandTracking)
			{
				DEBUG("Start() Start hand tracking.");
				StartHandTracking();
			}
		}

		void Update()
		{
			if (m_EnableHandGestureEx != m_EnableHandGesture)
			{
				m_EnableHandGestureEx = m_EnableHandGesture;
				if (m_EnableHandGesture)
				{
					DEBUG("Update() Start hand gesture.");
					StartHandGesture();
				}
				if (!m_EnableHandGesture)
				{
					DEBUG("Update() Stop hand gesture.");
					StopHandGesture();
				}
			}

			if (m_EnableHandTrackingEx != m_EnableHandTracking)
			{
				m_EnableHandTrackingEx = m_EnableHandTracking;
				if (m_EnableHandTracking)
				{
					DEBUG("Update() Start hand tracking.");
					StartHandTracking();
				}
				if (!m_EnableHandTracking)
				{
					DEBUG("Update() Stop hand tracking.");
					StopHandTracking();
				}
			}

			if (m_EnableHandGesture)
			{
				GetHandGestureData();
				if (hasHandGestureData)
				{
					UpdateHandGestureDataLeft();
					UpdateHandGestureDataRight();
				}
			}

			if (m_EnableHandTracking)
				GetHandTrackingData();
		}

		void OnEnable()
		{
			SystemEvent.Listen(WVR_EventType.WVR_EventType_HandGesture_Abnormal, OnEvent);
			SystemEvent.Listen(WVR_EventType.WVR_EventType_HandTracking_Abnormal, OnEvent);
		}

		void OnDisable()
		{
			SystemEvent.Remove(WVR_EventType.WVR_EventType_HandGesture_Abnormal, OnEvent);
			SystemEvent.Remove(WVR_EventType.WVR_EventType_HandTracking_Abnormal, OnEvent);
		}
		#endregion

		void OnEvent(WVR_Event_t systemEvent)
		{
			switch (systemEvent.common.type)
			{
				case WVR_EventType.WVR_EventType_HandGesture_Abnormal:
					DEBUG("OnEvent() WVR_EventType_HandGesture_Abnormal, restart the hand gesture component.");
					RestartHandGesture();
					break;
				case WVR_EventType.WVR_EventType_HandTracking_Abnormal:
					DEBUG("OnEvent() WVR_EventType_HandTracking_Abnormal, restart the hand tracking component.");
					RestartHandTracking();
					break;
				default:
					break;
			}
		}

		#region Hand Gesture Lifecycle
		private HandGestureStatus m_HandGestureStatus = HandGestureStatus.NOT_START;
		private static ReaderWriterLockSlim m_HandGestureStatusRWLock = new ReaderWriterLockSlim();
		private void SetHandGestureStatus(HandGestureStatus status)
		{
			try
			{
				m_HandGestureStatusRWLock.TryEnterWriteLock(2000);
				m_HandGestureStatus = status;
			}
			catch (Exception e)
			{
				Log.e(LOG_TAG, "SetHandGestureStatus() " + e.Message, true);
				throw;
			}
			finally
			{
				m_HandGestureStatusRWLock.ExitWriteLock();
			}
		}

		public HandGestureStatus GetHandGestureStatus()
		{
			if ((supportedFeature & (ulong)WVR_SupportedFeature.WVR_SupportedFeature_HandGesture) == 0)
				return HandGestureStatus.UNSUPPORT;

			try
			{
				m_HandGestureStatusRWLock.TryEnterReadLock(2000);
				return m_HandGestureStatus;
			}
			catch (Exception e)
			{
				Log.e(LOG_TAG, "GetHandGestureStatus() " + e.Message, true);
				throw;
			}
			finally
			{
				m_HandGestureStatusRWLock.ExitReadLock();
			}
		}

		private object handGestureThreadLock = new object();
		private event HandGestureResultDelegate handGestureResultCB = null;
		private void StartHandGestureLock()
		{
			bool result = false;

			HandGestureStatus status = GetHandGestureStatus();
			if (m_EnableHandGesture &&
				(
					status == HandGestureStatus.NOT_START ||
					status == HandGestureStatus.START_FAILURE
				)
			)
			{
				SetHandGestureStatus(HandGestureStatus.STARTING);
				result = Interop.WVR_StartHandGesture() == WVR_Result.WVR_Success ? true : false;
				SetHandGestureStatus(result ? HandGestureStatus.AVAILABLE : HandGestureStatus.START_FAILURE);
			}

			status = GetHandGestureStatus();
			DEBUG("StartHandGestureLock() " + result + ", status: " + status);
			GeneralEvent.Send(HAND_GESTURE_STATUS, status);

			if (handGestureResultCB != null)
			{
				handGestureResultCB(this, result);
				handGestureResultCB = null;
			}
		}

		private void StartHandGestureThread()
		{
			lock (handGestureThreadLock)
			{
				DEBUG("StartHandGestureThread()");
				StartHandGestureLock();
			}
		}

		private void StartHandGesture()
		{
			Thread hand_gesture_t = new Thread(StartHandGestureThread);
			hand_gesture_t.Start();
		}

		private void StopHandGestureLock()
		{
			HandGestureStatus status = GetHandGestureStatus();
			if (status == HandGestureStatus.AVAILABLE)
			{
				DEBUG("StopHandGestureLock()");
				SetHandGestureStatus(HandGestureStatus.STOPING);
				Interop.WVR_StopHandGesture();
				SetHandGestureStatus(HandGestureStatus.NOT_START);
				hasHandGestureData = false;
			}

			status = GetHandGestureStatus();
			GeneralEvent.Send(HAND_GESTURE_STATUS, status);
		}

		private void StopHandGestureThread()
		{
			lock (handGestureThreadLock)
			{
				DEBUG("StopHandGestureThread()");
				StopHandGestureLock();
			}
		}

		private void StopHandGesture()
		{
			Thread hand_gesture_t = new Thread(StopHandGestureThread);
			hand_gesture_t.Start();
		}

		private void RestartHandGestureThread()
		{
			lock (handGestureThreadLock)
			{
				DEBUG("RestartHandGestureThread()");
				StopHandGestureLock();
				StartHandGestureLock();
			}
		}

		public void RestartHandGesture()
		{
			Thread hand_gesture_t = new Thread(RestartHandGestureThread);
			hand_gesture_t.Start();
		}

		public void RestartHandGesture(HandGestureResultDelegate callback)
		{
			if (handGestureResultCB == null)
				handGestureResultCB = callback;
			else
				handGestureResultCB += callback;

			RestartHandGesture();
		}
		#endregion

		#region Hand Tracking Lifecycle
		private HandTrackingStatus m_HandTrackingStatus = HandTrackingStatus.NOT_START;
		private static ReaderWriterLockSlim m_HandTrackingStatusRWLock = new ReaderWriterLockSlim();
		private void SetHandTrackingStatus(HandTrackingStatus status)
		{
			try
			{
				m_HandTrackingStatusRWLock.TryEnterWriteLock(2000);
				m_HandTrackingStatus = status;
			}
			catch (Exception e)
			{
				Log.e(LOG_TAG, "SetHandTrackingStatus() " + e.Message, true);
				throw;
			}
			finally
			{
				m_HandTrackingStatusRWLock.ExitWriteLock();
			}
		}

		public HandTrackingStatus GetHandTrackingStatus()
		{
			if ((supportedFeature & (ulong)WVR_SupportedFeature.WVR_SupportedFeature_HandTracking) == 0)
				return HandTrackingStatus.UNSUPPORT;

			try
			{
				m_HandTrackingStatusRWLock.TryEnterReadLock(2000);
				return m_HandTrackingStatus;
			}
			catch (Exception e)
			{
				Log.e(LOG_TAG, "GetHandTrackingStatus() " + e.Message, true);
				throw;
			}
			finally
			{
				m_HandTrackingStatusRWLock.ExitReadLock();
			}
		}

		private object handTrackingThreadLocker = new object();
		private event HandTrackingResultDelegate handTrackingResultCB = null;
		private void StartHandTrackingLock()
		{
			bool result = false;

			HandTrackingStatus status = GetHandTrackingStatus();
			if (m_EnableHandTracking &&
				(
					status == HandTrackingStatus.NOT_START ||
					status == HandTrackingStatus.START_FAILURE
				)
			)
			{
				SetHandTrackingStatus(HandTrackingStatus.STARTING);
				result = Interop.WVR_StartHandTracking() == WVR_Result.WVR_Success ? true : false;
				SetHandTrackingStatus(result ? HandTrackingStatus.AVAILABLE : HandTrackingStatus.START_FAILURE);
			}

			status = GetHandTrackingStatus();
			DEBUG("StartHandTrackingLock() " + result + ", status: " + status);
			GeneralEvent.Send(HAND_TRACKING_STATUS, status);

			if (handTrackingResultCB != null)
			{
				handTrackingResultCB(this, result);
				handTrackingResultCB = null;
			}
		}

		private void StartHandTrackingThread()
		{
			lock (handTrackingThreadLocker)
			{
				DEBUG("StartHandTrackingThread()");
				StartHandTrackingLock();
			}
		}

		private void StartHandTracking()
		{
			Thread hand_tracking_t = new Thread(StartHandTrackingThread);
			hand_tracking_t.Start();
		}

		private void StopHandTrackingLock()
		{
			HandTrackingStatus status = GetHandTrackingStatus();
			if (status == HandTrackingStatus.AVAILABLE)
			{
				DEBUG("StopHandTrackingLock()");
				SetHandTrackingStatus(HandTrackingStatus.STOPING);
				Interop.WVR_StopHandTracking();
				SetHandTrackingStatus(HandTrackingStatus.NOT_START);
				hasHandTrackingData = false;
			}

			status = GetHandTrackingStatus();
			GeneralEvent.Send(HAND_TRACKING_STATUS, status);
		}

		private void StopHandTrackingThread()
		{
			lock (handTrackingThreadLocker)
			{
				DEBUG("StopHandTrackingThread()");
				StopHandTrackingLock();
			}
		}

		private void StopHandTracking()
		{
			Thread hand_tracking_t = new Thread(StopHandTrackingThread);
			hand_tracking_t.Start();
		}

		private void RestartHandTrackingThread()
		{
			lock (handTrackingThreadLocker)
			{
				DEBUG("RestartHandTrackingThread()");
				StopHandTrackingLock();
				StartHandTrackingLock();
			}
		}

		public void RestartHandTracking()
		{
			Thread hand_tracking_t = new Thread(RestartHandTrackingThread);
			hand_tracking_t.Start();
		}

		public void RestartHandTracking(HandTrackingResultDelegate callback)
		{
			if (handTrackingResultCB == null)
				handTrackingResultCB = callback;
			else
				handTrackingResultCB += callback;

			RestartHandTracking();
		}
		#endregion

		#region Hand Gesture API
		private bool hasHandGestureData = false;
		private WVR_HandGestureData_t handGestureData = new WVR_HandGestureData_t();
		private void GetHandGestureData()
		{
			HandGestureStatus status = GetHandGestureStatus();
			if (status == HandGestureStatus.AVAILABLE)
				hasHandGestureData = Interop.WVR_GetHandGestureData(ref handGestureData) == WVR_Result.WVR_Success ? true : false;
		}

		private void UpdateHandGestureDataLeft()
		{
			m_HandGestureLeftEx = m_HandGestureLeft;
			m_HandGestureLeft = handGestureData.left;

			if (m_HandGestureLeft != m_HandGestureLeftEx)
			{
				DEBUG("UpdateHandGestureDataLeft() Receives " + m_HandGestureLeft);
				GeneralEvent.Send(HAND_STATIC_GESTURE_LEFT, m_HandGestureLeft);
			}
		}

		public ulong GetHandGestureLeft()
		{
			ulong gesture_value = 0;
			switch (m_HandGestureLeft)
			{
				case WVR_HandGestureType.WVR_HandGestureType_Fist:
					gesture_value = (ulong)StaticGestures.FIST;
					break;
				case WVR_HandGestureType.WVR_HandGestureType_Five:
					gesture_value = (ulong)StaticGestures.FIVE;
					break;
				case WVR_HandGestureType.WVR_HandGestureType_IndexUp:
					gesture_value = (ulong)StaticGestures.INDEXUP;
					break;
				case WVR_HandGestureType.WVR_HandGestureType_ThumbUp:
					gesture_value = (ulong)StaticGestures.THUMBUP;
					break;
				case WVR_HandGestureType.WVR_HandGestureType_OK:
					gesture_value = (ulong)StaticGestures.OK;
					break;
				default:
					break;
			}
			return gesture_value;
		}

		private void UpdateHandGestureDataRight()
		{
			m_HandGestureRightEx = m_HandGestureRight;
			m_HandGestureRight = handGestureData.right;

			if (m_HandGestureRight != m_HandGestureRightEx)
			{
				DEBUG("UpdateHandGestureDataLeft() Receives " + m_HandGestureRight);
				GeneralEvent.Send(HAND_STATIC_GESTURE_RIGHT, m_HandGestureRight);
			}
		}

		public ulong GetHandGestureRight()
		{
			ulong gesture_value = 0;
			switch (m_HandGestureRight)
			{
				case WVR_HandGestureType.WVR_HandGestureType_Fist:
					gesture_value = (ulong)StaticGestures.FIST;
					break;
				case WVR_HandGestureType.WVR_HandGestureType_Five:
					gesture_value = (ulong)StaticGestures.FIVE;
					break;
				case WVR_HandGestureType.WVR_HandGestureType_IndexUp:
					gesture_value = (ulong)StaticGestures.INDEXUP;
					break;
				case WVR_HandGestureType.WVR_HandGestureType_ThumbUp:
					gesture_value = (ulong)StaticGestures.THUMBUP;
					break;
				case WVR_HandGestureType.WVR_HandGestureType_OK:
					gesture_value = (ulong)StaticGestures.OK;
					break;
				default:
					break;
			}
			return gesture_value;
		}
		#endregion

		#region Hand Tracking API
		private bool hasHandTrackingData = false;
		private WVR_HandSkeletonData_t handSkeletonData = new WVR_HandSkeletonData_t();
		private WVR_HandPoseData_t handPoseData = new WVR_HandPoseData_t();
		private WVR_PoseOriginModel originModel = WVR_PoseOriginModel.WVR_PoseOriginModel_OriginOnHead;
		public void GetHandTrackingData()
		{
			HandTrackingStatus status = GetHandTrackingStatus();
			if (status == HandTrackingStatus.AVAILABLE)
			{
				ClientInterface.GetOrigin(ref originModel);
				hasHandTrackingData = Interop.WVR_GetHandTrackingData(ref handSkeletonData, ref handPoseData, originModel) == WVR_Result.WVR_Success ? true : false;
			}
		}

		public bool GetHandSkeletonData(ref WVR_HandSkeletonData_t skeleton)
		{
			skeleton = handSkeletonData;
			return hasHandTrackingData;
		}

		public bool GetHandPoseData(ref WVR_HandPoseData_t pose)
		{
			pose = handPoseData;
			return hasHandTrackingData;
		}

		public float GetHandConfidence(HandType hand)
		{
			if (hasHandTrackingData)
			{
				if (hand == HandType.LEFT)
					return handSkeletonData.left.confidence;
				if (hand == HandType.RIGHT)
					return handSkeletonData.right.confidence;
			}
			return 0;
		}

		public bool IsHandPoseValid(HandType hand)
		{
			if (hasHandTrackingData)
			{
				if (hand == HandType.LEFT)
					return (handSkeletonData.left.confidence > 0.1f);
				if (hand == HandType.RIGHT)
					return (handSkeletonData.right.confidence > 0.1f);
			}
			return false;
		}
		#endregion
	}
}
