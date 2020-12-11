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
	sealed class ControllerInputModule : PointerInputModule
	{
		const string LOG_TAG = "Wave.Essence.InputModule.ControllerInputModule";
		private void DEBUG(string msg)
		{
			if (Log.EnableDebugLog)
				Log.d(LOG_TAG, msg, true);
		}
		private void INFO(string msg) { Log.i(LOG_TAG, msg, true); }

		public enum BeamModes { Flexible, Fixed, Mouse }

#if UNITY_EDITOR
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
#endif

		#region Customized Settings
		private BeamModes m_BeamModeEx = BeamModes.Mouse;
		[Tooltip("There are 3 modes of different beam types.")]
		[SerializeField]
		private BeamModes m_BeamMode = BeamModes.Mouse;
		public BeamModes BeamMode { get { return m_BeamMode; } set { m_BeamMode = value; } }

		// If drag is prior, the click event will NOT be sent after dragging.
		private bool m_PriorDrag = false;
		public bool PriorDrag { get { return m_PriorDrag; } set { m_PriorDrag = value; } }

		[Tooltip("Select to enable events of Dominant controller.")]
		[SerializeField]
		private bool m_DominantEvent = true;
		public bool DominantEvent { get { return m_DominantEvent; } set { m_DominantEvent = value; } }
		private bool enableDominantEvent = true;

		[Tooltip("Set the PhysicsRaycaster eventMask of Dominant controller.")]
		[SerializeField]
		private LayerMask m_DominantRaycastMask = ~0;
		public LayerMask DominantRaycastMask { get { return m_DominantRaycastMask; } set { m_DominantRaycastMask = value; } }

		[Tooltip("Select to enable events of NonDominant controller.")]
		[SerializeField]
		private bool m_NonDominantEvent = true;
		public bool NonDominantEvent { get { return m_NonDominantEvent; } set { m_NonDominantEvent = value; } }
		private bool enableNonDominantEvent = true;

		[Tooltip("Set the PhysicsRaycaster eventMask of NonDominant controller.")]
		[SerializeField]
		private LayerMask m_NonDominantRaycastMask = ~0;
		public LayerMask NonDominantRaycastMask { get { return m_NonDominantRaycastMask; } set { m_NonDominantRaycastMask = value; } }

		[Tooltip("Choose the buttons to trigger events.")]
		[SerializeField]
		private List<XR_BinaryButton> m_ButtonToTrigger = new List<XR_BinaryButton>();
		public List<XR_BinaryButton> ButtonToTrigger { get { return m_ButtonToTrigger; } set { m_ButtonToTrigger = value; } }
		private List<bool> buttonState = new List<bool>(), preButtonState = new List<bool>();

		[Tooltip("Set the beam length in Fixed Beam Mode.")]
		[SerializeField]
		private float m_FixedBeamLength = 50;
		public float FixedBeamLength { get { return m_FixedBeamLength; } set { m_FixedBeamLength = value; } }
		#endregion

		private GameObject head = null;

		// Do NOT allow event DOWN being sent multiple times during kClickInterval
		// since UI element of Unity needs time to perform transitions.
		const float kClickInterval = 0.2f;

		// After selecting an object over this duration, the drag action will be taken.
		const float kTimeToDrag = 0.2f;

		// The beam end offset + this distance = the pointer distance.
		const float kBeamToPointerDistance = 0.5f;

		private GameObject nonDominantMouseModePointerCameraObject = null;
		private GameObject dominantMouseModePointerCameraObject = null;

		private bool toUpdateBeam = true;
		private bool toUpdatePointer = true;

		#region Beam Configuration
		[System.Serializable]
		class BeamConfig
		{
			public float StartWidth;
			public float EndWidth;
			public float StartOffset;
			public float EndOffset;
			public Color32 StartColor;
			public Color32 EndColor;

			public BeamConfig() { }
			public BeamConfig(BeamConfig src)
			{
				StartWidth = src.StartWidth;
				EndWidth = src.EndWidth;
				StartOffset = src.StartOffset;
				EndOffset = src.EndOffset;
				StartColor = src.StartColor;
				EndColor = src.EndColor;
			}
			public BeamConfig(float startWidth, float endWidth, float startOffset, float endOffset, Color32 startColor, Color32 endColor)
			{
				StartWidth = startWidth;
				EndWidth = endWidth;
				StartOffset = startOffset;
				EndOffset = endOffset;
				StartColor = startColor;
				EndColor = endColor;
			}

			public void copyFrom(BeamConfig src)
			{
				StartWidth = src.StartWidth;
				EndWidth = src.EndWidth;
				StartOffset = src.StartOffset;
				EndOffset = src.EndOffset;
				StartColor = src.StartColor;
				EndColor = src.EndColor;
			}
		}

		private static BeamConfig flexibleBeamConfig = new BeamConfig
		{
			StartWidth = 0.000625f,
			EndWidth = 0.00125f,
			StartOffset = 0.015f,
			EndOffset = 1.2f,
			StartColor = new Color32(255, 255, 255, 255),
			EndColor = new Color32(255, 255, 255, 0)
		};
		private static BeamConfig fixedBeamConfig = new BeamConfig
		{
			StartWidth = 0.000625f,
			EndWidth = 0.00125f,
			StartOffset = 0.015f,
			EndOffset = 50,
			StartColor = new Color32(255, 255, 255, 255),
			EndColor = new Color32(255, 255, 255, 255)
		};
		private static BeamConfig mouseBeamConfig = new BeamConfig
		{
			StartWidth = 0.000625f,
			EndWidth = 0.00125f,
			StartOffset = 0.015f,
			EndOffset = 1.2f,
			StartColor = new Color32(255, 255, 255, 255),
			EndColor = new Color32(255, 255, 255, 77)
		};

		class BeamModeSetting
		{
			public BeamModes Mode { get; set; }
			public BeamConfig Config { get; set; }

			public BeamModeSetting(BeamModes mode, BeamConfig config)
			{
				this.Mode = mode;
				this.Config = new BeamConfig(config);
			}
		}
		#endregion

		#region Event Controller Handling
		private List<EventController> m_EventControllers = new List<EventController>();
		class EventController
		{
			public XR_Hand device
			{
				get;
				set;
			}

			public GameObject model
			{
				get;
				set;
			}

			public GameObject prevRaycastedObject
			{
				get;
				set;
			}

			public PointerEventData eventData
			{
				get;
				set;
			}

			public ControllerPointer pointer
			{
				get;
				set;
			}

			public bool pointerEnabled
			{
				get;
				set;
			}

			public ControllerBeam beam
			{
				get;
				set;
			}

			public bool beamEnabled
			{
				get;
				set;
			}

			private List<BeamModeSetting> raycastModeSettings;
			public void SetBeamConfig(BeamModes mode, BeamConfig config)
			{
				for (int i = 0; i < raycastModeSettings.Count; i++)
				{
					if (raycastModeSettings[i].Mode == mode)
					{
						raycastModeSettings[i].Config.copyFrom(config);
					}
				}
			}
			public BeamConfig GetBeamConfig(BeamModes mode)
			{
				for (int i = 0; i < raycastModeSettings.Count; i++)
				{
					if (raycastModeSettings[i].Mode == mode)
						return raycastModeSettings[i].Config;
				}
				return null;
			}

			public EventController(XR_Hand type)
			{
				device = type;
				model = null;
				prevRaycastedObject = null;
				eventData = new PointerEventData(EventSystem.current);
				beam = null;
				beamEnabled = false;
				pointer = null;
				pointerEnabled = false;
				raycastModeSettings = new List<BeamModeSetting>();
				raycastModeSettings.Add(new BeamModeSetting(BeamModes.Flexible, flexibleBeamConfig));
				raycastModeSettings.Add(new BeamModeSetting(BeamModes.Fixed, fixedBeamConfig));
				raycastModeSettings.Add(new BeamModeSetting(BeamModes.Mouse, mouseBeamConfig));
			}
		}

		private EventController GetEventController(XR_Hand dt)
		{
			for (int i = 0; i < m_EventControllers.Count; i++)
			{
				if (m_EventControllers[i].device == dt)
					return m_EventControllers[i];
			}
			return null;
		}
		private void AddEventController(XR_Hand type)
		{
			m_EventControllers.Add(new EventController(type));
		}
		private void ResetEventController()
		{
			m_EventControllers.Clear();
		}
		#endregion

		#region Event Controller Components Update
		private void UpdateControllerModelInProcess()
		{
			for (int i = 0; i < EventControllerProvider.ControllerTypes.Length; i++)
			{
				XR_Hand dev_type = EventControllerProvider.ControllerTypes[i];
				EventController event_controller = GetEventController(dev_type);

				GameObject origin_model = event_controller.model;
				GameObject new_model = EventControllerProvider.Instance.GetEventController(dev_type);
				LayerMask layer_mask = ~0;
				if (dev_type == XR_Hand.Dominant)
					layer_mask = m_DominantRaycastMask;
				if (dev_type == XR_Hand.NonDominant)
					layer_mask = m_NonDominantRaycastMask;

				if (origin_model == null)
				{
					if (new_model != null)
					{
						// replace with new controller instance.
						DEBUG("UpdateControllerModelInProcess() " + dev_type + ", replace null with new controller instance.");
						SetupEventController(event_controller, new_model, layer_mask);
					}
				}
				else
				{
					if (new_model == null)
					{
						DEBUG("UpdateControllerModelInProcess() " + dev_type + ", clear controller instance.");
						SetupEventController(event_controller, null, layer_mask);
					}
					else
					{
						if (!ReferenceEquals(origin_model, new_model))
						{
							// replace with new controller instance.
							DEBUG("UpdateControllerModelInProcess() " + dev_type + ", set new controller instance.");
							SetupEventController(event_controller, new_model, layer_mask);
						}
					}
				}
			}
		}

		private void SetupEventController(EventController eventController, GameObject controller_model, LayerMask mask)
		{
			// Deactivate the old model.
			if (eventController.model != null)
			{
				DEBUG("SetupEventController() deactivate " + eventController.model.name);
				eventController.model.SetActive(false);
			}

			// Replace with a new model.
			eventController.model = controller_model;

			// Activate the new model.
			// Note: must setup beam first.
			if (eventController.model != null)
			{
				DEBUG("SetupEventController() activate " + eventController.model.name);
				eventController.model.SetActive(true);

				// Set up PhysicsRaycaster.
				PhysicsRaycaster phy_raycaster = eventController.model.GetComponent<PhysicsRaycaster>();
				if (phy_raycaster == null)
					phy_raycaster = eventController.model.AddComponent<PhysicsRaycaster>();
				phy_raycaster.eventMask = mask;
				DEBUG("SetupEventController() PhysicsRaycaster eventMask: " + phy_raycaster.eventMask.value);

				// Get the model beam.
				eventController.beam = eventController.model.GetComponentInChildren<ControllerBeam>(true);
				if (eventController.beam != null)
				{
					DEBUG("SetupEventController() set up ControllerBeam: " + eventController.beam.gameObject.name + " of " + eventController.device);
					SetupEventControllerBeam(eventController, Vector3.zero, true);
				}

				// Get the model pointer.
				eventController.pointer = eventController.model.GetComponentInChildren<ControllerPointer>(true);
				if (eventController.pointer != null)
				{
					DEBUG("SetupEventController() set up ControllerPointer: " + eventController.pointer.gameObject.name + " of " + eventController.device);
					SetupEventControllerPointer(eventController);
				}

				// Disable Camera to save rendering cost.
				Camera[] event_cameras = eventController.model.GetComponentsInChildren<Camera>();
				for (int i = 0; i < event_cameras.Length; i++)
				{
					event_cameras[i].stereoTargetEye = StereoTargetEyeMask.None;
					event_cameras[i].enabled = false;
				}
			}
		}

		private void SetupEventControllerBeam(EventController eventController, Vector3 intersectionPosition, bool updateRaycastConfig = false)
		{
			if (eventController == null || eventController.beam == null)
				return;

			BeamConfig beam_config = eventController.GetBeamConfig(m_BeamMode);
			if (updateRaycastConfig)
			{
				beam_config.StartWidth = eventController.beam.StartWidth;
				beam_config.EndWidth = eventController.beam.EndWidth;
				beam_config.StartOffset = eventController.beam.StartOffset;
				beam_config.StartColor = eventController.beam.StartColor;
				beam_config.EndColor = eventController.beam.EndColor;

				switch (m_BeamMode)
				{
					case BeamModes.Flexible:
					case BeamModes.Mouse:
						beam_config.EndOffset = eventController.beam.EndOffset;
						break;
					case BeamModes.Fixed:
						beam_config.EndOffset = m_FixedBeamLength;
						break;
					default:
						break;
				}
				eventController.SetBeamConfig(m_BeamMode, beam_config);

				DEBUG("SetupEventControllerBeam() " + eventController.device + ", " + m_BeamMode + " mode config - "
					+ "StartWidth: " + beam_config.StartWidth
					+ ", EndWidth: " + beam_config.EndWidth
					+ ", StartOffset: " + beam_config.StartOffset
					+ ", EndOffset: " + beam_config.EndOffset
					+ ", StartColor: " + beam_config.StartColor.ToString()
					+ ", EndColor: " + beam_config.EndColor.ToString()
				);
			}

			if (m_BeamMode != m_BeamModeEx || toUpdateBeam)
			{
				eventController.beam.StartWidth = beam_config.StartWidth;
				eventController.beam.EndWidth = beam_config.EndWidth;
				eventController.beam.StartOffset = beam_config.StartOffset;
				eventController.beam.EndOffset = beam_config.EndOffset;
				eventController.beam.StartColor = beam_config.StartColor;
				eventController.beam.EndColor = beam_config.EndColor;

				toUpdateBeam = false;

				DEBUG("SetupEventControllerBeam() " + eventController.device + ", " + m_BeamMode + " mode"
					+ ", StartWidth: " + eventController.beam.StartWidth
					+ ", EndWidth: " + eventController.beam.EndWidth
					+ ", StartOffset: " + eventController.beam.StartOffset
					+ ", length: " + eventController.beam.EndOffset
					+ ", StartColor: " + eventController.beam.StartColor.ToString()
					+ ", EndColor: " + eventController.beam.EndColor.ToString());
			}

			if (m_BeamMode == BeamModes.Flexible)
			{
				GameObject curr_raycasted_obj = GetRaycastedObject(eventController.device);
				if (curr_raycasted_obj != null)
					eventController.beam.OnPointerEnter(curr_raycasted_obj, intersectionPosition, true);
				else
				{
					if (curr_raycasted_obj != eventController.prevRaycastedObject)
						eventController.beam.OnPointerExit(eventController.prevRaycastedObject);
				}
			}
		}

		private void SetupEventControllerPointer(EventController eventController)
		{
			if (eventController.pointer == null)
				return;

			SetupEventControllerPointer(eventController, Vector3.zero);
		}

		private void SetupEventControllerPointer(EventController eventController, Vector3 intersectionPosition)
		{
			if (eventController == null || eventController.pointer == null)
				return;

			float pointerDistanceInMeters = 0;
			if (m_BeamMode != m_BeamModeEx || toUpdatePointer)
			{
				switch (m_BeamMode)
				{
					case BeamModes.Flexible:
					case BeamModes.Mouse:
						if (eventController.beam != null)
							pointerDistanceInMeters = eventController.beam.EndOffset + kBeamToPointerDistance;// eventController.beam.endOffsetMin;
						else
							pointerDistanceInMeters = mouseBeamConfig.EndOffset + kBeamToPointerDistance;

						eventController.pointer.PointerDistanceInMeters = pointerDistanceInMeters;
						eventController.pointer.ShowPointer = true;
						break;
					case BeamModes.Fixed:
						eventController.pointer.ShowPointer = false;
						break;
					default:
						break;
				}

				toUpdatePointer = false;

				DEBUG("SetupEventControllerPointer() " + eventController.device + ", " + m_BeamMode + " mode"
					+ ", pointerDistanceInMeters: " + pointerDistanceInMeters);
			}

			if (m_BeamMode != BeamModes.Fixed)
			{
				GameObject curr_raycasted_obj = GetRaycastedObject(eventController.device);
				if (curr_raycasted_obj != null)
					eventController.pointer.OnPointerEnter(curr_raycasted_obj, intersectionPosition, (m_BeamMode == BeamModes.Flexible));
				else
				{
					if (curr_raycasted_obj != eventController.prevRaycastedObject)
						eventController.pointer.OnPointerExit(eventController.prevRaycastedObject);
				}
			}
		}

		public void ChangeBeamLength(XR_Hand dt, float length)
		{
			EventController event_controller = GetEventController(dt);
			if (event_controller == null)
				return;

			if (m_BeamMode == BeamModes.Fixed || m_BeamMode == BeamModes.Mouse)
				event_controller.beam.EndOffset = length;

			toUpdateBeam = true;
			toUpdatePointer = true;
			SetupEventControllerBeam(event_controller, Vector3.zero, true);
			SetupEventControllerPointer(event_controller);
		}
		#endregion

		private void SetupPointerCamera(XR_Hand type)
		{
			if (head == null)
			{
				DEBUG("SetupPointerCamera() no head!!");
				return;
			}
			if (type == XR_Hand.Dominant && dominantMouseModePointerCameraObject == null)
			{
				// 1. Create a "DominantPointerCamera" GameObject.
				dominantMouseModePointerCameraObject = new GameObject("DominantPointerCamera");
				if (dominantMouseModePointerCameraObject == null)
					return;

				// 2. Set "DominantPointerCamera" as the head's child.
				dominantMouseModePointerCameraObject.transform.SetParent(head.transform, false);
				dominantMouseModePointerCameraObject.transform.localPosition = Vector3.zero;
				DEBUG("SetupPointerCamera() Dominant - set pointerCamera parent to " + dominantMouseModePointerCameraObject.transform.parent.name);

				// 3. Add component "ControllerPointerTracker".
				dominantMouseModePointerCameraObject.SetActive(false);
				ControllerPointerTracker pcTracker = dominantMouseModePointerCameraObject.AddComponent<ControllerPointerTracker>();
				pcTracker.TrackerType = XR_Hand.Dominant;
				dominantMouseModePointerCameraObject.SetActive(true);
				DEBUG("SetupPointerCamera() Dominant - add component ControllerPointerTracker");

				// 4. Set the physics raycaster's eventMask.
				PhysicsRaycaster phy_raycaster = dominantMouseModePointerCameraObject.GetComponent<PhysicsRaycaster>();
				if (phy_raycaster != null)
				{
					phy_raycaster.eventMask = m_DominantRaycastMask;
					DEBUG("SetupPointerCamera() Dominant - set physics raycast mask to " + phy_raycaster.eventMask.value);
				}

				// 5. Disable the "ControllerPointerTracker" camera.
				Camera pc = dominantMouseModePointerCameraObject.GetComponent<Camera>();
				if (pc != null)
				{
					pc.enabled = false;
					//pc.nearClipPlane = 0.01f;	// Prevent warnings in VR mode.
				}
			}

			if (type == XR_Hand.NonDominant && nonDominantMouseModePointerCameraObject == null)
			{
				// 1. Create a "NonDominantPointerCamera" GameObject.
				nonDominantMouseModePointerCameraObject = new GameObject("NonDominantPointerCamera");
				if (nonDominantMouseModePointerCameraObject == null)
					return;

				// 2. Set "NonDominantPointerCamera" as the head's child.
				nonDominantMouseModePointerCameraObject.transform.SetParent(head.transform, false);
				nonDominantMouseModePointerCameraObject.transform.localPosition = Vector3.zero;
				DEBUG("SetupPointerCamera() NonDominant - set pointerCamera parent to " + nonDominantMouseModePointerCameraObject.transform.parent.name);

				// 3. Add component "ControllerPointerTracker".
				nonDominantMouseModePointerCameraObject.SetActive(false);
				ControllerPointerTracker pcTracker = nonDominantMouseModePointerCameraObject.AddComponent<ControllerPointerTracker>();
				pcTracker.TrackerType = XR_Hand.NonDominant;
				nonDominantMouseModePointerCameraObject.SetActive(true);
				DEBUG("SetupPointerCamera() NonDominant - add component ControllerPointerTracker");

				// 4. Set the physics raycaster's eventMask.
				PhysicsRaycaster phy_raycaster = nonDominantMouseModePointerCameraObject.GetComponent<PhysicsRaycaster>();
				if (phy_raycaster != null)
				{
					phy_raycaster.eventMask = m_NonDominantRaycastMask;
					DEBUG("SetupPointerCamera() NonDominant - set physics raycast mask to " + phy_raycaster.eventMask.value);
				}

				// 5. Disable the "ControllerPointerTracker" camera.
				Camera pc = nonDominantMouseModePointerCameraObject.GetComponent<Camera>();
				if (pc != null)
				{
					pc.enabled = false;
					//pc.nearClipPlane = 0.01f;	// Prevent warnings in VR mode.
				}
			}
		}

		private void HandleRaycast()
		{
			for (int i = 0; i < EventControllerProvider.ControllerTypes.Length; i++)
			{
				XR_Hand dev_type = EventControllerProvider.ControllerTypes[i];
				// -------------------- Conditions for running loop begins -----------------
				// 1.Do nothing if no event controller.
				EventController event_controller = GetEventController(dev_type);
				if (event_controller == null)
					continue;

				GameObject controller_model = event_controller.model;
				if (controller_model == null)
					continue;

				// 2. Exit the objects "entered" previously if losing the system focus.
				if (!ApplicationScene.IsFocused)
				{
					ExitAllObjects(event_controller);
					continue;
				}

				// 3. Exit the objects "entered" previously if disabling events.
				if (dev_type == XR_Hand.Dominant && enableDominantEvent == false)
				{
					ExitObjects(event_controller, ref preGraphicRaycastObjectsDominant);
					ExitObjects(event_controller, ref prePhysicsRaycastObjectsDominant);
					continue;
				}
				if (dev_type == XR_Hand.NonDominant && enableNonDominantEvent == false)
				{
					ExitObjects(event_controller, ref preGraphicRaycastObjectsNoDomint);
					ExitObjects(event_controller, ref prePhysicsRaycastObjectsNoDomint);
					continue;
				}

				// 4. Exit the objects "entered" previously if the device pose is invalid.
				bool valid_pose = WXRDevice.IsTracked((XR_Device)dev_type);
				if (!valid_pose)
				{
					ExitAllObjects(event_controller);
					continue;
				}
				// -------------------- Conditions for running loop ends -----------------


				// -------------------- Set up the event camera begins -------------------
				event_controller.prevRaycastedObject = GetRaycastedObject(dev_type);

				Camera event_camera = null;

				if (m_BeamMode == BeamModes.Mouse)
				{
					if (dev_type == XR_Hand.Dominant)
						event_camera = (dominantMouseModePointerCameraObject != null ? dominantMouseModePointerCameraObject.GetComponent<Camera>() : null);

					if (dev_type == XR_Hand.NonDominant)
						event_camera = (nonDominantMouseModePointerCameraObject != null ? nonDominantMouseModePointerCameraObject.GetComponent<Camera>() : null);
				}
				else
				{
					event_camera = controller_model.GetComponentInChildren<Camera>();
				}
				if (event_camera == null)
					continue;

				ResetPointerEventDataHybrid(dev_type, event_camera);
				// -------------------- Set up the event camera ends ---------------------


				// -------------------- Raycast begins -------------------
				// 1. Get the nearest graphic raycast object.
				// Also, all raycasted graphic objects are stored in graphicRaycastObjects<device type>.
				if (dev_type == XR_Hand.Dominant)
					GraphicRaycast(event_controller, event_camera, ref graphicRaycastResultsDominant, ref graphicRaycastObjectsDominant);
				if (dev_type == XR_Hand.NonDominant)
					GraphicRaycast(event_controller, event_camera, ref graphicRaycastResultsNoDomint, ref graphicRaycastObjectsNoDomint);

				// 2. Get the physical raycast object.
				// If the physical object is nearer than the graphic object, pointerCurrentRaycast will be set to the physical object.
				// Also, all raycasted physical objects are stored in physicsRaycastObjects<device type>.
				PhysicsRaycaster phy_raycaster = null;
				if (m_BeamMode == BeamModes.Mouse)
				{
					phy_raycaster = event_camera.GetComponent<PhysicsRaycaster>();
				}
				else
				{
					phy_raycaster = controller_model.GetComponentInChildren<PhysicsRaycaster>();
				}
				if (phy_raycaster != null)
				{
					// Issue: GC.Alloc 40 bytes.
					if (dev_type == XR_Hand.Dominant)
						PhysicsRaycast(event_controller, phy_raycaster, ref physicsRaycastResultsDominant, ref physicsRaycastObjectsDominant);
					if (dev_type == XR_Hand.NonDominant)
						PhysicsRaycast(event_controller, phy_raycaster, ref physicsRaycastResultsNoDomint, ref physicsRaycastObjectsNoDomint);
				}
				// -------------------- Raycast ends -------------------

				// Get the pointerCurrentRaycast object.
				GameObject curr_raycasted_obj = GetRaycastedObject(dev_type);

				// -------------------- Send Events begins -------------------
				// 1. Exit previous object, enter new object.
				if (dev_type == XR_Hand.Dominant)
				{
					EnterExitObjects(event_controller, graphicRaycastObjectsDominant, ref preGraphicRaycastObjectsDominant);
					EnterExitObjects(event_controller, physicsRaycastObjectsDominant, ref prePhysicsRaycastObjectsDominant);
				}
				if (dev_type == XR_Hand.NonDominant)
				{
					EnterExitObjects(event_controller, graphicRaycastObjectsNoDomint, ref preGraphicRaycastObjectsNoDomint);
					EnterExitObjects(event_controller, physicsRaycastObjectsNoDomint, ref prePhysicsRaycastObjectsNoDomint);
				}


				// 2. Hover object.
				if (curr_raycasted_obj != null && curr_raycasted_obj == event_controller.prevRaycastedObject)
				{
					OnTriggerHover(dev_type, event_controller.eventData);
				}

				// 3. Get button states, some events are triggered by the button.
				CheckButtonState((XR_Device)dev_type);

				if (!btnPressDown && btnPressed)
				{
					// button hold means to drag.
					OnDrag(dev_type, event_controller.eventData);
				}
				else if (Time.unscaledTime - event_controller.eventData.clickTime < kClickInterval)
				{
					// Delay new events until kClickInterval has passed.
				}
				else if (btnPressDown && !event_controller.eventData.eligibleForClick)
				{
					// 1. button not pressed -> pressed.
					// 2. no pending Click should be procced.
					OnTriggerDown(dev_type, event_controller.eventData);
				}
				else if (!btnPressed)
				{
					// 1. If Down before, send Up event and clear Down state.
					// 2. If Dragging, send Drop & EndDrag event and clear Dragging state.
					// 3. If no Down or Dragging state, do NOTHING.
					OnTriggerUp(dev_type, event_controller.eventData);
				}
				// -------------------- Send Events ends -------------------

				PointerEventData event_data = event_controller.eventData;
				Vector3 intersec_pos = GetIntersectionPosition(event_data.enterEventCamera, event_data.pointerCurrentRaycast);

				RaycastResultProvider.Instance.SetRaycastResult(
					dev_type,
					event_controller.eventData.pointerCurrentRaycast.gameObject,
					intersec_pos
				);

				CheckBeamPointerActive(event_controller);
				SetupEventControllerBeam(event_controller, intersec_pos, false);
				SetupEventControllerPointer(event_controller, intersec_pos);
			} // for (int i = 0; i < EventControllerProvider.ControllerTypes.Length; i++)
		}

		#region Override BaseInputModule
		private bool mEnabled = false;
		protected override void OnEnable()
		{
			if (!mEnabled)
			{
				base.OnEnable();
				INFO("OnEnable()");

				// 0. Disable the existed StandaloneInputModule.
				Destroy(eventSystem.GetComponent<StandaloneInputModule>());

				// 1. Set up necessary components for Controller input.
				head = Camera.main.gameObject;
				if (head != null)
				{
					INFO("OnEnable() set up head to " + head.name);
					SetupPointerCamera(XR_Hand.Dominant);
					SetupPointerCamera(XR_Hand.NonDominant);
				}
				else
				{
					Log.w(LOG_TAG, "OnEnable() Please set the Main Camera.", true);
				}

				// 2. Initialize the event controller list.
				for (int i = 0; i < EventControllerProvider.ControllerTypes.Length; i++)
					AddEventController(EventControllerProvider.ControllerTypes[i]);

				// 3. Initialize the button states.
				buttonState.Clear();
				preButtonState.Clear();
				for (int i = 0; i < m_ButtonToTrigger.Count; i++)
				{
					buttonState.Add(false);
					preButtonState.Add(false);
				}

				// 4. Record the initial Controller raycast mode.
				m_BeamModeEx = m_BeamMode;

				// 5. Check the InputModuleSystem.
				if (InputModuleSystem.Instance != null)
					Log.i(LOG_TAG, "OnEnable() Loaded InputModuleSystem.");

				// 6. Record the event switch.
				enableDominantEvent = m_DominantEvent;
				enableNonDominantEvent = m_NonDominantEvent;

				// 7. Set the interaction mode to Controller.
				Interop.WVR_SetInteractionMode(WVR_InteractionMode.WVR_InteractionMode_Controller);

				mEnabled = true;
			}
		}

		protected override void OnDisable()
		{
			if (mEnabled)
			{
				base.OnDisable();
				DEBUG("OnDisable()");

				for (int i = 0; i < EventControllerProvider.ControllerTypes.Length; i++)
				{
					XR_Hand dev_type = EventControllerProvider.ControllerTypes[i];
					EventController _event_controller = GetEventController(dev_type);
					if (_event_controller != null)
					{
						ExitAllObjects(_event_controller);
					}
				}

				ResetEventController();

				mEnabled = false;
			}
		}

		public override void Process()
		{
			if (!mEnabled)
				return;

			UpdateControllerModelInProcess();

			// Handle the raycast before updating raycast mode.
			enableDominantEvent = m_DominantEvent;
			enableNonDominantEvent = m_NonDominantEvent;
			if (InputModuleSystem.Instance.SingleInput)
			{
				enableDominantEvent = (InputModuleSystem.Instance.PrimaryInput == XR_Hand.Dominant);
				enableNonDominantEvent = (InputModuleSystem.Instance.PrimaryInput == XR_Hand.NonDominant);
			}

			HandleRaycast();
			m_BeamModeEx = m_BeamMode;
		}
		#endregion

		#region Raycast
		private Vector2 eventDataPosition = Vector2.zero;
		private RaycastResult firstRaycastResult = new RaycastResult();
		private void ResetPointerEventDataHybrid(XR_Hand type, Camera eventCam)
		{
			EventController event_controller = GetEventController(type);
			if (event_controller != null)
			{
				if (event_controller.eventData == null)
					event_controller.eventData = new PointerEventData(EventSystem.current);

				if (m_BeamMode == BeamModes.Mouse && eventCam != null)
				{
					eventDataPosition.x = 0.5f * eventCam.pixelWidth;
					eventDataPosition.y = 0.5f * eventCam.pixelHeight;
				}
				else
				{
					eventDataPosition.x = 0.5f * Screen.width;
					eventDataPosition.y = 0.5f * Screen.height;
				}

				event_controller.eventData.Reset();
				event_controller.eventData.position = eventDataPosition;
				firstRaycastResult.Clear();
				event_controller.eventData.pointerCurrentRaycast = firstRaycastResult;
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

		List<RaycastResult> physicsRaycastResultsDominant = new List<RaycastResult>();
		List<RaycastResult> physicsRaycastResultsNoDomint = new List<RaycastResult>();
		List<RaycastResult> physicsResultList = new List<RaycastResult>();
		private void PhysicsRaycast(EventController event_controller, PhysicsRaycaster raycaster, ref List<RaycastResult> raycastResults, ref List<GameObject> raycastObjects)
		{
			raycastResults.Clear();
			raycastObjects.Clear();

			Profiler.BeginSample("PhysicsRaycaster.Raycast() dominant.");
			raycaster.Raycast(event_controller.eventData, raycastResults);
			Profiler.EndSample();

			GetResultList(raycastResults, physicsResultList);
			if (physicsResultList.Count == 0)
				return;

			firstRaycastResult = FindFirstResult(raycastResults);

			//if (firstRaycastResult.module != null)
				//DEBUG ("PhysicsRaycast() device: " + event_controller.device + ", camera: " + firstRaycastResult.module.eventCamera + ", first result = " + firstRaycastResult);
			event_controller.eventData.pointerCurrentRaycast = SelectRaycastResult(event_controller.eventData.pointerCurrentRaycast, firstRaycastResult);

			raycastTarget = event_controller.eventData.pointerCurrentRaycast.gameObject;
			while (raycastTarget != null)
			{
				raycastObjects.Add(raycastTarget);
				raycastTarget = (raycastTarget.transform.parent != null ? raycastTarget.transform.parent.gameObject : null);
			}
		}

		List<RaycastResult> graphicRaycastResultsDominant = new List<RaycastResult>();
		List<RaycastResult> graphicRaycastResultsNoDomint = new List<RaycastResult>();
		List<RaycastResult> graphicResultList = new List<RaycastResult>();
		private GameObject raycastTarget = null;
		private void GraphicRaycast(EventController event_controller, Camera event_camera, ref List<RaycastResult> raycastResults, ref List<GameObject> raycastObjects)
		{
			Profiler.BeginSample("Find GraphicRaycaster.");
			GraphicRaycaster[] graphic_raycasters = FindObjectsOfType<GraphicRaycaster>();
			Profiler.EndSample();

			raycastResults.Clear();
			raycastObjects.Clear();

			for (int i = 0; i < graphic_raycasters.Length; i++)
			{
				if (graphic_raycasters[i].gameObject != null && graphic_raycasters[i].gameObject.GetComponent<Canvas>() != null)
					graphic_raycasters[i].gameObject.GetComponent<Canvas>().worldCamera = event_camera;
				else
					continue;

				// 1. Get the dominant raycast results list.
				graphic_raycasters[i].Raycast(event_controller.eventData, raycastResults);
				GetResultList(raycastResults, graphicResultList);
				if (graphicResultList.Count == 0)
					continue;

				// 2. Get the dominant raycast objects list.
				firstRaycastResult = FindFirstResult(graphicResultList);

				//DEBUG ("GraphicRaycast() device: " + event_controller.device + ", camera: " + firstRaycastResult.module.eventCamera + ", first result = " + firstRaycastResult);
				event_controller.eventData.pointerCurrentRaycast = SelectRaycastResult(event_controller.eventData.pointerCurrentRaycast, firstRaycastResult);
				raycastResults.Clear();
			}

			raycastTarget = event_controller.eventData.pointerCurrentRaycast.gameObject;
			while (raycastTarget != null)
			{
				raycastObjects.Add(raycastTarget);
				raycastTarget = (raycastTarget.transform.parent != null ? raycastTarget.transform.parent.gameObject : null);
			}
		}
		#endregion

		#region EventSystem
		List<GameObject> graphicRaycastObjectsDominant = new List<GameObject>(), preGraphicRaycastObjectsDominant = new List<GameObject>();
		List<GameObject> graphicRaycastObjectsNoDomint = new List<GameObject>(), preGraphicRaycastObjectsNoDomint = new List<GameObject>();
		List<GameObject> physicsRaycastObjectsDominant = new List<GameObject>(), prePhysicsRaycastObjectsDominant = new List<GameObject>();
		List<GameObject> physicsRaycastObjectsNoDomint = new List<GameObject>(), prePhysicsRaycastObjectsNoDomint = new List<GameObject>();
		private void EnterExitObjects(EventController eventController, List<GameObject> enterObjects, ref List<GameObject> exitObjects)
		{
			if (exitObjects.Count > 0)
			{
				for (int i = 0; i < exitObjects.Count; i++)
				{
					if (exitObjects[i] != null && !enterObjects.Contains(exitObjects[i]))
					{
						ExecuteEvents.Execute(exitObjects[i], eventController.eventData, ExecuteEvents.pointerExitHandler);
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
						ExecuteEvents.Execute(enterObjects[i], eventController.eventData, ExecuteEvents.pointerEnterHandler);
						DEBUG("EnterExitObjects() enter: " + enterObjects[i]);
					}
				}
			}

			CopyList(enterObjects, exitObjects);
		}

		private void ExitObjects(EventController event_controller, ref List<GameObject> exitObjects)
		{
			if (exitObjects.Count > 0)
			{
				for (int i = 0; i < exitObjects.Count; i++)
				{
					if (exitObjects[i] != null)
					{
						ExecuteEvents.Execute(exitObjects[i], event_controller.eventData, ExecuteEvents.pointerExitHandler);
						DEBUG("ExitObjects() exit: " + exitObjects[i]);
					}
				}
			}

			exitObjects.Clear();
		}

		private void ExitAllObjects(EventController event_controller)
		{
			ExitObjects(event_controller, ref preGraphicRaycastObjectsDominant);
			ExitObjects(event_controller, ref preGraphicRaycastObjectsNoDomint);
			ExitObjects(event_controller, ref prePhysicsRaycastObjectsDominant);
			ExitObjects(event_controller, ref prePhysicsRaycastObjectsNoDomint);
		}

		private void OnTriggerDown(XR_Hand type, PointerEventData eventData)
		{
			GameObject curr_raycasted_obj = GetRaycastedObject(type);
			if (curr_raycasted_obj == null)
				return;

			// Send Pointer Down. If not received, get handler of Pointer Click.
			eventData.pressPosition = eventData.position;
			eventData.pointerPressRaycast = eventData.pointerCurrentRaycast;
			eventData.pointerPress =
				ExecuteEvents.ExecuteHierarchy(curr_raycasted_obj, eventData, ExecuteEvents.pointerDownHandler)
				?? ExecuteEvents.GetEventHandler<IPointerClickHandler>(curr_raycasted_obj);

			DEBUG("OnTriggerDown() device: " + type + " send Pointer Down to " + eventData.pointerPress + ", current GameObject is " + curr_raycasted_obj);

			// If Drag Handler exists, send initializePotentialDrag event.
			eventData.pointerDrag = ExecuteEvents.GetEventHandler<IDragHandler>(curr_raycasted_obj);
			if (eventData.pointerDrag != null)
			{
				DEBUG("OnTriggerDown() device: " + type + " send initializePotentialDrag to " + eventData.pointerDrag + ", current GameObject is " + curr_raycasted_obj);
				ExecuteEvents.Execute(eventData.pointerDrag, eventData, ExecuteEvents.initializePotentialDrag);
			}

			// press happened (even not handled) object.
			eventData.rawPointerPress = curr_raycasted_obj;
			// allow to send Pointer Click event
			eventData.eligibleForClick = true;
			// reset the screen position of press, can be used to estimate move distance
			eventData.delta = Vector2.zero;
			// current Down, reset drag state
			eventData.dragging = false;
			eventData.useDragThreshold = true;
			// record the count of Pointer Click should be processed, clean when Click event is sent.
			eventData.clickCount = 1;
			// set clickTime to current time of Pointer Down instead of Pointer Click.
			// since Down & Up event should not be sent too closely. (< kClickInterval)
			eventData.clickTime = Time.unscaledTime;
		}

		private void OnTriggerUp(XR_Hand type, PointerEventData eventData)
		{
			if (!eventData.eligibleForClick && !eventData.dragging)
			{
				// 1. no pending click
				// 2. no dragging
				// Mean user has finished all actions and do NOTHING in current frame.
				return;
			}

			GameObject curr_raycasted_obj = GetRaycastedObject(type);
			// curr_raycasted_obj may be different with eventData.pointerDrag so we don't check null

			if (eventData.pointerPress != null)
			{
				// In the frame of button is pressed -> unpressed, send Pointer Up
				DEBUG("OnTriggerUp() type: " + type + " send Pointer Up to " + eventData.pointerPress);
				ExecuteEvents.Execute(eventData.pointerPress, eventData, ExecuteEvents.pointerUpHandler);
			}

			if (eventData.eligibleForClick)
			{
				GameObject click_obj = ExecuteEvents.GetEventHandler<IPointerClickHandler>(curr_raycasted_obj);
				if (!m_PriorDrag)
				{
					if (click_obj != null)
					{
						if (click_obj == eventData.pointerPress)
						{
							// In the frame of button from being pressed to unpressed, send Pointer Click if Click is pending.
							DEBUG("OnTriggerUp() type: " + type + " send Pointer Click to " + eventData.pointerPress);
							ExecuteEvents.Execute(eventData.pointerPress, eventData, ExecuteEvents.pointerClickHandler);
						}
						else
						{
							DEBUG("OnTriggerUp() type: " + type
								+ " pointer down object " + eventData.pointerPress
								+ " is different with click object " + click_obj);
						}
					}
					else
					{
						if (eventData.dragging)
						{
							GameObject _pointerDrop = ExecuteEvents.GetEventHandler<IDropHandler>(curr_raycasted_obj);
							if (_pointerDrop == eventData.pointerDrag)
							{
								// In next frame of button from being pressed to unpressed, send Drop and EndDrag if dragging.
								DEBUG("OnTriggerUp() type: " + type + " send Pointer Drop to " + eventData.pointerDrag);
								ExecuteEvents.Execute(eventData.pointerDrag, eventData, ExecuteEvents.dropHandler);
							}
							DEBUG("OnTriggerUp() type: " + type + " send Pointer endDrag to " + eventData.pointerDrag);
							ExecuteEvents.Execute(eventData.pointerDrag, eventData, ExecuteEvents.endDragHandler);

							eventData.pointerDrag = null;
							eventData.dragging = false;
						}
					}
				}
				else
				{
					if (eventData.dragging)
					{
						GameObject _pointerDrop = ExecuteEvents.GetEventHandler<IDropHandler>(curr_raycasted_obj);
						if (_pointerDrop == eventData.pointerDrag)
						{
							// In next frame of button from being pressed to unpressed, send Drop and EndDrag if dragging.
							DEBUG("OnTriggerUp() type: " + type + " send Pointer Drop to " + eventData.pointerDrag);
							ExecuteEvents.Execute(eventData.pointerDrag, eventData, ExecuteEvents.dropHandler);
						}
						DEBUG("OnTriggerUp() type: " + type + " send Pointer endDrag to " + eventData.pointerDrag);
						ExecuteEvents.Execute(eventData.pointerDrag, eventData, ExecuteEvents.endDragHandler);

						eventData.pointerDrag = null;
						eventData.dragging = false;
					}
					else
					{
						if (click_obj != null)
						{
							if (click_obj == eventData.pointerPress)
							{
								// In the frame of button from being pressed to unpressed, send Pointer Click if Click is pending.
								DEBUG("OnTriggerUp() type: " + type + " send Pointer Click to " + eventData.pointerPress);
								ExecuteEvents.Execute(eventData.pointerPress, eventData, ExecuteEvents.pointerClickHandler);
							}
							else
							{
								DEBUG("OnTriggerUp() type: " + type
								+ " pointer down object " + eventData.pointerPress
								+ " is different with click object " + click_obj);
							}
						}
					}
				}
			}

			// Down of pending Click object.
			eventData.pointerPress = null;
			// press happened (even not handled) object.
			eventData.rawPointerPress = null;
			// clear pending state.
			eventData.eligibleForClick = false;
			// Click is processed, clearcount.
			eventData.clickCount = 0;
			// Up is processed thus clear the time limitation of Down event.
			eventData.clickTime = 0;
		}

		private void OnDrag(XR_Hand type, PointerEventData eventData)
		{
			if (Time.unscaledTime - eventData.clickTime < kTimeToDrag)
				return;
			if (eventData.pointerDrag == null)
				return;

			if (!eventData.dragging)
			{
				DEBUG("OnDrag() device: " + type + " send BeginDrag to " + eventData.pointerDrag);
				ExecuteEvents.Execute(eventData.pointerDrag, eventData, ExecuteEvents.beginDragHandler);
				eventData.dragging = true;
			}
			else
			{
				ExecuteEvents.Execute(eventData.pointerDrag, eventData, ExecuteEvents.dragHandler);
			}
		}

		private void OnTriggerHover(XR_Hand type, PointerEventData eventData)
		{
			GameObject curr_raycasted_obj = GetRaycastedObject(type);
			ExecuteEvents.ExecuteHierarchy(curr_raycasted_obj, eventData, PointerEvents.pointerHoverHandler);
		}

		private void OnTriggerEnterAndExit(XR_Hand type, PointerEventData eventData)
		{
			GameObject curr_raycasted_obj = GetRaycastedObject(type);

			if (eventData.pointerEnter != curr_raycasted_obj)
			{
				DEBUG("OnTriggerEnterAndExit() " + type + ", enter: " + curr_raycasted_obj + ", exit: " + eventData.pointerEnter);

				HandlePointerExitAndEnter(eventData, curr_raycasted_obj);

				DEBUG("OnTriggerEnterAndExit() " + type + ", pointerEnter: " + eventData.pointerEnter + ", camera: " + eventData.enterEventCamera);
			}
		}
		#endregion

		bool btnPressDown = false, btnPressed = false;
		private void CheckButtonState(XR_Device device)
		{
			btnPressDown = false;
			btnPressed = false;

#if UNITY_EDITOR
			if (Application.isEditor)
			{
				for (int b = 0; b < m_ButtonToTrigger.Count; b++)
				{
					btnPressDown |= DummyButton.GetStatus(device, WvrButton(m_ButtonToTrigger[b]), WVR_InputType.WVR_InputType_Button, DummyButton.ButtonState.Press);
					btnPressed |= DummyButton.GetStatus(device, WvrButton(m_ButtonToTrigger[b]), WVR_InputType.WVR_InputType_Button, DummyButton.ButtonState.Hold);
				}
			}
			else
#endif
			{
				for (int i = 0; i < m_ButtonToTrigger.Count; i++)
				{
					preButtonState[i] = buttonState[i];
					buttonState[i] = WXRDevice.KeyDown(device, m_ButtonToTrigger[i]);

					if (!preButtonState[i] && buttonState[i])
						btnPressDown = true;
					if (buttonState[i])
						btnPressed = true;
				}
			}
		}

		private void onButtonClick(EventController event_controller)
		{
			GameObject curr_raycasted_obj = GetRaycastedObject(event_controller.device);

			if (curr_raycasted_obj == null)
				return;

			Button btn = curr_raycasted_obj.GetComponent<Button>();
			if (btn != null)
			{
				DEBUG("onButtonClick() trigger Button.onClick to " + btn + " from " + event_controller.device);
				btn.onClick.Invoke();
			}
			else
			{
				DEBUG("onButtonClick() " + event_controller.device + ", " + curr_raycasted_obj.name + " does NOT contain Button!");
			}
		}

		/// <summary> Get the intersection position in world space </summary>
		private Vector3 GetIntersectionPosition(Camera cam, RaycastResult raycastResult)
		{
			if (cam == null)
				return Vector3.zero;

			float intersectionDistance = raycastResult.distance + cam.nearClipPlane;
			Vector3 intersectionPosition = cam.transform.forward * intersectionDistance + cam.transform.position;
			return intersectionPosition;
		}

		private GameObject GetRaycastedObject(XR_Hand type)
		{
			EventController event_controller = GetEventController(type);
			if (event_controller != null && event_controller.eventData != null)
				return event_controller.eventData.pointerCurrentRaycast.gameObject;
			return null;
		}

		private void CheckBeamPointerActive(EventController eventController)
		{
			if (eventController == null)
				return;

			if (eventController.beam != null)
			{
				bool enabled = eventController.beam.gameObject.activeSelf && eventController.beam.ShowBeam;
				if (eventController.beamEnabled != enabled)
				{
					eventController.beamEnabled = enabled;
					toUpdateBeam = eventController.beamEnabled;
					DEBUG("CheckBeamPointerActive() " + eventController.device + ", beam is " + (eventController.beamEnabled ? "active." : "inactive."));
				}
			}
			else
			{
				eventController.beamEnabled = false;
			}

			if (eventController.pointer != null)
			{
				bool enabled = eventController.pointer.gameObject.activeSelf && eventController.pointer.ShowPointer;
				if (eventController.pointerEnabled != enabled)
				{
					eventController.pointerEnabled = enabled;
					toUpdatePointer = eventController.pointerEnabled;
					DEBUG("CheckBeamPointerActive() " + eventController.device + ", pointer is " + (eventController.pointerEnabled ? "active." : "inactive."));
				}
			}
			else
			{
				eventController.pointerEnabled = false;
			}
		}

		private void CopyList(List<GameObject> src, List<GameObject> dst)
		{
			dst.Clear();
			for (int i = 0; i < src.Count; i++)
				dst.Add(src[i]);
		}
	}
}
