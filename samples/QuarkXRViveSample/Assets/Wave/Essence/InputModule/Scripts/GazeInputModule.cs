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
using UnityEngine.EventSystems;
using UnityEngine.Profiling;
using UnityEngine.UI;
using Wave.Native;
using System;
#if UNITY_EDITOR
using Wave.Essence.Editor;
#endif

namespace Wave.Essence.InputModule
{
	[DisallowMultipleComponent]
	[RequireComponent(typeof(EventSystem))]
	sealed class GazeInputModule : PointerInputModule
	{
		const string LOG_TAG = "Wave.Essence.InputModule.GazeInputModule";
		private void DEBUG(string msg)
		{
			if (Log.EnableDebugLog)
				Log.d(LOG_TAG, msg, true);
		}

		public enum GazeEvent
		{
			Down = 0,
			Click = 1,
			Submit = 2
		}

		private WVR_InputId WvrButton(XR_BinaryButton event_button)
		{
			switch (event_button)
			{
				case XR_BinaryButton.MenuPress:
					return WVR_InputId.WVR_InputId_Alias1_Menu;
				case XR_BinaryButton.GripPress:
					return WVR_InputId.WVR_InputId_Alias1_Grip;
				case XR_BinaryButton.TouchpadPress:
				case XR_BinaryButton.TouchpadTouch:
					return WVR_InputId.WVR_InputId_Alias1_Touchpad;
				case XR_BinaryButton.TriggerPress:
					return WVR_InputId.WVR_InputId_Alias1_Trigger;
				case XR_BinaryButton.ThumbstickPress:
				case XR_BinaryButton.ThumbstickTouch:
					return WVR_InputId.WVR_InputId_Alias1_Thumbstick;
				default:
					break;
			}

			return WVR_InputId.WVR_InputId_Alias1_Touchpad;
		}

		#region Customized Settings
		private bool m_EnableGazeEx = true;
		[SerializeField]
		private bool m_EnableGaze = true;
		public bool EnableGaze { get { return m_EnableGaze; } set { m_EnableGaze = value; } }

		[Tooltip("Set the event sent if gazed.")]
		[SerializeField]
		private GazeEvent m_InputEvent = GazeEvent.Click;
		public GazeEvent InputEvent { get { return m_InputEvent; } set { m_InputEvent = value; } }

		private bool m_TimerControlDefault = false;
		[Tooltip("A timer will be enabled to trigger gaze events if sets this value.")]
		[SerializeField]
		private bool m_TimerControl = true;
		public bool TimerControl {
			get { return m_TimerControl;
			}
			set {
				if (Log.gpl.Print)
					DEBUG("TimerControl() " + value);
				m_TimerControl = value;
				m_TimerControlDefault = value;
			}
		}

		[Tooltip("Set the timer countdown seconds.")]
		[SerializeField]
		private float m_TimeToGaze = 2.0f;
		public float TimeToGaze { get { return m_TimeToGaze; } set { m_TimeToGaze = value; } }

		[Tooltip("Set to trigger gaze events by buttons.")]
		[SerializeField]
		private bool m_ButtonControl = false;
		public bool ButtonControl { get { return m_ButtonControl; } set { m_ButtonControl = value; } }

		[Tooltip("Set the device type of buttons.")]
		[SerializeField]
		private List<XR_Device> m_ButtonControlDevices = new List<XR_Device>();
		public List<XR_Device> ButtonControlDevices { get { return m_ButtonControlDevices; } set { m_ButtonControlDevices = value; } }

		[Tooltip("Set the buttons to trigger gaze events.")]
		[SerializeField]
		private List<XR_BinaryButton> m_ButtonControlKeys = new List<XR_BinaryButton>();
		public List<XR_BinaryButton> ButtonControlKeys { get { return m_ButtonControlKeys; } set { m_ButtonControlKeys = value; } }

		private List<List<bool>> buttonState = new List<List<bool>>(), preButtonState = new List<List<bool>>();
		#endregion

		private GameObject head = null;
		private Camera m_Camera = null;
		private PhysicsRaycaster physicsRaycaster = null;

		private bool btnPressDown = false;
		private float currUnscaledTime = 0;

		private GazePointer gazePointer = null;

		#region PointerInputModule overrides. 
		private bool mEnabled = false;
		protected override void OnEnable()
		{
			if (!mEnabled)
			{
				base.OnEnable();

				// 0. Remove the existed StandaloneInputModule.
				Destroy(eventSystem.GetComponent<StandaloneInputModule>());

				// 1. Set up necessary components for Gaze input.
				head = Camera.main.gameObject;
				if (head != null)
				{
					m_Camera = head.GetComponent<Camera>();
					m_Camera.stereoTargetEye = StereoTargetEyeMask.None;
					DEBUG("OnEnable() Found event camera " + (m_Camera != null ? m_Camera.gameObject.name : "null"));
					physicsRaycaster = head.GetComponent<PhysicsRaycaster>();
					if (physicsRaycaster == null)
					{
						physicsRaycaster = head.AddComponent<PhysicsRaycaster>();
						DEBUG("OnEnable() Added PhysicsRaycaster " + (physicsRaycaster != null ? physicsRaycaster.gameObject.name : "null"));
					}
					gazePointer = head.GetComponentInChildren<GazePointer>();
					if (gazePointer == null)
					{
						GameObject gameObject = new GameObject("Gaze Pointer");
						gameObject.transform.SetParent(head.transform, false);
						gazePointer = gameObject.AddComponent<GazePointer>();
						DEBUG("OnEnable() Added gazePointer " + (gazePointer != null ? gazePointer.gameObject.name : "null"));
					}
				}

				// 2. Show the Gaze gazePointer.
				m_EnableGazeEx = m_EnableGaze;
				if (m_EnableGaze)
					ActivateMeshDrawer(true);

				// 3. Initialize the button states.
				buttonState.Clear();
				for (int d = 0; d < m_ButtonControlDevices.Count; d++)
				{
					List<bool> dev_list = new List<bool>();
					for (int k = 0; k < m_ButtonControlKeys.Count; k++)
					{
						dev_list.Add(false);
					}
					buttonState.Add(dev_list);
				}
				preButtonState.Clear();
				for (int d = 0; d < m_ButtonControlDevices.Count; d++)
				{
					List<bool> dev_list = new List<bool>();
					for (int k = 0; k < m_ButtonControlKeys.Count; k++)
					{
						dev_list.Add(false);
					}
					preButtonState.Add(dev_list);
				}

				// 4. Record the initial Gaze control method.
				m_TimerControlDefault = m_TimerControl;

				// 5. Check the InputModuleSystem.
				if (InputModuleSystem.Instance != null)
					Log.i(LOG_TAG, "OnEnable() Loaded InputModuleSystem.");

				// 6. Set the interaction mode to Controller.
				Interop.WVR_SetInteractionMode(WVR_InteractionMode.WVR_InteractionMode_Gaze);

				mEnabled = true;
			}
		}

		protected override void OnDisable()
		{
			if (mEnabled)
			{
				DEBUG("OnDisable()");
				base.OnDisable();

				ActivateMeshDrawer(false);
				gazePointer = null;

				ExitAllObjects();

				mEnabled = false;
			}
		}

		private bool ValidateParameters()
		{
			if (head == null || m_Camera == null)
				return false;

			if (!ApplicationScene.IsFocused)
			{
				ExitAllObjects();
				return false;
			}

			return true;
		}

		private GameObject prevRaycastedObject = null;
		public override void Process()
		{
			if (m_EnableGazeEx != m_EnableGaze)
			{
				m_EnableGazeEx = m_EnableGaze;
				ActivateMeshDrawer(m_EnableGaze);
			}

			if (m_EnableGaze)
			{
				if (!ValidateParameters())
				{
					gazeTime = Time.unscaledTime;
					return;
				}

				// 1. Timer control or button control.
				GazeControl();

				// 2. Graphic raycast and physics raycast.
				prevRaycastedObject = GetRaycastedObject();
				HandleRaycast();

				// 3. Update the timer & gazePointer state. Send the Gaze event.
				GazeHandling();
			}
		}
		#endregion

		private void ActivateMeshDrawer(bool active)
		{
			if (gazePointer == null)
				return;

			MeshRenderer mr = gazePointer.gameObject.GetComponentInChildren<MeshRenderer>();
			if (mr != null && mr.enabled != active)
			{
				DEBUG("ActivateMeshDrawer() " + (active ? "Enable " : "Disable ") + "the ring mesh.");
				mr.enabled = active;
			}
			else
			{
				if (Log.gpl.Print)
					Log.e(LOG_TAG, "ActivateMeshDrawer() Oooooooooooops! No MeshRenderer of " + gazePointer.gameObject.name);
			}
		}

		private void GazeControl()
		{
			m_TimerControl = m_TimerControlDefault;
			if (!WXRDevice.IsTracked(XR_Device.Dominant) && !WXRDevice.IsTracked(XR_Device.NonDominant))
				m_TimerControl = true;
		}


		#region Raycast
		private Vector3 gazeScreenPos2D = Vector2.zero;
		private void HandleRaycast()
		{
			// center of screen
			gazeScreenPos2D.x = 0.5f * Screen.width;
			gazeScreenPos2D.y = 0.5f * Screen.height;
			ResetPointerEventData();

			GraphicRaycast(ref graphicRaycastResults, ref graphicRaycastTargets);
			PhysicsRaycast(ref physicsRaycastResults, ref physicsRaycastTargets);

			EnterExitObjects(graphicRaycastTargets, ref preGraphicRaycastTargets);
			EnterExitObjects(physicsRaycastTargets, ref prePhysicsRaycastTargets);
		}

		private PointerEventData pointerData = null;
		private RaycastResult firstRaycastResult = new RaycastResult();
		private void ResetPointerEventData()
		{
			if (pointerData == null)
			{
				pointerData = new PointerEventData(eventSystem);
				pointerData.pointerCurrentRaycast = new RaycastResult();
			}

			pointerData.Reset();
			pointerData.position = gazeScreenPos2D;
			firstRaycastResult.Clear();
			pointerData.pointerCurrentRaycast = firstRaycastResult;
		}

		private GameObject GetRaycastedObject()
		{
			if (pointerData != null)
				return pointerData.pointerCurrentRaycast.gameObject;

			return null;
		}

		private void InvokeButtonClick(GameObject target)
		{
			GameObject click_obj = ExecuteEvents.GetEventHandler<IPointerClickHandler>(target);
			if (click_obj != null)
			{
				if (click_obj.GetComponent<Button>() != null)
				{
					DEBUG("InvokeButtonClick() on " + click_obj.name);
					click_obj.GetComponent<Button>().OnSubmit(pointerData);
				}
			}
		}

		private void SendPointerEvent(GameObject target)
		{
			// PointerClick is equivalent to Button Click.
			//InvokeButtonClick(target);

			if (m_InputEvent == GazeEvent.Click)
			{
				DEBUG("SendPointerEvent() Send a click event to " + target.name);
				ExecuteEvents.ExecuteHierarchy(target, pointerData, ExecuteEvents.pointerClickHandler);

				pointerData.clickTime = currUnscaledTime;
			}
			else if (m_InputEvent == GazeEvent.Down)
			{
				// like "mouse" action, press->release soon, do NOT keep the pointerPressRaycast cause do NOT need to controll "down" object while not gazing.
				pointerData.pressPosition = pointerData.position;
				pointerData.pointerPressRaycast = pointerData.pointerCurrentRaycast;

				DEBUG("SendPointerEvent() Send a down event to " + target.name);
				var down_object = ExecuteEvents.ExecuteHierarchy(target, pointerData, ExecuteEvents.pointerDownHandler);
				if (down_object != null)
				{
					DEBUG("SendPointerEvent() Send a up event to " + down_object.name);
					ExecuteEvents.ExecuteHierarchy(down_object, pointerData, ExecuteEvents.pointerUpHandler);
				}
			}
			else if (m_InputEvent == GazeEvent.Submit)
			{
				DEBUG("SendPointerEvent() Send a submit event to " + target.name);
				ExecuteEvents.ExecuteHierarchy(target, pointerData, ExecuteEvents.submitHandler);
			}
		}

		private void GetResultList(List<RaycastResult> originList, List<RaycastResult> targetList)
		{
			targetList.Clear();
			for (int i = 0; i < originList.Count; i++)
			{
				if (originList[i].gameObject != null)
					targetList.Add(originList[i]);
			}
		}

		private RaycastResult SelectRaycastResult(RaycastResult currResult, RaycastResult nextResult)
		{
			if (currResult.gameObject == null)
				return nextResult;
			if (nextResult.gameObject == null)
				return currResult;

			if (currResult.worldPosition == Vector3.zero)
				currResult.worldPosition = GetIntersectionPosition(currResult.module.eventCamera, currResult);

			float curr_distance = (float)Math.Round(Mathf.Abs(currResult.worldPosition.z - currResult.module.eventCamera.transform.position.z), 3);

			if (nextResult.worldPosition == Vector3.zero)
				nextResult.worldPosition = GetIntersectionPosition(nextResult.module.eventCamera, nextResult);

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

		List<RaycastResult> graphicRaycastResults = new List<RaycastResult>();
		List<GameObject> graphicRaycastTargets = new List<GameObject>(), preGraphicRaycastTargets = new List<GameObject>();
		List<RaycastResult> graphicResultList = new List<RaycastResult>();
		private GameObject raycastTarget = null;
		private void GraphicRaycast(ref List<RaycastResult> raycastResults, ref List<GameObject> raycastObjects)
		{
			Profiler.BeginSample("Find GraphicRaycaster for Gaze.");
			GraphicRaycaster[] graphic_raycasters = GameObject.FindObjectsOfType<GraphicRaycaster>();
			Profiler.EndSample();

			raycastResults.Clear();
			raycastObjects.Clear();

			for (int i = 0; i < graphic_raycasters.Length; i++)
			{
				if (graphic_raycasters[i].gameObject != null && graphic_raycasters[i].gameObject.GetComponent<Canvas>() != null)
					graphic_raycasters[i].gameObject.GetComponent<Canvas>().worldCamera = m_Camera;
				else
					continue;

				// 1. Get the raycast results list.
				graphic_raycasters[i].Raycast(pointerData, raycastResults);
				GetResultList(raycastResults, graphicResultList);
				if (graphicResultList.Count == 0)
					continue;

				// 2. Get the raycast objects list.
				firstRaycastResult = FindFirstResult(graphicResultList);

				//DEBUG ("GraphicRaycast() device: " + event_controller.device + ", camera: " + firstRaycastResult.module.m_Camera + ", first result = " + firstRaycastResult);
				pointerData.pointerCurrentRaycast = SelectRaycastResult(pointerData.pointerCurrentRaycast, firstRaycastResult);
				raycastResults.Clear();
			} // for (int i = 0; i < graphic_raycasters.Length; i++)

			raycastTarget = pointerData.pointerCurrentRaycast.gameObject;
			while (raycastTarget != null)
			{
				raycastObjects.Add(raycastTarget);
				raycastTarget = (raycastTarget.transform.parent != null ? raycastTarget.transform.parent.gameObject : null);
			}
		}

		private void EnterExitObjects(List<GameObject> enterObjects, ref List<GameObject> exitObjects)
		{
			if (exitObjects.Count > 0)
			{
				for (int i = 0; i < exitObjects.Count; i++)
				{
					if (exitObjects[i] != null && !enterObjects.Contains(exitObjects[i]))
					{
						ExecuteEvents.Execute(exitObjects[i], pointerData, ExecuteEvents.pointerExitHandler);
						DEBUG("EnterExitObjects() exit: " + exitObjects[i]);
					}
				}
			}

			if (enterObjects.Count > 0)
			{
				for (int i = 0; i < enterObjects.Count; i++)
				{
					if (enterObjects[i] != null && !exitObjects.Contains(enterObjects[i]))
					{
						ExecuteEvents.Execute(enterObjects[i], pointerData, ExecuteEvents.pointerEnterHandler);
						DEBUG("EnterExitObjects() enter: " + enterObjects[i]);
					}
				}
			}

			CopyList(enterObjects, exitObjects);
		}

		List<RaycastResult> physicsRaycastResults = new List<RaycastResult>();
		List<GameObject> physicsRaycastTargets = new List<GameObject>(), prePhysicsRaycastTargets = new List<GameObject>();
		List<RaycastResult> physicsResultList = new List<RaycastResult>();
		private void PhysicsRaycast(ref List<RaycastResult> raycastResults, ref List<GameObject> raycastObjects)
		{
			raycastResults.Clear();
			raycastObjects.Clear();

			Profiler.BeginSample("PhysicsRaycaster.Raycast() Gaze.");
			physicsRaycaster.Raycast(pointerData, raycastResults);
			Profiler.EndSample();

			GetResultList(raycastResults, physicsResultList);
			if (physicsResultList.Count == 0)
				return;

			firstRaycastResult = FindFirstResult(physicsResultList);

			//if (firstRaycastResult.module != null)
				//DEBUG ("PhysicsRaycast() camera: " + firstRaycastResult.module.m_Camera + ", first result = " + firstRaycastResult);
			pointerData.pointerCurrentRaycast = SelectRaycastResult(pointerData.pointerCurrentRaycast, firstRaycastResult);

			raycastTarget = pointerData.pointerCurrentRaycast.gameObject;
			while (raycastTarget != null)
			{
				raycastObjects.Add(raycastTarget);
				raycastTarget = (raycastTarget.transform.parent != null ? raycastTarget.transform.parent.gameObject : null);
			}
		}

		private void ExitAllObjects()
		{
			for (int i = 0; i < prePhysicsRaycastTargets.Count; i++)
			{
				if (prePhysicsRaycastTargets[i] != null)
				{
					ExecuteEvents.Execute(prePhysicsRaycastTargets[i], pointerData, ExecuteEvents.pointerExitHandler);
					DEBUG("ExitAllObjects() exit: " + prePhysicsRaycastTargets[i]);
				}
			}

			prePhysicsRaycastTargets.Clear();

			for (int i = 0; i < preGraphicRaycastTargets.Count; i++)
			{
				if (preGraphicRaycastTargets[i] != null)
				{
					ExecuteEvents.Execute(preGraphicRaycastTargets[i], pointerData, ExecuteEvents.pointerExitHandler);
					DEBUG("ExitAllObjects() exit: " + preGraphicRaycastTargets[i]);
				}
			}

			preGraphicRaycastTargets.Clear();
		}

		/**
		 * @brief get intersection position in world space
		 **/
		private Vector3 GetIntersectionPosition(Camera cam, RaycastResult raycastResult)
		{
			// Check for camera
			if (cam == null)
			{
				return Vector3.zero;
			}

			float intersectionDistance = raycastResult.distance + cam.nearClipPlane;
			Vector3 intersectionPosition = cam.transform.position + cam.transform.forward * intersectionDistance;
			return intersectionPosition;
		}
		#endregion


		private float gazeTime = 0.0f;
		private void GazeHandling()
		{
			// The gameobject to which raycast positions
			var curr_raycasted_obj = GetRaycastedObject();
			bool interactable = (curr_raycasted_obj != null);//pointerData.pointerPress != null || ExecuteEvents.GetEventHandler<IPointerClickHandler>(curr_raycasted_obj) != null;

			bool sendEvent = false;

			currUnscaledTime = Time.unscaledTime;
			if (prevRaycastedObject != curr_raycasted_obj)
			{
				DEBUG("prevRaycastedObject: "
					+ (prevRaycastedObject != null ? prevRaycastedObject.name : "null")
					+ ", curr_raycasted_obj: "
					+ (curr_raycasted_obj != null ? curr_raycasted_obj.name : "null"));
				if (curr_raycasted_obj != null)
					gazeTime = currUnscaledTime;
			}
			else
			{
				if (curr_raycasted_obj != null)
				{
					if (m_TimerControl)
					{
						if (currUnscaledTime - gazeTime > m_TimeToGaze)
						{
							sendEvent = true;
							gazeTime = currUnscaledTime;
						}
						float rate = ((currUnscaledTime - gazeTime) / m_TimeToGaze) * 100;
						if (gazePointer != null)
						{
							gazePointer.RingPercent = interactable ? (int)rate : 0;
						}
					}

					if (m_ButtonControl)
					{
						if (!m_TimerControl)
						{
							if (gazePointer != null)
								gazePointer.RingPercent = 0;
						}

						UpdateButtonStates();
						if (btnPressDown)
						{
							sendEvent = true;
							this.gazeTime = currUnscaledTime;
						}
					}
				}
				else
				{
					if (gazePointer != null)
						gazePointer.RingPercent = 0;
				}
			}

			// Standalone Input Module information
			pointerData.delta = Vector2.zero;
			pointerData.dragging = false;

			DeselectIfSelectionChanged(curr_raycasted_obj, pointerData);

			if (sendEvent)
			{
				SendPointerEvent(curr_raycasted_obj);
			}
		} // GazeHandling()

		private void UpdateButtonStates()
		{
			btnPressDown = false;

#if UNITY_EDITOR
			if (Application.isEditor)
			{
				for (int d = 0; d < m_ButtonControlDevices.Count; d++)
				{
					for (int k = 0; k < m_ButtonControlKeys.Count; k++)
					{
						btnPressDown |= DummyButton.GetStatus(
							m_ButtonControlDevices[d],
							WvrButton(m_ButtonControlKeys[k]),
							WVR_InputType.WVR_InputType_Button,
							DummyButton.ButtonState.Press
						);
					}
				}
			}
			else
#endif
			{
				for (int d = 0; d < m_ButtonControlDevices.Count; d++)
				{
					for (int k = 0; k < m_ButtonControlKeys.Count; k++)
					{
						preButtonState[d][k] = buttonState[d][k];
						buttonState[d][k] = WXRDevice.KeyDown(m_ButtonControlDevices[d], m_ButtonControlKeys[k]);

						if (!preButtonState[d][k] && buttonState[d][k])
						{
							btnPressDown = true;
							return;
						}
					}
				}
			}
		} // UpdateButtonStates()

		private void CopyList(List<GameObject> src, List<GameObject> dst)
		{
			dst.Clear();
			for (int i = 0; i < src.Count; i++)
				dst.Add(src[i]);
		}
	}
}
