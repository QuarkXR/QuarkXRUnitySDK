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
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Wave.Native;

namespace Wave.Essence.Hand
{
	[DisallowMultipleComponent]
	public class HandInputModule : BaseInputModule
	{
		private const string LOG_TAG = "Wave.Essence.Hand.HandInputModule";
		private void DEBUG(string msg)
		{
			if (Log.EnableDebugLog)
				Log.d(LOG_TAG, msg, true);
		}
		private void INFO(string msg) { Log.i(LOG_TAG, msg, true); }


		#region Customized Settings
		[Tooltip("If not selected, no events will be sent.")]
		[SerializeField]
		private bool m_EnableEvent = true;
		public bool EnableEvent { get { return m_EnableEvent; } set { m_EnableEvent = value; } }

		[Tooltip("Set the right hand selector used to point objects in a scene when the hand gesture is pinch.")]
		[SerializeField]
		private GameObject m_RightPinchSelector = null;
		public GameObject RightPinchSelector { get { return m_RightPinchSelector; } set { m_RightPinchSelector = value; } }

		[Tooltip("Set the left hand selector used to point objects in a scene when the hand gesture is pinch.")]
		[SerializeField]
		private GameObject m_LeftPinchSelector = null;
		public GameObject LeftPinchSelector { get { return m_LeftPinchSelector; } set { m_LeftPinchSelector = value; } }

		[Tooltip("The threshold of pinch on.")]
		[SerializeField]
		[Range(0.5f, 1)]
		private float m_PinchOnThreshold = 0.7f;
		public float PinchOnThreshold { get { return m_PinchOnThreshold; } set { m_PinchOnThreshold = value; } }

		[SerializeField]
		[Tooltip("Start dragging when pinching over this duration of time in seconds.")]
		private float m_PinchTimeToDrag = 1.0f;
		public float PinchTimeToDrag { get { return m_PinchTimeToDrag; } set { m_PinchTimeToDrag = value; } }

		[SerializeField]
		[Range(0.5f, 1)]
		[Tooltip("The threshold of pinch off.")]
		private float m_PinchOffThreshold = 0.7f;
		public float PinchOffThreshold { get { return m_PinchOffThreshold; } set { m_PinchOffThreshold = value; } }
		#endregion


		private void ActivateBeamPointer(HandManager.HandType hand, bool active)
		{
			GameObject beam = HandBeamProvider.Instance.GetHandBeam(hand);
			if (beam != null && beam.GetComponent<HandBeam>() != null)
				beam.GetComponent<HandBeam>().ShowBeam = active;

			GameObject pointer = HandPointerProvider.Instance.GetHandPointer(hand);
			if (pointer != null && pointer.GetComponent<HandSpotPointer>() != null)
				pointer.GetComponent<HandSpotPointer>().ShowPointer = active;
		}

		/// HandBeam and HandPointer
		private GameObject beamObject = null;
		private HandBeam m_HandBeam = null;
		private GameObject pointerObject = null;
		private HandSpotPointer m_HandSpotPointer = null;

		/// HandPointerTracker to track the HandPointer
		private GameObject pointerTrackerObject = null;
		private HandPointerTracker m_HandPointerTracker = null;

		/// Camera and PhysicsRaycaster from HandPointerTracker
		private Camera m_Camera = null;
		private PhysicsRaycaster m_PhysicsRaycaster = null;
		private bool ValidateParameters()
		{
			// 1. Enables events.
			if (!m_EnableEvent)
			{
				ActivateBeamPointer(HandManager.HandType.RIGHT, false);
				ActivateBeamPointer(HandManager.HandType.LEFT, false);
				return false;
			}

			// 2. Validates the pinch on/off threshold.
			if (m_PinchOffThreshold > m_PinchOnThreshold)
				m_PinchOffThreshold = m_PinchOnThreshold;

			// 3. Validates the beam and pointer.
			GameObject new_beam = HandBeamProvider.Instance.GetHandBeam(HandManager.FocusedHand);
			if (new_beam != null && !ReferenceEquals(beamObject, new_beam))
			{
				beamObject = new_beam;
				m_HandBeam = beamObject.GetComponent<HandBeam>();
			}
			if (beamObject == null)
				m_HandBeam = null;

			GameObject new_pointer = HandPointerProvider.Instance.GetHandPointer(HandManager.FocusedHand);
			if (new_pointer != null && !GameObject.ReferenceEquals(pointerObject, new_pointer))
			{
				pointerObject = new_pointer;
				m_HandSpotPointer = pointerObject.GetComponent<HandSpotPointer>();
			}
			if (pointerObject == null)
				m_HandSpotPointer = null;

			if (m_HandBeam == null || m_HandSpotPointer == null)
			{
				if (Log.gpl.Print)
				{
					if (m_HandBeam == null)
						Log.i(LOG_TAG, "ValidateParameters() No beam of " + HandManager.FocusedHand, true);
					if (m_HandSpotPointer == null)
						Log.i(LOG_TAG, "ValidateParameters() No pointer of " + HandManager.FocusedHand, true);
				}
				return false;
			}

			// 4. Validates the Camera and PhysicsRaycaster.
			if (m_HandPointerTracker != null)
			{
				if (m_Camera == null)
					m_Camera = m_HandPointerTracker.GetPointerTrackerCamera();
				if (m_PhysicsRaycaster == null)
					m_PhysicsRaycaster = m_HandPointerTracker.GetPhysicsRaycaster();
			}

			if (m_Camera == null)
			{
				if (Log.gpl.Print)
				{
					if (m_Camera == null)
						Log.i(LOG_TAG, "ValidateParameters() Forget to put HandPointerTracker??");
				}
				return false;
			}

			return true;
		}


		#region BaseInputModule Overrides
		private bool m_Enabled = false;
		protected override void OnEnable()
		{
			if (!m_Enabled)
			{
				base.OnEnable();
				DEBUG("OnEnable()");

				Destroy(GetComponent<StandaloneInputModule>());
				if (HandPointerTracker.Instance == null)
				{
					if (Camera.main != null)
					{
						pointerTrackerObject = new GameObject("HandPointerTracker");
						pointerTrackerObject.transform.SetParent(Camera.main.gameObject.transform, false);
						pointerTrackerObject.transform.localPosition = Vector3.zero;
						m_HandPointerTracker = pointerTrackerObject.AddComponent<HandPointerTracker>();
					}
				}
				else
				{
					m_HandPointerTracker = HandPointerTracker.Instance;
				}

				m_Enabled = true;
			}
		}

		protected override void OnDisable()
		{
			if (m_Enabled)
			{
				base.OnDisable();
				DEBUG("OnDisable()");

				m_Enabled = false;
			}
		}

		private WVR_HandPoseType m_HandGestureRight = WVR_HandPoseType.WVR_HandPoseType_Invalid;
		private WVR_HandPoseType m_HandGestureLeft = WVR_HandPoseType.WVR_HandPoseType_Invalid;

		private bool hasHandPoseData = false;
		private WVR_HandPoseData_t handPoseData = new WVR_HandPoseData_t();
		private bool IsHandFocusSwitched()
		{
			if (HandManager.FocusedHand == HandManager.HandType.RIGHT)
			{
				// Switch the focus hand to left.
				if ((m_HandGestureLeft == WVR_HandPoseType.WVR_HandPoseType_Pinch) && (handPoseData.left.pinch.strength >= m_PinchOnThreshold))
				{
					HandManager.FocusedHand = HandManager.HandType.LEFT;
					return true;
				}
			}

			if (HandManager.FocusedHand == HandManager.HandType.LEFT)
			{
				// Switch the focus hand to right.
				if ((m_HandGestureRight == WVR_HandPoseType.WVR_HandPoseType_Pinch) && (handPoseData.right.pinch.strength >= m_PinchOnThreshold))
				{
					HandManager.FocusedHand = HandManager.HandType.RIGHT;
					return true;
				}
			}

			return false;
		}

		private Vector3 m_PinchOriginRight = Vector3.zero, m_PinchDirectionRight = Vector3.zero;
		private Vector3 m_PinchOriginLeft = Vector3.zero, m_PinchDirectionLeft = Vector3.zero;

		private bool isPinch = false;
		private const uint PINCH_FRAME_COUNT = 10;
		private uint pinchFrame = 0, unpinchFrame = 0;
		private void LegalizeBeamPointerOnPinch()
		{
			bool effective = false;
			/**
			 * Set the beam and pointer to effective when
			 * Not pinch currently and, 1 or 2 happens.
			 * 1. Focused hand is right and right pinch strength is enough.
			 * 2. Focused hand is left and left pinch strength is enough.
			 **/
			if (!isPinch)
			{
				if (((HandManager.FocusedHand == HandManager.HandType.RIGHT) &&
					 ((m_HandGestureRight == WVR_HandPoseType.WVR_HandPoseType_Pinch) && (handPoseData.right.pinch.strength >= m_PinchOnThreshold))
					) ||
					((HandManager.FocusedHand == HandManager.HandType.LEFT) &&
					 ((m_HandGestureLeft == WVR_HandPoseType.WVR_HandPoseType_Pinch) && (handPoseData.left.pinch.strength >= m_PinchOnThreshold))
					)
				)
				{
					effective = true;
				}
			}
			if (effective)
			{
				pinchFrame++;
				if (pinchFrame > PINCH_FRAME_COUNT)
				{
					isPinch = true;
					m_HandBeam?.SetEffectiveBeam(true);
					m_HandSpotPointer?.SetEffectivePointer(true);
					unpinchFrame = 0;
				}
			}

			bool uneffective = false;
			/**
			 * Set the beam and pointer to uneffective when
			 * Is pinching currently and, 1 or 2 happens.
			 * 1. Focused hand is right and, right gesture is not pinch or right pinch strength is not enough.
			 * 2. Focused hand is left and, left gesture is not pinch or left pinch strength is not enough.
			 **/
			if (isPinch)
			{
				if (((HandManager.FocusedHand == HandManager.HandType.RIGHT) &&
					 ((m_HandGestureRight != WVR_HandPoseType.WVR_HandPoseType_Pinch) || (handPoseData.right.pinch.strength < m_PinchOffThreshold))
					) ||
					((HandManager.FocusedHand == HandManager.HandType.LEFT) &&
					 ((m_HandGestureLeft != WVR_HandPoseType.WVR_HandPoseType_Pinch) || (handPoseData.left.pinch.strength < m_PinchOffThreshold))
					)
				)
				{
					uneffective = true;
				}
			}
			if (uneffective)
			{
				unpinchFrame++;
				if (unpinchFrame > PINCH_FRAME_COUNT)
				{
					isPinch = false;
					m_HandBeam?.SetEffectiveBeam(false);
					m_HandSpotPointer?.SetEffectivePointer(false);
					pinchFrame = 0;
				}
			}
		}

		public override void Process()
		{
			/// 0. Checks the necessary conditions.
			/// DO NOT process when one of conditions below happens:
			/// 1. Hand Input Event is disabled.
			/// 2. Focused Hand does NOT have a selector with a beam and a pointer.
			/// 3. No event camera.
			/// Also, updates the beam and pointer of focused hand.
			if (!ValidateParameters())
				return;

			/// 1. Save previous raycasted object.
			prevRaycastedObject = GetRaycastedObject();

			/// 2. Updates hand pose related data.
			if (HandManager.Instance != null)
				hasHandPoseData = HandManager.Instance.GetHandPoseData(ref handPoseData);
			if (hasHandPoseData)
			{
				m_HandGestureRight = handPoseData.right.state.type;
				Coordinate.GetVectorFromGL(handPoseData.right.pinch.origin, out m_PinchOriginRight);
				Coordinate.GetVectorFromGL(handPoseData.right.pinch.direction, out m_PinchDirectionRight);
				m_HandGestureLeft = handPoseData.left.state.type;
				Coordinate.GetVectorFromGL(handPoseData.left.pinch.origin, out m_PinchOriginLeft);
				Coordinate.GetVectorFromGL(handPoseData.left.pinch.direction, out m_PinchDirectionLeft);
			}
			else
			{
				m_HandGestureRight = WVR_HandPoseType.WVR_HandPoseType_Invalid;
				m_HandGestureLeft = WVR_HandPoseType.WVR_HandPoseType_Invalid;
			}

			/// 3. Updates the selector pose with hand pose data.
			if (m_RightPinchSelector != null)
			{
				m_RightPinchSelector.transform.position = m_PinchOriginRight;
				if (!m_PinchDirectionRight.Equals(Vector3.zero))
					m_RightPinchSelector.transform.rotation = Quaternion.LookRotation(m_PinchDirectionRight);
			}

			if (m_LeftPinchSelector != null)
			{
				m_LeftPinchSelector.transform.position = m_PinchOriginLeft;
				if (!m_PinchDirectionLeft.Equals(Vector3.zero))
					m_LeftPinchSelector.transform.rotation = Quaternion.LookRotation(m_PinchDirectionLeft);
			}

			/// 4. Checks the hand pose data and switches the focused hand if needed.
			///    If the focused hand is switched, skip following actions.
			if (IsHandFocusSwitched())
				return;

			/// 5. The beam and pointer will become effective when pinching and uneffective when not pinching.
			/// isPinch is updated here.
			LegalizeBeamPointerOnPinch();

			/// 6. Shows the beam and pointer of the focused hand when the pose is valid and
			///    Hides the beam and pointer of the non-focused hand.
			if (HandManager.FocusedHand == HandManager.HandType.RIGHT)
			{
				ActivateBeamPointer(HandManager.HandType.LEFT, false);
				bool valid_pose = IBonePose.Instance.IsHandPoseValid(HandManager.HandType.RIGHT);
				ActivateBeamPointer(HandManager.HandType.RIGHT, valid_pose);
			}
			else // FocusedHand == LEFT
			{
				ActivateBeamPointer(HandManager.HandType.RIGHT, false);
				bool valid_pose = IBonePose.Instance.IsHandPoseValid(HandManager.HandType.LEFT);
				ActivateBeamPointer(HandManager.HandType.LEFT, valid_pose);
			}

			/// 7. Raycasts when not dragging.
			if ((mPointerEventData == null) ||
				(mPointerEventData != null && !mPointerEventData.dragging))
			{
				ResetPointerEventData();
				GraphicRaycast();
				PhysicsRaycast();
			}

			/// 8. Shows the pointer when casting to an object. Hides the pointer when not casting to any object.
			GameObject curr_raycasted_object = GetRaycastedObject();
			if (curr_raycasted_object != null)
				m_HandSpotPointer.OnPointerEnter(curr_raycasted_object, Vector3.zero, false);
			else
				m_HandSpotPointer.OnPointerExit(prevRaycastedObject);

			/// 9. If the pinch origin is invalid, do NOT send event at this frame.
			/// If dragging before, will keep dragging.
			bool send_event =
				((HandManager.FocusedHand == HandManager.HandType.RIGHT) && (m_PinchOriginRight != Vector3.zero)) ||
				((HandManager.FocusedHand == HandManager.HandType.LEFT) && (m_PinchOriginLeft != Vector3.zero));
			if (send_event)
			{
				OnGraphicPointerEnterExit();
				OnPhysicsPointerEnterExit();

				OnPointerHover();

				if (!mPointerEventData.eligibleForClick)
				{
					if (isPinch)
						OnPointerDown();
				}
				else if (mPointerEventData.eligibleForClick)
				{
					if (isPinch)
					{
						// Down before, and receives the selected gesture continuously.
						OnPointerDrag();

					}
					else
					{
						DEBUG("Focus hand: " + HandManager.FocusedHand
							+ ", right strength: " + handPoseData.right.pinch.strength
							+ ", left strength: " + handPoseData.left.pinch.strength);
						// Down before, but not receive the selected gesture.
						OnPointerUp();
					}
				}
			}


			Vector3 intersection_position = GetIntersectionPosition(mPointerEventData.pointerCurrentRaycast);
		}
		#endregion


		#region Pinch Selector Control
		private Quaternion toRotation = Quaternion.identity;
		private void RotateSelector(GameObject selector, Quaternion fromRotation)
		{
			if (HandManager.FocusedHand == HandManager.HandType.RIGHT)
				toRotation = IBonePose.Instance.GetBoneTransform(BonePoseImpl.Bones.RIGHT_WRIST).rot * Quaternion.Inverse(fromRotation);
			else
				toRotation = IBonePose.Instance.GetBoneTransform(BonePoseImpl.Bones.LEFT_WRIST).rot * Quaternion.Inverse(fromRotation);

			selector.transform.rotation *= toRotation;
		}
		private void MoveSelector(GameObject selector, Vector3 offset)
		{
			if (HandManager.FocusedHand == HandManager.HandType.RIGHT)
				selector.transform.position = IBonePose.Instance.GetBoneTransform(BonePoseImpl.Bones.RIGHT_WRIST).pos + offset;
			if (HandManager.FocusedHand == HandManager.HandType.LEFT)
				selector.transform.position = IBonePose.Instance.GetBoneTransform(BonePoseImpl.Bones.LEFT_WRIST).pos + offset;
		}
		#endregion


		#region Raycast
		private PointerEventData mPointerEventData = null;
		private void ResetPointerEventData()
		{
			if (mPointerEventData == null)
			{
				mPointerEventData = new PointerEventData(eventSystem);
				mPointerEventData.pointerCurrentRaycast = new RaycastResult();
			}

			mPointerEventData.Reset();
			mPointerEventData.position = new Vector2(0.5f * m_Camera.pixelWidth, 0.5f * m_Camera.pixelHeight); // center of screen
			firstRaycastResult.Clear();
			mPointerEventData.pointerCurrentRaycast = firstRaycastResult;
		}

		private GameObject prevRaycastedObject = null;
		private GameObject GetRaycastedObject()
		{
			if (mPointerEventData == null)
				return null;

			return mPointerEventData.pointerCurrentRaycast.gameObject;
		}

		private Vector3 GetIntersectionPosition(RaycastResult raycastResult)
		{
			if (m_Camera == null)
				return Vector3.zero;

			float intersectionDistance = raycastResult.distance + m_Camera.nearClipPlane;
			Vector3 intersectionPosition = m_Camera.transform.forward * intersectionDistance + m_Camera.transform.position;
			return intersectionPosition;
		}

		private List<RaycastResult> GetResultList(List<RaycastResult> originList)
		{
			List<RaycastResult> result_list = new List<RaycastResult>();
			for (int i = 0; i < originList.Count; i++)
			{
				if (originList[i].gameObject != null)
					result_list.Add(originList[i]);
			}
			return result_list;
		}

		private RaycastResult SelectRaycastResult(RaycastResult currResult, RaycastResult nextResult)
		{
			if (currResult.gameObject == null)
				return nextResult;
			if (nextResult.gameObject == null)
				return currResult;

			if (currResult.worldPosition == Vector3.zero)
				currResult.worldPosition = GetIntersectionPosition(currResult);

			float curr_distance = (float)Math.Round(Mathf.Abs(currResult.worldPosition.z - currResult.module.eventCamera.transform.position.z), 3);

			if (nextResult.worldPosition == Vector3.zero)
				nextResult.worldPosition = GetIntersectionPosition(nextResult);

			float next_distance = (float)Math.Round(Mathf.Abs(nextResult.worldPosition.z - currResult.module.eventCamera.transform.position.z), 3);

			// 1. Check the distance.
			if (next_distance > curr_distance)
				return currResult;

			if (next_distance < curr_distance)
			{
				DEBUG("SelectRaycastResult() "
					+ nextResult.gameObject.name + ", position: " + nextResult.worldPosition
					+ ", distance: " + next_distance
					+ " is smaller than "
					+ currResult.gameObject.name + ", position: " + currResult.worldPosition
					+ ", distance: " + curr_distance
					);

				return nextResult;
			}

			// 2. Check the "Order in Layer" of the Canvas.
			if (nextResult.sortingOrder > currResult.sortingOrder)
				return nextResult;

			return currResult;
		}

		private RaycastResult m_Result = new RaycastResult();
		private RaycastResult FindFirstResult(List<RaycastResult> resultList)
		{
			m_Result = resultList[0];
			for (int i = 1; i < resultList.Count; i++)
				m_Result = SelectRaycastResult(m_Result, resultList[i]);
			return m_Result;
		}

		private RaycastResult firstRaycastResult = new RaycastResult();
		private GraphicRaycaster[] graphic_raycasters;
		private List<RaycastResult> graphicRaycastResults = new List<RaycastResult>();
		private List<GameObject> graphicRaycastObjects = new List<GameObject>(), preGraphicRaycastObjects = new List<GameObject>();
		private GameObject raycastTarget = null;

		private void GraphicRaycast()
		{
			if (m_Camera == null)
				return;

			// Find GraphicRaycaster
			graphic_raycasters = FindObjectsOfType<GraphicRaycaster>();

			graphicRaycastResults.Clear();
			graphicRaycastObjects.Clear();

			for (int i = 0; i < graphic_raycasters.Length; i++)
			{
				// Ignore the Blocker of Dropdown.
				if (graphic_raycasters[i].gameObject.name.Equals("Blocker"))
					continue;

				// Change the Canvas' event camera.
				if (graphic_raycasters[i].gameObject.GetComponent<Canvas>() != null)
					graphic_raycasters[i].gameObject.GetComponent<Canvas>().worldCamera = m_Camera;
				else
					continue;

				// Raycasting.
				graphic_raycasters[i].Raycast(mPointerEventData, graphicRaycastResults);
				graphicRaycastResults = GetResultList(graphicRaycastResults);
				if (graphicRaycastResults.Count == 0)
					continue;

				// Get the results.
				firstRaycastResult = FindFirstResult(graphicRaycastResults);

				//DEBUG ("GraphicRaycast() device: " + event_controller.device + ", camera: " + firstRaycastResult.module.m_Camera + ", first result = " + firstRaycastResult);
				mPointerEventData.pointerCurrentRaycast = SelectRaycastResult(mPointerEventData.pointerCurrentRaycast, firstRaycastResult);
				graphicRaycastResults.Clear();
			} // for (int i = 0; i < graphic_raycasters.Length; i++)

			raycastTarget = mPointerEventData.pointerCurrentRaycast.gameObject;
			while (raycastTarget != null)
			{
				graphicRaycastObjects.Add(raycastTarget);
				raycastTarget = (raycastTarget.transform.parent != null ? raycastTarget.transform.parent.gameObject : null);
			}
		}

		private List<RaycastResult> physicsRaycastResults = new List<RaycastResult>();
		private List<GameObject> physicsRaycastObjects = new List<GameObject>(), prePhysicsRaycastObjects = new List<GameObject>();

		private void PhysicsRaycast()
		{
			if (m_Camera == null || m_PhysicsRaycaster == null)
				return;

			// Clear cache values.
			physicsRaycastResults.Clear();
			physicsRaycastObjects.Clear();

			// Raycasting.
			m_PhysicsRaycaster.Raycast(mPointerEventData, physicsRaycastResults);
			if (physicsRaycastResults.Count == 0)
				return;

			for (int i = 0; i < physicsRaycastResults.Count; i++)
			{
				// Ignore the GameObject with BonePose component.
				if (physicsRaycastResults[i].gameObject.GetComponent<BonePose>() != null)
					continue;

				physicsRaycastObjects.Add(physicsRaycastResults[i].gameObject);
			}

			firstRaycastResult = FindFirstRaycast(physicsRaycastResults);

			//DEBUG ("PhysicsRaycast() device: " + event_controller.device + ", camera: " + firstRaycastResult.module.m_Camera + ", first result = " + firstRaycastResult);
			mPointerEventData.pointerCurrentRaycast = SelectRaycastResult(mPointerEventData.pointerCurrentRaycast, firstRaycastResult);
		}
		#endregion

		#region Event Handling
		private void OnGraphicPointerEnterExit()
		{
			if (graphicRaycastObjects.Count != 0)
			{
				for (int i = 0; i < graphicRaycastObjects.Count; i++)
				{
					if (graphicRaycastObjects[i] != null && !preGraphicRaycastObjects.Contains(graphicRaycastObjects[i]))
					{
						ExecuteEvents.Execute(graphicRaycastObjects[i], mPointerEventData, ExecuteEvents.pointerEnterHandler);
						DEBUG("OnGraphicPointerEnterExit() enter: " + graphicRaycastObjects[i]);
					}
				}
			}

			if (preGraphicRaycastObjects.Count != 0)
			{
				for (int i = 0; i < preGraphicRaycastObjects.Count; i++)
				{
					if (preGraphicRaycastObjects[i] != null && !graphicRaycastObjects.Contains(preGraphicRaycastObjects[i]))
					{
						ExecuteEvents.Execute(preGraphicRaycastObjects[i], mPointerEventData, ExecuteEvents.pointerExitHandler);
						DEBUG("OnGraphicPointerEnterExit() exit: " + preGraphicRaycastObjects[i]);
					}
				}
			}

			CopyList(graphicRaycastObjects, preGraphicRaycastObjects);
		}

		private void OnPhysicsPointerEnterExit()
		{
			if (physicsRaycastObjects.Count != 0)
			{
				for (int i = 0; i < physicsRaycastObjects.Count; i++)
				{
					if (physicsRaycastObjects[i] != null && !prePhysicsRaycastObjects.Contains(physicsRaycastObjects[i]))
					{
						ExecuteEvents.Execute(physicsRaycastObjects[i], mPointerEventData, ExecuteEvents.pointerEnterHandler);
						DEBUG("OnPhysicsPointerEnterExit() enter: " + physicsRaycastObjects[i]);
					}
				}
			}

			if (prePhysicsRaycastObjects.Count != 0)
			{
				for (int i = 0; i < prePhysicsRaycastObjects.Count; i++)
				{
					if (prePhysicsRaycastObjects[i] != null && !physicsRaycastObjects.Contains(prePhysicsRaycastObjects[i]))
					{
						ExecuteEvents.Execute(prePhysicsRaycastObjects[i], mPointerEventData, ExecuteEvents.pointerExitHandler);
						DEBUG("OnPhysicsPointerEnterExit() exit: " + prePhysicsRaycastObjects[i]);
					}
				}
			}

			CopyList(physicsRaycastObjects, prePhysicsRaycastObjects);
		}

		private void OnPointerHover()
		{
			GameObject go = GetRaycastedObject();
			if (go != null && prevRaycastedObject == go)
				ExecuteEvents.ExecuteHierarchy(go, mPointerEventData, PointerEvents.pointerHoverHandler);
		}

		private void OnPointerDown()
		{
			GameObject go = GetRaycastedObject();
			if (go == null) return;

			// Send a Pointer Down event. If not received, get handler of Pointer Click.
			mPointerEventData.pressPosition = mPointerEventData.position;
			mPointerEventData.pointerPressRaycast = mPointerEventData.pointerCurrentRaycast;
			mPointerEventData.pointerPress =
				ExecuteEvents.ExecuteHierarchy(go, mPointerEventData, ExecuteEvents.pointerDownHandler)
				?? ExecuteEvents.GetEventHandler<IPointerClickHandler>(go);

			DEBUG("OnPointerDown() send Pointer Down to " + mPointerEventData.pointerPress + ", current GameObject is " + go);

			// If Drag Handler exists, send initializePotentialDrag event.
			mPointerEventData.pointerDrag = ExecuteEvents.GetEventHandler<IDragHandler>(go);
			if (mPointerEventData.pointerDrag != null)
			{
				DEBUG("OnPointerDown() send initializePotentialDrag to " + mPointerEventData.pointerDrag + ", current GameObject is " + go);
				ExecuteEvents.Execute(mPointerEventData.pointerDrag, mPointerEventData, ExecuteEvents.initializePotentialDrag);
			}

			// Press happened (even not handled) object.
			mPointerEventData.rawPointerPress = go;
			// Allow to send Pointer Click event
			mPointerEventData.eligibleForClick = true;
			// Reset the screen position of press, can be used to estimate move distance
			mPointerEventData.delta = Vector2.zero;
			// Current Down, reset drag state
			mPointerEventData.dragging = false;
			mPointerEventData.useDragThreshold = true;
			// Record the count of Pointer Click should be processed, clean when Click event is sent.
			mPointerEventData.clickCount = 1;
			// Set clickTime to current time of Pointer Down instead of Pointer Click
			// since Down & Up event should not be sent too closely. (< CLICK_TIME)
			mPointerEventData.clickTime = Time.unscaledTime;
		}

		private void OnPointerDrag()
		{
			if (Time.unscaledTime - mPointerEventData.clickTime < m_PinchTimeToDrag)
				return;
			if (mPointerEventData.pointerDrag == null)
				return;

			if (!mPointerEventData.dragging)
			{
				DEBUG("OnPointerDrag() send BeginDrag to " + mPointerEventData.pointerDrag);
				ExecuteEvents.Execute(mPointerEventData.pointerDrag, mPointerEventData, ExecuteEvents.beginDragHandler);
				mPointerEventData.dragging = true;
			}
			else
			{
				ExecuteEvents.Execute(mPointerEventData.pointerDrag, mPointerEventData, ExecuteEvents.dragHandler);
			}
		}

		private void OnPointerUp()
		{
			GameObject go = GetRaycastedObject();
			// The "go" may be different with mPointerEventData.pointerDrag so we don't check null.

			if (mPointerEventData.pointerPress != null)
			{
				// In the frame of button is pressed -> unpressed, send Pointer Up
				DEBUG("OnPointerUp() send Pointer Up to " + mPointerEventData.pointerPress);
				ExecuteEvents.Execute(mPointerEventData.pointerPress, mPointerEventData, ExecuteEvents.pointerUpHandler);
			}

			if (mPointerEventData.eligibleForClick)
			{
				GameObject click_object = ExecuteEvents.GetEventHandler<IPointerClickHandler>(go);
				if (click_object != null)
				{
					if (click_object == mPointerEventData.pointerPress)
					{
						// In the frame of button from being pressed to unpressed, send Pointer Click if Click is pending.
						DEBUG("OnPointerUp() send Pointer Click to " + mPointerEventData.pointerPress);
						ExecuteEvents.Execute(mPointerEventData.pointerPress, mPointerEventData, ExecuteEvents.pointerClickHandler);
					}
					else
					{
						DEBUG("OnTriggerUpMouse() pointer down object " + mPointerEventData.pointerPress + " is different with click object " + click_object);
					}
				}

				if (mPointerEventData.dragging)
				{
					GameObject drop_object = ExecuteEvents.GetEventHandler<IDropHandler>(go);
					if (drop_object == mPointerEventData.pointerDrag)
					{
						// In the frame of button from being pressed to unpressed, send Drop and EndDrag if dragging.
						DEBUG("OnPointerUp() send Pointer Drop to " + mPointerEventData.pointerDrag);
						ExecuteEvents.Execute(mPointerEventData.pointerDrag, mPointerEventData, ExecuteEvents.dropHandler);
					}

					DEBUG("OnPointerUp() send Pointer endDrag to " + mPointerEventData.pointerDrag);
					ExecuteEvents.Execute(mPointerEventData.pointerDrag, mPointerEventData, ExecuteEvents.endDragHandler);

					mPointerEventData.pointerDrag = null;
					mPointerEventData.dragging = false;
				}
			}

			// Down object.
			mPointerEventData.pointerPress = null;
			// Press happened (even not handled) object.
			mPointerEventData.rawPointerPress = null;
			// Clear pending state.
			mPointerEventData.eligibleForClick = false;
			// Click event is sent, clear count.
			mPointerEventData.clickCount = 0;
			// Up event is sent, clear the time limitation of Down event.
			mPointerEventData.clickTime = 0;
		}
		#endregion

		private void CopyList(List<GameObject> src, List<GameObject> dst)
		{
			dst.Clear();
			for (int i = 0; i < src.Count; i++)
				dst.Add(src[i]);
		}
	}
}
