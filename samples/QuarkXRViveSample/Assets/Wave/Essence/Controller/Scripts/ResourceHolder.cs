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
using Wave.Native;
using System.Threading;
using System.IO;
using System.Runtime.InteropServices;
using System;

namespace Wave.Essence.Controller
{
	[System.Serializable]
	public class BatteryIndicator
	{
		public int level;
		public float min;
		public float max;
		public string texturePath;
		public bool textureLoaded;
		public Texture2D batteryTexture;
	}

	[System.Serializable]
	public class Model_TouchDesc
	{
		public Vector3 center;
		public Vector3 up;
		public Vector3 down;
		public Vector3 left;
		public Vector3 right;
		public float FloatingDistance;
	}

	[System.Serializable]
	public class TouchSetting
	{
		public Vector3 touchForward;
		public Vector3 touchCenter;
		public Vector3 touchRight;
		public Vector3 touchPtU;
		public Vector3 touchPtW;
		public Vector3 touchPtV;
		public float raidus;
		public float touchptHeight;
	}

	[System.Serializable]
	public class ModelResource
	{
		public string renderModelName;
		public XR_Hand hand;
		public bool mergeToOne;
		public uint sectionCount;

		public FBXInfo_t[] FBXInfo;
		public MeshInfo_t[] SectionInfo;
		public bool parserReady;

		public Texture2D modelTexture;

		public bool isTouchSetting;
		public TouchSetting TouchSetting;

		public bool isBatterySetting;
		public List<BatteryIndicator> batteryTextureList;
	}

	public class ResourceHolder
	{
		private static string LOG_TAG = "ResourceHolder";
		private Thread mthread;

		private static ResourceHolder instance = null;
		public static ResourceHolder Instance
		{
			get
			{
				if (instance == null)
				{
					Log.i(LOG_TAG, "create WaveVR_ControllerResourceHolder instance");

					instance = new ResourceHolder();
				}
				return instance;
			}
		}

		private void PrintDebugLog(string msg)
		{
			Log.d(LOG_TAG, msg);
		}

		private void PrintInfoLog(string msg)
		{
			Log.i(LOG_TAG, msg);
		}

		private void PrintWarningLog(string msg)
		{
			Log.w(LOG_TAG, msg);
		}

		public List<ModelResource> renderModelList = new List<ModelResource>();

		public bool isRenderModelExist(string renderModel, XR_Hand hand, bool merge)
		{
			foreach (ModelResource t in renderModelList)
			{
				if ((t.renderModelName == renderModel) && (t.mergeToOne == merge) && (t.hand == hand))
				{
					return true;
				}
			}

			return false;
		}

		public ModelResource getRenderModelResource(string renderModel, XR_Hand hand, bool merge)
		{
			foreach (ModelResource t in renderModelList)
			{
				if ((t.renderModelName == renderModel) && (t.mergeToOne == merge) && (t.hand == hand))
				{
					return t;
				}
			}

			return null;
		}

		public bool addRenderModel(string renderModel, string ModelFolder, XR_Hand hand, bool merge)
		{
			if (isRenderModelExist(renderModel, hand, merge))
				return false;

			string FBXFile = ModelFolder + "/";
			string imageFile = ModelFolder + "/";

			FBXFile += "controller00.fbx";
			imageFile += "controller00.png";

			if (!File.Exists(FBXFile))
				return false;

			if (!File.Exists(imageFile))
				return false;
			PrintDebugLog("---  start  ---, merge = " + merge);
			ModelResource newMR = new ModelResource();
			newMR.renderModelName = renderModel;
			newMR.mergeToOne = merge;
			newMR.parserReady = false;
			newMR.hand = hand;
			renderModelList.Add(newMR);

			mthread = new Thread(() => readNativeData(newMR, merge, ModelFolder));
			mthread.Start();

			PrintDebugLog("---  Read image file start  ---");
			byte[] imgByteArray = File.ReadAllBytes(imageFile);
			PrintDebugLog("---  Read image file end  ---");
			PrintDebugLog("---  Load image start  ---");
			Texture2D modelpng = new Texture2D(2, 2, TextureFormat.BGRA32, false);
			bool retLoad = modelpng.LoadImage(imgByteArray);
			if (retLoad)
			{
				PrintDebugLog("---  Load image end  ---, size: " + imgByteArray.Length);
			}
			else
			{
				PrintWarningLog("failed to load texture");
			}
			newMR.modelTexture = modelpng;
			PrintDebugLog("---  Parse battery image start  ---");
			newMR.isBatterySetting = getBatteryIndicatorParam(newMR, ModelFolder);
			PrintDebugLog("---  Parse battery image end  ---");
			PrintDebugLog("---  end  ---");

			return true;
		}

		void readNativeData(ModelResource curr, bool mergeTo, string modelFolderPath)
		{
			PrintDebugLog("---  thread start  ---");
			PrintInfoLog("Render model name: " + curr.renderModelName + ", merge = " + curr.mergeToOne);

			IntPtr ptrError = Marshal.AllocHGlobal(64);
			for (int j = 0; j < 64; j++)
			{
				Marshal.WriteByte(ptrError, j, 0);
			}
			string FBXFile = modelFolderPath + "/";
			FBXFile += "controller00.fbx";

			bool ret = false;
			uint sessionid = 0;
			uint sectionCount = 0;
			string errorCode = "";

			if (File.Exists(FBXFile))
			{
				ret = Interop.WVR_OpenMesh(FBXFile, ref sessionid, ptrError, mergeTo);
				errorCode = Marshal.PtrToStringAnsi(ptrError);

				if (!ret)
				{
					PrintWarningLog("FBX parse failed: " + errorCode);
					return;
				}
			}
			else
			{
				PrintWarningLog("FBX is not found");
				return;
			}

			PrintInfoLog("FBX parse succeed, sessionid = " + sessionid);
			bool finishLoading = Interop.WVR_GetSectionCount(sessionid, ref sectionCount);

			if (!finishLoading || sectionCount == 0)
			{
				PrintWarningLog("failed to load mesh");
				return;
			}

			curr.sectionCount = sectionCount;
			curr.FBXInfo = new FBXInfo_t[curr.sectionCount];
			curr.SectionInfo = new MeshInfo_t[curr.sectionCount];

			for (int i = 0; i < curr.sectionCount; i++)
			{
				curr.FBXInfo[i] = new FBXInfo_t();
				curr.SectionInfo[i] = new MeshInfo_t();

				curr.FBXInfo[i].meshName = Marshal.AllocHGlobal(256);

				for (int j=0; j<256; j++)
				{
					Marshal.WriteByte(curr.FBXInfo[i].meshName, j, 0);
				}
			}


			ret = Interop.WVR_GetMeshData(sessionid, curr.FBXInfo);
			if (!ret)
			{
				for (int i = 0; i < sectionCount; i++)
				{
					Marshal.FreeHGlobal(curr.FBXInfo[i].meshName);
				}

				curr.SectionInfo = null;
				curr.FBXInfo = null;
				Interop.WVR_ReleaseMesh(sessionid);
				return;
			}

			for (uint i = 0; i < curr.sectionCount; i++)
			{
				curr.SectionInfo[i]._vectice = new Vector3[curr.FBXInfo[i].verticeCount];
				for (int j = 0; j < curr.FBXInfo[i].verticeCount; j++)
				{
					curr.SectionInfo[i]._vectice[j] = new Vector3();
				}
				curr.SectionInfo[i]._normal = new Vector3[curr.FBXInfo[i].normalCount];
				for (int j = 0; j < curr.FBXInfo[i].verticeCount; j++)
				{
					curr.SectionInfo[i]._normal[j] = new Vector3();
				}
				curr.SectionInfo[i]._uv = new Vector2[curr.FBXInfo[i].uvCount];
				for (int j = 0; j < curr.FBXInfo[i].verticeCount; j++)
				{
					curr.SectionInfo[i]._uv[j] = new Vector2();
				}
				curr.SectionInfo[i]._indice = new int[curr.FBXInfo[i].indiceCount];
				for (int j = 0; j < curr.FBXInfo[i].verticeCount; j++)
				{
					curr.SectionInfo[i]._indice[j] = new int();
				}

				bool active = false;

				bool tret = Interop.WVR_GetSectionData(sessionid, i, curr.SectionInfo[i]._vectice, curr.SectionInfo[i]._normal, curr.SectionInfo[i]._uv, curr.SectionInfo[i]._indice, ref active);
				if (!tret) continue;

				curr.SectionInfo[i]._active = active;

				PrintInfoLog("i = " + i + ", active = " + curr.SectionInfo[i]._active);
				PrintInfoLog("i = " + i + ", relative transform = [" + curr.FBXInfo[i].matrix.m0 + " , " + curr.FBXInfo[i].matrix.m1 + " , " + curr.FBXInfo[i].matrix.m2 + " , " + curr.FBXInfo[i].matrix.m3 + "] ");
				PrintInfoLog("i = " + i + ", relative transform = [" + curr.FBXInfo[i].matrix.m4 + " , " + curr.FBXInfo[i].matrix.m5 + " , " + curr.FBXInfo[i].matrix.m6 + " , " + curr.FBXInfo[i].matrix.m7 + "] ");
				PrintInfoLog("i = " + i + ", relative transform = [" + curr.FBXInfo[i].matrix.m8 + " , " + curr.FBXInfo[i].matrix.m9 + " , " + curr.FBXInfo[i].matrix.m10 + " , " + curr.FBXInfo[i].matrix.m11 + "] ");
				PrintInfoLog("i = " + i + ", relative transform = [" + curr.FBXInfo[i].matrix.m12 + " , " + curr.FBXInfo[i].matrix.m13 + " , " + curr.FBXInfo[i].matrix.m14 + " , " + curr.FBXInfo[i].matrix.m15 + "] ");
				PrintInfoLog("i = " + i + ", vertice count = " + curr.FBXInfo[i].verticeCount + ", normal count = " + curr.FBXInfo[i].normalCount + ", uv count = " + curr.FBXInfo[i].uvCount + ", indice count = " + curr.FBXInfo[i].indiceCount);
			}
			Interop.WVR_ReleaseMesh(sessionid);
			curr.isTouchSetting = GetTouchPadParam(curr, modelFolderPath);
			curr.parserReady = true;
			PrintDebugLog("---  thread end  ---");
		}

		private bool GetTouchPadParam(ModelResource curr, string modelFolderPath)
		{
			if (curr == null)
			{
				PrintWarningLog("Model resource is null!");
				return false;
			}

			string TouchPadJsonPath = modelFolderPath + "/";

			TouchPadJsonPath += "Touchpad.json";

			if (!File.Exists(TouchPadJsonPath))
			{
				PrintWarningLog(TouchPadJsonPath + " is not found!");
				return false;
			}

			StreamReader json_sr = new StreamReader(TouchPadJsonPath);

			string JsonString = json_sr.ReadToEnd();
			PrintInfoLog("Touchpad json: " + JsonString);
			json_sr.Close();

			if (JsonString.Equals(""))
			{
				PrintWarningLog("JsonString is empty!");
				return false;
			}

			curr.TouchSetting = new TouchSetting();

			Model_TouchDesc td = JsonUtility.FromJson<Model_TouchDesc>(JsonString);

			if (td != null)
			{
				curr.TouchSetting.touchCenter = td.center;
				curr.TouchSetting.touchForward = td.up;
				curr.TouchSetting.touchRight = td.right;
				curr.TouchSetting.touchptHeight = td.FloatingDistance;

				PrintDebugLog("Touch Center pointer is found! x: " + curr.TouchSetting.touchCenter.x + " ,y: " + curr.TouchSetting.touchCenter.y + " ,z: " + curr.TouchSetting.touchCenter.z);
				PrintDebugLog("Touch Up pointer is found! x: " + curr.TouchSetting.touchForward.x + " ,y: " + curr.TouchSetting.touchForward.y + " ,z: " + curr.TouchSetting.touchForward.z);
				PrintDebugLog("Touch right pointer is found! x: " + curr.TouchSetting.touchRight.x + " ,y: " + curr.TouchSetting.touchRight.y + " ,z: " + curr.TouchSetting.touchRight.z);

				PrintInfoLog("Floating distance : " + curr.TouchSetting.touchptHeight);

				curr.TouchSetting.touchPtW = (curr.TouchSetting.touchForward - curr.TouchSetting.touchCenter).normalized; //analog +y direction.
				curr.TouchSetting.touchPtU = (curr.TouchSetting.touchRight - curr.TouchSetting.touchCenter).normalized; //analog +x direction.
				curr.TouchSetting.touchPtV = Vector3.Cross(curr.TouchSetting.touchPtU, curr.TouchSetting.touchPtW).normalized;
				curr.TouchSetting.raidus = (curr.TouchSetting.touchForward - curr.TouchSetting.touchCenter).magnitude;

				PrintInfoLog("touchPtW! x: " + curr.TouchSetting.touchPtW.x + " ,y: " + curr.TouchSetting.touchPtW.y + " ,z: " + curr.TouchSetting.touchPtW.z);
				PrintInfoLog("touchPtU! x: " + curr.TouchSetting.touchPtU.x + " ,y: " + curr.TouchSetting.touchPtU.y + " ,z: " + curr.TouchSetting.touchPtU.z);
				PrintInfoLog("touchPtV! x: " + curr.TouchSetting.touchPtV.x + " ,y: " + curr.TouchSetting.touchPtV.y + " ,z: " + curr.TouchSetting.touchPtV.z);
				PrintInfoLog("raidus: " + curr.TouchSetting.raidus);

				return true;
			}

			return false;
		}

		bool getBatteryIndicatorParam(ModelResource curr, string modelFolderPath)
		{
			if (curr == null)
			{
				PrintWarningLog("Model resource is null!");
				return false;
			}

			string batteryJsonFile = modelFolderPath + "/";

			batteryJsonFile += "BatteryIndicator.json";


			if (!File.Exists(batteryJsonFile))
			{
				PrintWarningLog(batteryJsonFile + " is not found!");
				return false;
			}

			StreamReader json_sr = new StreamReader(batteryJsonFile);

			string JsonString = json_sr.ReadToEnd();
			PrintInfoLog("BatteryIndicator json: " + JsonString);
			json_sr.Close();

			if (JsonString.Equals(""))
			{
				PrintWarningLog("JsonString is empty!");
				return false;
			}

			SimpleJSON.JSONNode jsNodes = SimpleJSON.JSONNode.Parse(JsonString);

			string tmpStr = "";
			tmpStr = jsNodes["LevelCount"].Value;

			if (tmpStr.Equals(""))
			{
				PrintWarningLog("Battery level is not found!");
				return false;
			}

			int batteryLevel = int.Parse(tmpStr);
			PrintInfoLog("Battery level is " + batteryLevel);

			if (batteryLevel <= 0)
			{
				PrintWarningLog("Battery level is less or equal to 0!");
				return false;
			}
			List<BatteryIndicator> batteryTextureList = new List<BatteryIndicator>();

			for (int i = 0; i < batteryLevel; i++)
			{
				string minStr = jsNodes["BatteryLevel"][i]["min"].Value;
				string maxStr = jsNodes["BatteryLevel"][i]["max"].Value;
				string pathStr = jsNodes["BatteryLevel"][i]["path"].Value;

				if (minStr.Equals("") || maxStr.Equals("") || pathStr.Equals(""))
				{
					PrintWarningLog("Min, Max or Path is not found!");
					batteryLevel = 0;
					batteryTextureList.Clear();
					return false;
				}

				string batteryLevelFile = modelFolderPath + "/" + pathStr;

				if (!File.Exists(batteryLevelFile))
				{
					PrintWarningLog(batteryLevelFile + " is not found!");
					batteryLevel = 0;
					batteryTextureList.Clear();
					return false;
				}

				BatteryIndicator tmpBI = new BatteryIndicator();
				tmpBI.level = i;
				tmpBI.min = float.Parse(minStr);
				tmpBI.max = float.Parse(maxStr);
				tmpBI.texturePath = batteryLevelFile;

				byte[] imgByteArray = File.ReadAllBytes(batteryLevelFile);
				PrintDebugLog("Image size: " + imgByteArray.Length);

				tmpBI.batteryTexture = new Texture2D(2, 2, TextureFormat.BGRA32, false);
				tmpBI.textureLoaded = tmpBI.batteryTexture.LoadImage(imgByteArray);

				PrintInfoLog("Battery Level: " + tmpBI.level + " min: " + tmpBI.min + " max: " + tmpBI.max + " path: " + tmpBI.texturePath + " loaded: " + tmpBI.textureLoaded);

				batteryTextureList.Add(tmpBI);
			}

			curr.batteryTextureList = batteryTextureList;
			PrintInfoLog("BatteryIndicator is ready!");
			return true;
		}
	}
}
