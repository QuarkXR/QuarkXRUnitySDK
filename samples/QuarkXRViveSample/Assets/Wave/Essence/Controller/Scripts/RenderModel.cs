// "Wave SDK
// © 2020 HTC Corporation. All Rights Reserved.
//
// Unless otherwise required by copyright law and practice,
// upon the execution of HTC SDK license agreement,
// HTC grants you access to and use of the Wave SDK(s).
// You shall fully comply with all of HTC’s SDK license agreement terms and
// conditions signed by you and all SDK and API requirements,
// specifications, and documentation provided by HTC to You."

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Wave.Native;
using System;
using System.Runtime.InteropServices;
using UnityEngine.XR;
using Wave.Essence.Extra;

#if UNITY_EDITOR
using UnityEditor;
#endif
namespace Wave.Essence.Controller
{
#if UNITY_EDITOR
	public class ReadOnlyAttribute : PropertyAttribute
	{
	}

	[CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
	public class ReadOnlyDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty prop, GUIContent label)
		{
			string valueStr;

			switch (prop.propertyType)
			{
				case SerializedPropertyType.Integer:
					valueStr = prop.intValue.ToString();
					break;
				case SerializedPropertyType.Boolean:
					valueStr = prop.boolValue.ToString();
					break;
				case SerializedPropertyType.Float:
					valueStr = prop.floatValue.ToString("0.00000");
					break;
				case SerializedPropertyType.String:
					valueStr = prop.stringValue;
					break;
				default:
					valueStr = "(not supported)";
					break;
			}

			EditorGUI.LabelField(position, label.text, valueStr);
		}
	}
#endif
	public class RenderModel : MonoBehaviour
	{
		private static string LOG_TAG = "RenderModel";
		private void PrintDebugLog(string msg)
		{
			Log.d(LOG_TAG, "Hand: " + WhichHand + ", " + msg, true);
		}

		private void PrintInfoLog(string msg)
		{
			Log.i(LOG_TAG, "Hand: " + WhichHand + ", " + msg, true);
		}

		private void PrintWarningLog(string msg)
		{
			Log.w(LOG_TAG, "Hand: " + WhichHand + ", " + msg, true);
		}

		public enum LoadingState
		{
			LoadingState_NOT_LOADED,
			LoadingState_LOADING,
			LoadingState_LOADED
		}

		public XR_Hand WhichHand = XR_Hand.Dominant;
		public GameObject defaultModel = null;
		public bool updateDynamically = true;
		public bool mergeToOneBone = true;

		public delegate void RenderModelReadyDelegate(XR_Hand hand);
		public static event RenderModelReadyDelegate onRenderModelReady = null;

		[HideInInspector]
		public GameObject controllerSpawned = null;
		private XRNode node;

		private bool connected = false;
		private string renderModelNamePath = "";
		private string renderModelName = "";

		private List<Color32> colors = new List<Color32>();
		private GameObject meshCom = null;
		private GameObject meshGO = null;
		private Mesh updateMesh;
		private Material modelMat;
		private Material ImgMaterial;
		private WaitForEndOfFrame wfef = null;
		private WaitForSeconds wfs = null;
		private bool showBatterIndicator = true;
		private bool isBatteryIndicatorReady = false;
		private BatteryIndicator currentBattery;
		private GameObject batteryGO = null;
		private MeshRenderer batteryMR = null;

#if UNITY_EDITOR
		[ReadOnly, SerializeField]
#endif
		public bool loadFromAsset = true;

		private ModelResource modelResource = null;
		private LoadingState mLoadingState = LoadingState.LoadingState_NOT_LOADED;

		void OnEnable()
		{
			PrintDebugLog("OnEnable");
			if (mLoadingState == LoadingState.LoadingState_LOADING)
			{
				deleteChild("RenderModel doesn't expect model is in loading, delete all children");
			}

			if (WhichHand == XR_Hand.Dominant)
			{
				node = XRNode.RightHand;
			}
			else
			{
				node = XRNode.LeftHand;
			}

			connected = checkConnection();

			if (connected)
			{
				WVR_DeviceType type = checkDeviceType();

				if (mLoadingState == LoadingState.LoadingState_LOADED)
				{
					if (isRenderModelNameSameAsPrevious())
					{
						PrintDebugLog("OnEnable - Controller connected, model was loaded!");
					}
					else
					{
						deleteChild("Controller load when OnEnable, render model is different!");
						onLoadController(type);
					}
				}
				else
				{
					PrintDebugLog("Controller load when OnEnable!");
					onLoadController(type);
				}
			}

			OEMConfig.onOEMConfigChanged += onOEMConfigChanged;
		}

		void OnDisable()
		{
			PrintDebugLog("OnDisable");
			if (mLoadingState == LoadingState.LoadingState_LOADING)
			{
				deleteChild("RenderModel doesn't complete creating meshes before OnDisable, delete all children");
			}

			OEMConfig.onOEMConfigChanged -= onOEMConfigChanged;
		}

		private void onOEMConfigChanged()
		{
			PrintDebugLog("onOEMConfigChanged");
			ReadJsonValues();
		}

		private void ReadJsonValues()
		{
			showBatterIndicator = false;

			JSON_BatteryPolicy batteryP = OEMConfig.getBatteryPolicy();

			if (batteryP != null)
			{
				if (batteryP.show == 2)
					showBatterIndicator = true;
			} else
			{
				PrintDebugLog("There is no system policy!");
			}

			PrintDebugLog("showBatterIndicator: " + showBatterIndicator);
		}

		private bool isRenderModelNameSameAsPrevious()
		{
			bool _connected = checkConnection();
			bool _same = false;

			if (!_connected)
				return _same;

			WVR_DeviceType type = checkDeviceType();

			string tmprenderModelName = ClientInterface.GetCurrentRenderModelName(type);

			PrintDebugLog("previous render model: " + renderModelName + ", current render model name: " + tmprenderModelName);

			if (tmprenderModelName == renderModelName)
			{
				_same = true;
			}

			return _same;
		}

		// Use this for initialization
		void Start()
		{
			PrintDebugLog("start() connect: " + connected + " Which hand: " + WhichHand);
			wfs = new WaitForSeconds(1.0f);
			ReadJsonValues();

			if (updateDynamically)
			{
				PrintDebugLog("updateDynamically, start a coroutine to check connection and render model name periodly");
				StartCoroutine(checkRenderModelAndDelete());
			}
		}

		int t = 0;
		bool IsFocusCapturedBySystemLastFrame = false;

		// Update is called once per frame
		void Update()
		{
#if !UNITY_EDITOR
			if (Interop.WVR_IsInputFocusCapturedBySystem())
			{
				IsFocusCapturedBySystemLastFrame = true;
				return;
			}
#endif

			if (IsFocusCapturedBySystemLastFrame || (t-- < 0))
			{
				updateBatteryLevel();
				t = 200;
				IsFocusCapturedBySystemLastFrame = false;
			}

			if (mLoadingState == LoadingState.LoadingState_NOT_LOADED)
			{
				bool validPoseState;
				InputDevice device = InputDevices.GetDeviceAtXRNode(node);

				if (InputDevices.GetDeviceAtXRNode(node) != null && InputDevices.GetDeviceAtXRNode(node).TryGetFeatureValue(new InputFeatureUsage<bool>("IsTracked"), out validPoseState)
					&& validPoseState)
				{
					WVR_DeviceType type = checkDeviceType();

					PrintDebugLog("spawn render model");
					onLoadController(type);
				}
			}


			if (Log.gpl.Print)
				Log.d(LOG_TAG, "Update() render model " + WhichHand + " connect ? " + this.connected + ", child object count ? " + transform.childCount + ", showBatteryIndicator: " + showBatterIndicator + ", hasBattery: " + isBatteryIndicatorReady);
		}

		public void applyChange()
		{
			deleteChild("Setting is changed.");
			WVR_DeviceType type = checkDeviceType();
			onLoadController(type);
		}


		private void onLoadController(WVR_DeviceType type)
		{
			mLoadingState = LoadingState.LoadingState_LOADING;
			PrintDebugLog("Pos: " + this.transform.localPosition.x + " " + this.transform.localPosition.y + " " + this.transform.localPosition.z);
			PrintDebugLog("Rot: " + this.transform.localEulerAngles);
			PrintDebugLog("MergeToOneBone: " + mergeToOneBone);
			PrintDebugLog("type: " + type);

			if (Interop.WVR_GetWaveRuntimeVersion() < 2)
			{
				PrintDebugLog("onLoadController in old service");
				if (defaultModel != null)
				{
					controllerSpawned = Instantiate(defaultModel, this.transform);
					controllerSpawned.transform.parent = this.transform;
				}
				mLoadingState = LoadingState.LoadingState_LOADED;
				return;
			}

			renderModelName = ClientInterface.GetCurrentRenderModelName(type);

			if (renderModelName.Equals(""))
			{
				PrintDebugLog("Can not find render model.");
				if (defaultModel != null)
				{
					PrintDebugLog("Can't load controller model from DS, load default model");
					controllerSpawned = Instantiate(defaultModel, this.transform);
					controllerSpawned.transform.parent = this.transform;
					mLoadingState = LoadingState.LoadingState_LOADED;
				}
				return;
			}

			PrintDebugLog("render model name = " + renderModelName);

			if (loadFromAsset && renderModelName.StartsWith("WVR_CR_"))
			{
				string rname = (this.node == XRNode.RightHand ? "WVR_CR_Right" : "WVR_CR_Left");

				PrintDebugLog("Resource model = " + rname);

				var a = Resources.Load("Controller/" + rname) as GameObject;
				controllerSpawned = Instantiate(a, this.transform);
				controllerSpawned.transform.parent = this.transform;
				mLoadingState = LoadingState.LoadingState_LOADED;

				return;
			}

			controllerSpawned = this.gameObject;
			PrintDebugLog("controllerSpawned = " + controllerSpawned.name);

			int deviceIndex = -1;
			string parameterName = "backdoor_get_device_index";
			IntPtr ptrParameterName = Marshal.StringToHGlobalAnsi(parameterName);
			IntPtr ptrResultDeviceIndex = Marshal.AllocHGlobal(2);
			Interop.WVR_GetParameters(type, ptrParameterName, ptrResultDeviceIndex, 2);

			int _out = 0;
			bool _ret = int.TryParse(Marshal.PtrToStringAnsi(ptrResultDeviceIndex), out _out);
			if (_ret)
				deviceIndex = _out;
			Marshal.FreeHGlobal(ptrParameterName);
			Marshal.FreeHGlobal(ptrResultDeviceIndex);

			PrintInfoLog("get controller id from runtime is " + renderModelName + ", deviceIndex = " + deviceIndex);

			// 1. check if there are assets in private folder
			string renderModelFolderPath = Interop.WVR_DeployRenderModelAssets(deviceIndex, renderModelName);

			mLoadingState = (renderModelFolderPath != "") ? LoadingState.LoadingState_LOADING : LoadingState.LoadingState_NOT_LOADED;

			if (renderModelFolderPath != "")
			{
				bool retModel = false;
				modelResource = null;
				renderModelNamePath = renderModelFolderPath + "Model";

				retModel = ResourceHolder.Instance.addRenderModel(renderModelName, renderModelNamePath, WhichHand, mergeToOneBone);
				if (retModel)
				{
					PrintDebugLog("Add " + renderModelName + " with " + WhichHand + " model sucessfully!");
				}

				modelResource = ResourceHolder.Instance.getRenderModelResource(renderModelName, WhichHand, mergeToOneBone);

				if (modelResource != null)
				{
					mLoadingState = LoadingState.LoadingState_LOADING;

					PrintDebugLog("Starting load " + renderModelName + " with <" + modelResource.hand + "> model!");

					if (modelResource.hand == XR_Hand.Dominant)
					{
						PrintDebugLog(modelResource.hand + " loads Materials/WaveControllerMatR");
						ImgMaterial = Resources.Load("Materials/WaveControllerMatR") as Material;
					}
					else
					{
						PrintDebugLog(modelResource.hand + " loads Materials/WaveControllerMatL");
						ImgMaterial = Resources.Load("Materials/WaveControllerMatL") as Material;
					}

					wfef = new WaitForEndOfFrame();

					StartCoroutine(SpawnRenderModel());
				}
				else
				{
					PrintDebugLog("Model is null!");

					if (defaultModel != null)
					{
						PrintDebugLog("Can't load controller model from DS, load default model");
						controllerSpawned = Instantiate(defaultModel, this.transform);
						controllerSpawned.transform.parent = this.transform;
						mLoadingState = LoadingState.LoadingState_LOADED;
					}
				}
			}
		}

		string emitterMeshName = "__CM__Emitter";

		IEnumerator SpawnRenderModel()
		{
			while (true)
			{
				if (modelResource != null)
				{
					if (modelResource.parserReady) break;
				}
				PrintDebugLog("SpawnRenderModel is waiting");
				yield return wfef;
			}

			PrintDebugLog("Start to spawn all meshes!");

			if (modelResource == null)
			{
				PrintDebugLog("modelResource is null, skipping spawn objects");
				mLoadingState = LoadingState.LoadingState_NOT_LOADED;
				yield return null;
			}

			string meshName = "";
			for (uint i = 0; i < modelResource.sectionCount; i++)
			{
				meshName = Marshal.PtrToStringAnsi(modelResource.FBXInfo[i].meshName);
				meshCom = null;
				meshGO = null;

				bool meshAlready = false;

				for (uint j = 0; j < i; j++)
				{
					string tmp = Marshal.PtrToStringAnsi(modelResource.FBXInfo[j].meshName);

					if (tmp.Equals(meshName))
					{
						meshAlready = true;
					}
				}

				if (meshAlready)
				{
					PrintDebugLog(meshName + " is created! skip.");
					continue;
				}

				if (mergeToOneBone && modelResource.SectionInfo[i]._active)
				{
					meshName = "Merge_" + meshName;
				}
				updateMesh = new Mesh();
				meshCom = new GameObject();
				meshCom.AddComponent<MeshRenderer>();
				meshCom.AddComponent<MeshFilter>();
				meshGO = Instantiate(meshCom);
				meshGO.transform.parent = this.transform;
				meshGO.name = meshName;

				Matrix4x4 t = RigidTransform.toMatrix44(modelResource.FBXInfo[i].matrix);

				Vector3 x = Coordinate.GetVectorFromGL(t);
				meshGO.transform.localPosition = new Vector3(x.x, x.y, -x.z);

				meshGO.transform.localRotation = Coordinate.GetQuaternionFromGL(t);
				Vector3 r = meshGO.transform.localEulerAngles;
				meshGO.transform.localEulerAngles = new Vector3(-r.x, r.y, r.z);
				meshGO.transform.localScale = Coordinate.GetScale(t);

				PrintDebugLog("i = " + i + " MeshGO = " + meshName + ", localPosition: " + meshGO.transform.localPosition.x + ", " + meshGO.transform.localPosition.y + ", " + meshGO.transform.localPosition.z);
				PrintDebugLog("i = " + i + " MeshGO = " + meshName + ", localRotation: " + meshGO.transform.localEulerAngles);
				PrintDebugLog("i = " + i + " MeshGO = " + meshName + ", localScale: " + meshGO.transform.localScale);

				var meshfilter = meshGO.GetComponent<MeshFilter>();
				updateMesh.Clear();
				updateMesh.vertices = modelResource.SectionInfo[i]._vectice;
				updateMesh.uv = modelResource.SectionInfo[i]._uv;
				updateMesh.uv2 = modelResource.SectionInfo[i]._uv;
				updateMesh.colors32 = colors.ToArray();
				updateMesh.normals = modelResource.SectionInfo[i]._normal;
				updateMesh.SetIndices(modelResource.SectionInfo[i]._indice, MeshTopology.Triangles, 0);
				updateMesh.name = meshName;
				if (meshfilter != null)
				{
					meshfilter.mesh = updateMesh;
				}
				var meshRenderer = meshGO.GetComponent<MeshRenderer>();
				if (meshRenderer != null)
				{
					if (ImgMaterial == null)
					{
						PrintDebugLog("ImgMaterial is null");
					}
					meshRenderer.material = ImgMaterial;
					meshRenderer.material.mainTexture = modelResource.modelTexture;
					meshRenderer.enabled = true;
				}

				if (meshName.Equals(emitterMeshName))
				{
					PrintDebugLog(meshName + " is found, set " + meshName + " active: true");
					meshGO.SetActive(true);
				}
				else if (meshName.Equals("__CM__Battery"))
				{
					isBatteryIndicatorReady = false;
					if (modelResource.isBatterySetting)
					{
						if (modelResource.batteryTextureList != null)
						{
							batteryMR = meshGO.GetComponent<MeshRenderer>();
							Material mat = null;

							if (modelResource.hand == XR_Hand.Dominant)
							{
								PrintDebugLog(modelResource.hand + " loaded Materials/WaveBatteryMatR");
								mat = Resources.Load("Materials/WaveBatteryMatR") as Material;
							}
							else
							{
								PrintDebugLog(modelResource.hand + " loaded Materials/WaveBatteryMatL");
								mat = Resources.Load("Materials/WaveBatteryMatL") as Material;
							}

							if (mat != null)
							{
								batteryMR.material = mat;
							}

							batteryMR.material.mainTexture = modelResource.batteryTextureList[0].batteryTexture;
							batteryMR.enabled = true;
							isBatteryIndicatorReady = true;
						}
					}
					meshGO.SetActive(false);
					PrintDebugLog(meshName + " is found, set " + meshName + " active: false (waiting for update");
					batteryGO = meshGO;
				}
				else if (meshName == "__CM__TouchPad_Touch")
				{
					PrintDebugLog(meshName + " is found, set " + meshName + " active: false");
					meshGO.SetActive(false);
				}
				else
				{
					PrintDebugLog("set " + meshName + " active: " + modelResource.SectionInfo[i]._active);
					meshGO.SetActive(modelResource.SectionInfo[i]._active);
				}

				yield return wfef;
			}
			PrintDebugLog("send " + WhichHand + " RENDER_MODEL_READY ");

			onRenderModelReady?.Invoke(WhichHand);

			Resources.UnloadUnusedAssets();
			mLoadingState = LoadingState.LoadingState_LOADED;
		}

		void updateBatteryLevel()
		{
			if (batteryGO != null)
			{
				if (showBatterIndicator && isBatteryIndicatorReady)
				{
					if ((modelResource == null) || (modelResource.batteryTextureList == null))
						return;

					bool found = false;
					WVR_DeviceType type = checkDeviceType();
					float batteryP = Interop.WVR_GetDeviceBatteryPercentage(type);
					if (batteryP < 0)
					{
						PrintDebugLog("updateBatteryLevel BatteryPercentage is negative, return");
						batteryGO.SetActive(false);
						return;
					}
					foreach (BatteryIndicator bi in modelResource.batteryTextureList)
					{
						if (batteryP >= bi.min / 100 && batteryP <= bi.max / 100)
						{
							currentBattery = bi;
							found = true;
							break;
						}
					}
					if (found)
					{
						if (batteryMR != null)
						{
							batteryMR.material.mainTexture = currentBattery.batteryTexture;
							PrintDebugLog("updateBatteryLevel battery level to " + currentBattery.level + ", battery percent: " + batteryP);
							batteryGO.SetActive(true);
						}
						else
						{
							PrintDebugLog("updateBatteryLevel Can't get battery mesh renderer");
							batteryGO.SetActive(false);
						}
					}
					else
					{
						batteryGO.SetActive(false);
					}
				}
				else
				{
					batteryGO.SetActive(false);
				}
			}
		}

		IEnumerator checkRenderModelAndDelete()
		{
			while (true)
			{
				DeleteControllerWhenDisconnect();
				yield return wfs;
			}
		}

		private void deleteChild(string reason)
		{
			PrintInfoLog(reason);
			var ch = transform.childCount;

			for (int i = 0; i < ch; i++)
			{
				PrintInfoLog("deleteChild: " + transform.GetChild(i).gameObject.name);

				GameObject CM = transform.GetChild(i).gameObject;

				Destroy(CM);
			}
			mLoadingState = LoadingState.LoadingState_NOT_LOADED;
		}

		private void DeleteControllerWhenDisconnect()
		{
			if (mLoadingState != LoadingState.LoadingState_LOADED)
				return;

			bool _connected = checkConnection();

			if (_connected)
			{
				WVR_DeviceType type = checkDeviceType();

				string tmprenderModelName = ClientInterface.GetCurrentRenderModelName(type);

				if (tmprenderModelName != renderModelName)
				{
					deleteChild("Destroy controller prefeb because render model is different");
				}
			}
			else
			{
				deleteChild("Destroy controller prefeb because it is disconnect");
			}
			return;
		}

		private bool checkConnection()
		{
#if UNITY_EDITOR
			return true;
#endif
			bool validPoseState;
			if (!InputDevices.GetDeviceAtXRNode(node).TryGetFeatureValue(new InputFeatureUsage<bool>("IsTracked"), out validPoseState))
				return false;

			return validPoseState;
		}

		private WVR_DeviceType checkDeviceType()
		{
			WVR_DeviceType type = WVR_DeviceType.WVR_DeviceType_Invalid;
			//if (WaveEssence.Instance && WaveEssence.Instance.IsLeftHanded)
			//{
			//	if (WhichHand == XR_Hand.Dominant)
			//		type = WVR_DeviceType.WVR_DeviceType_Controller_Left;
			//	else
			//		type = WVR_DeviceType.WVR_DeviceType_Controller_Right;
			//}
			//else
			{
				if (WhichHand == XR_Hand.Dominant)
					type = WVR_DeviceType.WVR_DeviceType_Controller_Right;
				else
					type = WVR_DeviceType.WVR_DeviceType_Controller_Left;
			}
			return type;
		}
	}
}
