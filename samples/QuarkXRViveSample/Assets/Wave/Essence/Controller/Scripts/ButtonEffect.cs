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
using UnityEngine.XR;
using Wave.Essence.Extra;

namespace Wave.Essence.Controller
{
	[System.Serializable]
	public class MeshObject
	{
		public string MeshName;
		public bool hasEffect;
		public GameObject gameObject;
		public Vector3 originPosition;
		public Material originMat;
		public Material effectMat;
	}

	public class ButtonEffect : MonoBehaviour
	{
		private static string LOG_TAG = "ButtonEffect";
		public bool enableButtonEffect = true;
		public XR_Hand HandType = XR_Hand.Dominant;
		public bool useSystemConfig = true;
		public Color buttonEffectColor = new Color(0, 179, 227, 255);
		public bool collectInStart = true;

		private void PrintDebugLog(string msg)
		{
			Log.d(LOG_TAG, "Hand: " + HandType + ", " + msg);
		}

		private void PrintInfoLog(string msg)
		{
			Log.i(LOG_TAG, "Hand: " + HandType + ", " + msg);
		}

		public class WVR_InputObject
		{
			public WVR_InputId destination;
			public WVR_InputId sourceId;
		}

		private static readonly string[] clickNames = new string[] {
			//"PrimaryButton", // Home
			"MenuButton",
			"GripButton",
			//"DL",
			//"DU",
			//"DR",
			//"DD",
			//"VU",
			//"VD",
			"TriggerButton",
			"Primary2DAxisClick",
			"TriggerButton",
			"PrimaryButton", //A
			"SecondaryButton",
			"PrimaryButton", //X
			"SecondaryButton",
			//"VU",
			//"VD",
			"TriggerButton",
			"Secondary2DAxisClick",
		};

		private static readonly InputFeatureUsage<bool>[] pressFreature = new InputFeatureUsage<bool>[] {
			XR_Feature.MenuPress,
			XR_Feature.GripPress,
			XR_Feature.TriggerPress,
			XR_Feature.TouchpadPress,
			XR_Feature.TriggerPress,
			XR_Feature.A_X_Press,
			XR_Feature.B_Y_Press,
			XR_Feature.A_X_Press,
			XR_Feature.B_Y_Press,
			XR_Feature.TriggerPress,
			XR_Feature.ThumbstickPress
		};

		private static readonly string[] PressEffectNames = new string[] {
			//"__CM__HomeButton", // WVR_InputId_Alias1_System
			"__CM__AppButton", // WVR_InputId_Alias1_Menu
			"__CM__Grip", // WVR_InputId_Alias1_Grip
			//"__CM__DPad_Left", // DPad_Left
			//"__CM__DPad_Up", // DPad_Up
			//"__CM__DPad_Right", // DPad_Right
			//"__CM__DPad_Down", // DPad_Down
			//"__CM__VolumeUp", // VolumeUpKey
			//"__CM__VolumeDown", // VolumeDownKey
			"__CM__DigitalTriggerKey", // BumperKey in DS < 3.2
			"__CM__TouchPad", // TouchPad_Press
			"__CM__TriggerKey", // TriggerKey
			"__CM__ButtonA", // ButtonA
			"__CM__ButtonB", // ButtonB
			"__CM__ButtonX", // ButtonX
			"__CM__ButtonY", // ButtonY
			//"__CM__VolumeKey", // Volume
			//"__CM__VolumeKey", // Volume
			"__CM__BumperKey", // BumperKey in DS >= 3.2
			"__CM__Thumbstick", // Thumbstick
		};

		private MeshObject[] pressObjectArrays = new MeshObject[clickNames.Length];

		private static readonly string[] axis2DTouchNames = new string[] {
			"Primary2DAxisTouch",
			"thumbstick"
		};

		private static readonly string[] axis2DNames = new string[] {
			"Primary2DAxis",
			"thumbstick"
		};

		private static readonly string[] axis2DEffectNames = new string[] {
			"__CM__TouchPad_Touch", // TouchPad_Touch
			"__CM__TouchPad_Touch" // TouchPad_Touch
		};

		private MeshObject[] axis2DObjectArrays = new MeshObject[axis2DTouchNames.Length];

		private GameObject touchpad = null;
		private Mesh touchpadMesh = null;
		private Mesh toucheffectMesh = null;
		private bool currentIsLeftHandMode = false;
		private XRNode node;

		void onRenderModelReady(XR_Hand hand)
		{
			if (hand == this.HandType)
			{
				PrintInfoLog("onRenderModelReady(" + hand + ") and collect");
				CollectEffectObjects();
			}
		}

		void OnEnable()
		{
			if (HandType == XR_Hand.Dominant)
			{
				node = XRNode.RightHand;
			}
			else
			{
				node = XRNode.LeftHand;
			}
			resetButtonState();
			RenderModel.onRenderModelReady += onRenderModelReady;
		}

		void OnDisable()
		{
			RenderModel.onRenderModelReady -= onRenderModelReady;
		}

		void OnApplicationPause(bool pauseStatus)
		{
			if (!pauseStatus) // resume
			{
				PrintInfoLog("Pause(" + pauseStatus + ") and reset button state");
				resetButtonState();
			}
		}

		void resetButtonState()
		{
			PrintDebugLog("reset button state");
			if (!enableButtonEffect)
			{
				PrintInfoLog("enable button effect : false");
				return;
			}

			for (int i = 0; i < pressObjectArrays.Length; i++)
			{
				if (pressObjectArrays[i] == null) continue;
				if (pressObjectArrays[i].hasEffect)
				{
					if (pressObjectArrays[i].gameObject != null && pressObjectArrays[i].originMat != null && pressObjectArrays[i].effectMat != null)
					{
						pressObjectArrays[i].gameObject.GetComponent<MeshRenderer>().material = pressObjectArrays[i].originMat;
						if (mergeToOneBone) pressObjectArrays[i].gameObject.SetActive(false);
					}
				}
			}

			for (int i = 0; i < axis2DObjectArrays.Length; i++)
			{
				if (axis2DObjectArrays[i] == null) continue;
				if (axis2DObjectArrays[i].hasEffect)
				{
					if (axis2DObjectArrays[i].gameObject != null && axis2DObjectArrays[i].originMat != null && axis2DObjectArrays[i].effectMat != null)
					{
						axis2DObjectArrays[i].gameObject.GetComponent<MeshRenderer>().material = axis2DObjectArrays[i].originMat;
						axis2DObjectArrays[i].gameObject.SetActive(false);
					}
				}
			}
		}

		// Use this for initialization
		void Start()
		{
			resetButtonState();
			if (collectInStart) CollectEffectObjects();
		}

		// Update is called once per frame
		//int touch_index = -1;
		void Update()
		{
			if (!checkConnection())
				return;

			if (!enableButtonEffect)
				return;

			if (WaveEssence.Instance)
			{
				if (currentIsLeftHandMode != WaveEssence.Instance.IsLeftHanded)
				{
					currentIsLeftHandMode = WaveEssence.Instance.IsLeftHanded;
					PrintInfoLog("Controller role is changed to " + (currentIsLeftHandMode ? "Left" : "Right") + " and reset button state");
					resetButtonState();
				}
			}

			#region ButtonPress
			for (int i = 0; i < clickNames.Length; i++)
			{
				if (pressObjectArrays[i] == null) continue;

				if (InputDevices.GetDeviceAtXRNode(node) != null)
				{
					bool buttonState;
					if (InputDevices.GetDeviceAtXRNode(node).TryGetFeatureValue(new InputFeatureUsage<bool>(clickNames[i]), out buttonState)
					&& buttonState)
					{
						if (Log.gpl.Print)
							PrintInfoLog(clickNames[i] + " clicks");
						int _i = GetPressInputMapping(i);
						if (_i == -1) continue;
						if (pressObjectArrays[_i].hasEffect)
						{
							if (pressObjectArrays[_i].gameObject != null && pressObjectArrays[_i].originMat != null && pressObjectArrays[_i].effectMat != null)
							{
								pressObjectArrays[_i].gameObject.GetComponent<MeshRenderer>().material = pressObjectArrays[_i].effectMat;
								if (mergeToOneBone) pressObjectArrays[_i].gameObject.SetActive(true);
							}
						}
					}
					else
					{
						int _i = GetPressInputMapping(i);
						if (_i == -1) continue;
						if (pressObjectArrays[_i].hasEffect)
						{
							if (pressObjectArrays[_i].gameObject != null && pressObjectArrays[_i].originMat != null && pressObjectArrays[_i].effectMat != null)
							{
								pressObjectArrays[_i].gameObject.GetComponent<MeshRenderer>().material = pressObjectArrays[_i].originMat;
								if (mergeToOneBone) pressObjectArrays[_i].gameObject.SetActive(false);
							}
						}
					}
				}
			}
			#endregion
			#region Axis2D
			for (int i = 0; i < axis2DTouchNames.Length; i++)
			{
				if (axis2DObjectArrays[i] == null) continue;
				if (axis2DObjectArrays[i].gameObject == null) continue;
				if (axis2DObjectArrays[i].gameObject.GetComponent<MeshRenderer>() == null) continue;
				if (InputDevices.GetDeviceAtXRNode(node) != null)
				{
					bool buttonState;
					int _i = GetTouchInputMapping(i);
					if (InputDevices.GetDeviceAtXRNode(node).TryGetFeatureValue(new InputFeatureUsage<bool>(axis2DTouchNames[i]), out buttonState))
					{
						if (buttonState)
						{
							if (axis2DObjectArrays[_i].hasEffect && axis2DObjectArrays[_i].MeshName == "__CM__TouchPad_Touch")
							{
								if (axis2DObjectArrays[_i].gameObject != null && axis2DObjectArrays[_i].originMat != null && axis2DObjectArrays[_i].effectMat != null)
								{
									Vector2 axis;
									if (InputDevices.GetDeviceAtXRNode(node).TryGetFeatureValue(new InputFeatureUsage<Vector2>(axis2DNames[i]), out axis))
									{
										axis2DObjectArrays[_i].gameObject.GetComponent<MeshRenderer>().material = axis2DObjectArrays[_i].effectMat;
										axis2DObjectArrays[_i].gameObject.SetActive(true);

										if (isTouchPadSetting)
										{
											float xangle = touchCenter.x / 100 + (axis.x * raidus * touchPtU.x) / 100 + (axis.y * raidus * touchPtW.x) / 100 + (touchptHeight * touchPtV.x) / 100;
											float yangle = touchCenter.y / 100 + (axis.x * raidus * touchPtU.y) / 100 + (axis.y * raidus * touchPtW.y) / 100 + (touchptHeight * touchPtV.y) / 100;
											float zangle = touchCenter.z / 100 + (axis.x * raidus * touchPtU.z) / 100 + (axis.y * raidus * touchPtW.z) / 100 + (touchptHeight * touchPtV.z) / 100;

											// touchAxis
											if (Log.gpl.Print)
												Log.d(LOG_TAG, "Hand: " + HandType + ", " + "Axis2D axis x: " + axis.x + " axis.y: " + axis.y + ", xangle: " + xangle + ", yangle: " + yangle + ", zangle: " + zangle);

											Vector3 touchPos = transform.TransformPoint(xangle, yangle, zangle);

											axis2DObjectArrays[_i].gameObject.transform.position = touchPos;

										}
										else
										{
											float xangle = axis.x * (touchpadMesh.bounds.size.x * touchpad.transform.localScale.x - toucheffectMesh.bounds.size.x * axis2DObjectArrays[_i].gameObject.transform.localScale.x) / 2;
											float yangle = axis.y * (touchpadMesh.bounds.size.z * touchpad.transform.localScale.z - toucheffectMesh.bounds.size.z * axis2DObjectArrays[_i].gameObject.transform.localScale.z) / 2;

											var height = touchpadMesh.bounds.size.y * touchpad.transform.localScale.y;

											var h = Mathf.Abs(touchpadMesh.bounds.max.y);
											if (Log.gpl.Print)
											{

												Log.d(LOG_TAG, "Hand: " + HandType + ", " + "Axis2D axis x: " + axis.x + " axis.y: " + axis.y + ", xangle: " + xangle + ", yangle: " + yangle + ", height: " + height + ",h: " + h);

#if DEBUG
												Log.d(LOG_TAG, "Hand: " + HandType + ", " + "TouchEffectMesh.bounds.size: " + toucheffectMesh.bounds.size.x + ", " + toucheffectMesh.bounds.size.y + ", " + toucheffectMesh.bounds.size.z);
												Log.d(LOG_TAG, "Hand: " + HandType + ", " + "TouchEffectMesh.scale: " + axis2DObjectArrays[_i].gameObject.transform.localScale.x + ", " + axis2DObjectArrays[_i].gameObject.transform.localScale.y + ", " + axis2DObjectArrays[_i].gameObject.transform.localScale.z);
												Log.d(LOG_TAG, "Hand: " + HandType + ", " + "TouchpadMesh.bounds.size: " + touchpadMesh.bounds.size.x + ", " + touchpadMesh.bounds.size.y + ", " + touchpadMesh.bounds.size.z);
												Log.d(LOG_TAG, "Hand: " + HandType + ", " + "TouchpadMesh. scale: " + axis2DObjectArrays[_i].gameObject.transform.localScale.x + ", " + axis2DObjectArrays[_i].gameObject.transform.localScale.y + ", " + axis2DObjectArrays[_i].gameObject.transform.localScale.z);
												Log.d(LOG_TAG, "Hand: " + HandType + ", " + "TouchEffect.originPosition: " + axis2DObjectArrays[_i].originPosition.x + ", " + axis2DObjectArrays[_i].originPosition.y + ", " + axis2DObjectArrays[_i].originPosition.z);
#endif
											}
											Vector3 translateVec = Vector3.zero;
											translateVec = new Vector3(xangle, h, yangle);
											axis2DObjectArrays[_i].gameObject.transform.localPosition = axis2DObjectArrays[_i].originPosition + translateVec;
										}
									}
								}
							}
						}
						else
						{
							axis2DObjectArrays[_i].gameObject.GetComponent<MeshRenderer>().material = axis2DObjectArrays[_i].originMat;
							axis2DObjectArrays[_i].gameObject.SetActive(false);
						}
					}
				}
			}
			#endregion
		}

		private Material effectMat;
		private Material touchMat;
		private bool mergeToOneBone = false;
		private bool isTouchPadSetting = false;
		private Vector3 touchCenter = new Vector3(0, 0, 0);
		private float raidus;
		private Vector3 touchPtW; //W is direction of the +y analog.
		private Vector3 touchPtU; //U is direction of the +x analog.
		private Vector3 touchPtV; //V is normal of moving plane of touchpad.
		private float touchptHeight = 0.0f;

		private bool checkConnection()
		{
			bool validPoseState;
			if (InputDevices.GetDeviceAtXRNode(node) == null) return false;
			if (!InputDevices.GetDeviceAtXRNode(node).TryGetFeatureValue(new InputFeatureUsage<bool>("IsTracked"), out validPoseState))
				return false;

			return validPoseState;
		}

		private WVR_DeviceType checkDeviceType()
		{
			WVR_DeviceType type = WVR_DeviceType.WVR_DeviceType_Invalid;
			if (WaveEssence.Instance && WaveEssence.Instance.IsLeftHanded)
			{
				if (HandType == XR_Hand.Dominant)
					type = WVR_DeviceType.WVR_DeviceType_Controller_Left;
				else
					type = WVR_DeviceType.WVR_DeviceType_Controller_Right;
			}
			else
			{
				if (HandType == XR_Hand.Dominant)
					type = WVR_DeviceType.WVR_DeviceType_Controller_Right;
				else
					type = WVR_DeviceType.WVR_DeviceType_Controller_Left;
			}
			return type;
		}

		private bool GetTouchPadParam()
		{
			WVR_DeviceType type = checkDeviceType();
			bool _connected = checkConnection();
			if (!_connected)
			{
				PrintDebugLog("Device is disconnect: ");
				return false;
			}

			string renderModelName = ClientInterface.GetCurrentRenderModelName(type);

			if (renderModelName.Equals(""))
			{
				PrintDebugLog("Get render model name fail!");
				return false;
			}

			PrintDebugLog("current render model name: " + renderModelName);

			ModelResource modelResource = ResourceHolder.Instance.getRenderModelResource(renderModelName, HandType, mergeToOneBone);

			if ((modelResource == null) || (modelResource.TouchSetting == null))
			{
				PrintDebugLog("Get render model resource fail!");
				return false;
			}

			touchCenter = modelResource.TouchSetting.touchCenter;
			touchPtW = modelResource.TouchSetting.touchPtW;
			touchPtU = modelResource.TouchSetting.touchPtU;
			touchPtV = modelResource.TouchSetting.touchPtV;
			raidus = modelResource.TouchSetting.raidus;
			touchptHeight = modelResource.TouchSetting.touchptHeight;

			PrintDebugLog("touchCenter! x: " + touchCenter.x + " ,y: " + touchCenter.y + " ,z: " + touchCenter.z);
			PrintDebugLog("touchPtW! x: " + touchPtW.x + " ,y: " + touchPtW.y + " ,z: " + touchPtW.z);
			PrintDebugLog("touchPtU! x: " + touchPtU.x + " ,y: " + touchPtU.y + " ,z: " + touchPtU.z);
			PrintDebugLog("touchPtV! x: " + touchPtV.x + " ,y: " + touchPtV.y + " ,z: " + touchPtV.z);
			PrintDebugLog("raidus: " + raidus);
			PrintDebugLog("Floating distance : " + touchptHeight);

			return true;
		}

		private int GetPressInputMapping(int pressIds_Index)
		{
			//PrintInfoLog(status.ToString() + " disable key mapping " + pressIds_Index);
			return pressIds_Index;
			//WVR_InputId _btn = pressIds[pressIds_Index];
			//bool _result = WaveVR_ButtonList.Instance.GetInputMappingPair(this.device, ref _btn);

			//if (!_result)
			//{
			//	PrintInfoLog("GetInputMappingPair failed[" + pressIds[pressIds_Index] + "].");
			//	return -1;
			//}

			//int _index = -1;
			//for (int i = 0; i < pressIds.Length; i++)
			//{
			//	if (pressObjectArrays[i].hasEffect && _btn == pressIds[i])
			//	{
			//		_index = i;
			//		break;
			//	}
			//}

			//if (_index >= 0 && _index < pressIds.Length)
			//{
			//	PrintInfoLog(status.ToString() + " button: " + pressIds[pressIds_Index] + " is mapped to " + _btn);
			//}
			//else
			//{
			//	PrintInfoLog("Can't get index in pressIds.");
			//}

			//return _index;
		}

		private int GetTouchInputMapping(int touchIds_Index)
		{
			//WVR_InputId _btn = touchIds[touchIds_Index];
			//bool _result = WaveVR_ButtonList.Instance.GetInputMappingPair(this.device, ref _btn);
			//if (!_result)
			//{
			//	PrintInfoLog("GetInputMappingPair failed[" + touchIds[touchIds_Index] + "].");
			//	return -1;
			//}

			//int _index = -1;
			//for (int i = 0; i < touchIds.Length; i++)
			//{
			//	if (touchObjectArrays[i].hasEffect && _btn == touchIds[i])
			//	{
			//		_index = i;
			//		break;
			//	}
			//}

			//if (_index >= 0 && _index < touchIds.Length)
			//{
			//	PrintInfoLog(status.ToString() + " button: " + touchIds[touchIds_Index] + " is mapped to " + _btn);
			//}
			//else
			//{
			//	PrintInfoLog("Can't get index in touchIds.");
			//}

			//if (touchIds[touchIds_Index] == WVR_InputId.WVR_InputId_Alias1_Thumbstick // dst
			//	&& _btn == WVR_InputId.WVR_InputId_Alias1_Thumbstick) // src
			//{
			//	PrintInfoLog("Touch effect doesn't support Thumbstick now!");
			//	_index = -1;
			//}

			return touchIds_Index;
		}

		private void CollectEffectObjects() // collect controller object which has effect
		{
			if (HandType == XR_Hand.Dominant)
			{
				PrintDebugLog(HandType + " load Materials/WaveColorOffsetMatR");
				effectMat = Resources.Load("Materials/WaveColorOffsetMatR") as Material;
			} else
			{
				PrintDebugLog(HandType + " load Materials/WaveColorOffsetMatL");
				effectMat = Resources.Load("Materials/WaveColorOffsetMatL") as Material;
			}
			touchMat = new Material(Shader.Find("Unlit/Texture"));
			if (useSystemConfig)
			{
				PrintInfoLog("use system config in controller model!");
				ReadJsonValues();
			}
			else
			{
				Log.w(LOG_TAG, "use custom config in controller model!");
			}

			var ch = this.transform.childCount;
			PrintDebugLog("childCount: " + ch);
			effectMat.color = buttonEffectColor;

			RenderModel wrm = this.GetComponent<RenderModel>();

			if (wrm != null)
			{
				mergeToOneBone = wrm.mergeToOneBone;
			} else
			{
				mergeToOneBone = false;
			}

			isTouchPadSetting = GetTouchPadParam();

			for (var j = 0; j < PressEffectNames.Length; j++)
			{
				pressObjectArrays[j] = new MeshObject();
				pressObjectArrays[j].MeshName = PressEffectNames[j];
				pressObjectArrays[j].hasEffect = false;
				pressObjectArrays[j].gameObject = null;
				pressObjectArrays[j].originPosition = new Vector3(0, 0, 0);
				pressObjectArrays[j].originMat = null;
				pressObjectArrays[j].effectMat = null;

				for (int i = 0; i < ch; i++)
				{
					GameObject CM = this.transform.GetChild(i).gameObject;
					string[] t = CM.name.Split("."[0]);
					var childname = t[0];
					if (pressObjectArrays[j].MeshName == childname)
					{
						pressObjectArrays[j].gameObject = CM;
						pressObjectArrays[j].originPosition = CM.transform.localPosition;
						pressObjectArrays[j].originMat = CM.GetComponent<MeshRenderer>().material;
						pressObjectArrays[j].effectMat = effectMat;
						pressObjectArrays[j].hasEffect = true;

						if (childname == "__CM__TouchPad")
						{
							touchpad = pressObjectArrays[j].gameObject;
							touchpadMesh = touchpad.GetComponent<MeshFilter>().mesh;
							if (touchpadMesh != null)
							{
								PrintInfoLog("touchpad is found! ");
							}
						}
						break;
					}
				}

				PrintInfoLog("Press " + pressObjectArrays[j].MeshName + " has effect: " + pressObjectArrays[j].hasEffect);
			}

			for (var j = 0; j < axis2DEffectNames.Length; j++)
			{
				axis2DObjectArrays[j] = new MeshObject();
				axis2DObjectArrays[j].MeshName = axis2DEffectNames[j];
				axis2DObjectArrays[j].hasEffect = false;
				axis2DObjectArrays[j].gameObject = null;
				axis2DObjectArrays[j].originPosition = new Vector3(0f, 0f, 0f);
				axis2DObjectArrays[j].originMat = null;
				axis2DObjectArrays[j].effectMat = null;

				for (int i = 0; i < ch; i++)
				{
					GameObject CM = this.transform.GetChild(i).gameObject;
					string[] t = CM.name.Split("."[0]);
					var childname = t[0];

					if (axis2DObjectArrays[j].MeshName == childname)
					{
						axis2DObjectArrays[j].gameObject = CM;
						axis2DObjectArrays[j].originPosition = CM.transform.localPosition;
						axis2DObjectArrays[j].originMat = CM.GetComponent<MeshRenderer>().material;
						axis2DObjectArrays[j].effectMat = effectMat;
						axis2DObjectArrays[j].hasEffect = true;

						if (childname == "__CM__TouchPad_Touch")
						{
							toucheffectMesh = axis2DObjectArrays[j].gameObject.GetComponent<MeshFilter>().mesh;
							if (toucheffectMesh != null)
							{
								PrintInfoLog("toucheffectMesh is found! ");
							}
						}
						break;
					}
				}

				PrintInfoLog("Touch " + axis2DObjectArrays[j].MeshName + " has effect: " + axis2DObjectArrays[j].hasEffect);
			}

			resetButtonState();
		}

		#region OEMConfig
		private Color StringToColor(string color_string)
		{
			float _color_r = (float)Convert.ToInt32(color_string.Substring(1, 2), 16);
			float _color_g = (float)Convert.ToInt32(color_string.Substring(3, 2), 16);
			float _color_b = (float)Convert.ToInt32(color_string.Substring(5, 2), 16);
			float _color_a = (float)Convert.ToInt32(color_string.Substring(7, 2), 16);

			return new Color(_color_r, _color_g, _color_b, _color_a);
		}

		private Texture2D GetTexture2D(string texture_path)
		{
			if (System.IO.File.Exists(texture_path))
			{
				var _bytes = System.IO.File.ReadAllBytes(texture_path);
				var _texture = new Texture2D(1, 1);
				_texture.LoadImage(_bytes);
				return _texture;
			}
			return null;
		}

		public void Circle(Texture2D tex, int cx, int cy, int r, Color col)
		{
			int x, y, px, nx, py, ny, d;

			for (x = 0; x <= r; x++)
			{
				d = (int)Mathf.Ceil(Mathf.Sqrt(r * r - x * x));
				for (y = 0; y <= d; y++)
				{
					px = cx + x;
					nx = cx - x;
					py = cy + y;
					ny = cy - y;

					tex.SetPixel(px, py, col);
					tex.SetPixel(nx, py, col);

					tex.SetPixel(px, ny, col);
					tex.SetPixel(nx, ny, col);

				}
			}
			tex.Apply();
		}

		private void ReadJsonValues()
		{
			JSON_ModelDesc jmd = OEMConfig.getControllerModelDesc();

			if (jmd != null)
			{
				if (jmd.touchpad_dot_use_texture)
				{
					if (System.IO.File.Exists(jmd.touchpad_dot_texture_name))
					{
						var _texture = GetTexture2D(jmd.touchpad_dot_texture_name);

						PrintInfoLog("touchpad_dot_texture_name: " + jmd.touchpad_dot_texture_name);
						touchMat.mainTexture = _texture;
						touchMat.color = buttonEffectColor;
					}
				} else
				{
					buttonEffectColor = StringToColor(jmd.touchpad_dot_color);
					var texture = new Texture2D(256, 256, TextureFormat.ARGB32, false);
					Color o = Color.clear;
					o.r = 1f;
					o.g = 1f;
					o.b = 1f;
					o.a = 0f;
					for (int i = 0; i < 256; i++)
					{
						for (int j = 0; j < 256; j++)
						{
							texture.SetPixel(i, j, o);
						}
					}
					texture.Apply();

					Circle(texture, 128, 128, 100, buttonEffectColor);

					touchMat.mainTexture = texture;
				}
			}
		}
		#endregion
	}
}
