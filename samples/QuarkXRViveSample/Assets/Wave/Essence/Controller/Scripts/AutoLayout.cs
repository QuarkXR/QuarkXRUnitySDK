// "Wave SDK
// © 2020 HTC Corporation. All Rights Reserved.
//
// Unless otherwise required by copyright law and practice,
// upon the execution of HTC SDK license agreement,
// HTC grants you access to and use of the Wave SDK(s).
// You shall fully comply with all of HTC’s SDK license agreement terms and
// conditions signed by you and all SDK and API requirements,
// specifications, and documentation provided by HTC to You."

//#define DEBUG
using System.Collections.Generic;
using UnityEngine;
using Wave.Native;
using Wave.Essence.Extra;

namespace Wave.Essence.Controller
{
	[System.Serializable]
	public enum DisplayPlane
	{
		Button_Auto,
		Button,
		Body_Up,
		Body_Middle,
		Body_Bottom
	};

	[System.Serializable]
	public class AutoButtonIndication
	{
		public enum Alignment
		{
			Balance,
			Right,
			Left
		};

		public enum KeyIndicator
		{
			Trigger,
			TouchPad,
			DigitalTrigger,
			App,
			Home,
			Volume,
			VolumeUp,
			VolumeDown,
			Grip,
			DPad_Left,
			DPad_Right,
			DPad_Up,
			DPad_Down,
			Bumper,
			ButtonA,
			ButtonB,
			ButtonX,
			ButtonY
		};

		public KeyIndicator keyType;
		public Alignment alignment = Alignment.Balance;
		[Range(0.0f, 0.1f)]
		public float distanceBetweenButtonAndText = 0.035f; // Distance between SourceObject and DestObject
		[Range(0.0f, 0.1f)]
		public float distanceBetweenButtonAndLine = 0.0f; // Distance between SourceObject and Line
		[Range(-0.1f, 0.1f)]
		public float lineLengthAdjustment = 0.0f; // Distance between Line and DestObject
		public bool useMultiLanguage = false;
		public string indicationText = "system";
	}

	[System.Serializable]
	public class AutoComponentsIndication
	{
		public string name;
		public string indicationText = "system";
		public string indicationKey = null;
		public GameObject sourceObject;
		public GameObject lineIndicator;
		public GameObject destObject;
		public AutoButtonIndication.Alignment alignment = AutoButtonIndication.Alignment.Balance;
		public float distanceBetweenButtonAndText; // Distance between SourceObject and DestObject
		public float distanceBetweenButtonAndLine; // Distance between SourceObject and Line
		public float lineLengthAdjustment; // Distance between Line and DestObject
		public bool useMultiLanguage = false;

		public bool leftRightFlag = false;
		public float zValue;
	}

	public class AutoLayout : MonoBehaviour
	{
		private const string LOG_TAG = "AutoLayout";

		[Header("Indication feature")]
		public bool showIndicator = false;
		[Range(0, 90.0f)]
		public float showIndicatorAngle = 30.0f;
		public bool hideIndicatorByRoll = true;
		public bool basedOnEmitter = true;
		public DisplayPlane displayPlane = DisplayPlane.Button_Auto;

		[Header("Line customization")]
		[Range(0.0001f, 0.1f)]
		public float lineStartWidth = 0.0004f;
		[Range(0.0001f, 0.1f)]
		public float lineEndWidth = 0.0004f;
		public Color lineColor = Color.white;

		[Header("Text customization")]
		[Range(0.01f, 0.2f)]
		public float textCharacterSize = 0.08f;
		[Range(0.01f, 0.2f)]
		public float zhCharactarSize = 0.07f;
		[Range(50, 200)]
		public int textFontSize = 100;
		public Color textColor = Color.white;

		[Header("Indications")]
		public List<AutoButtonIndication> buttonIndicationList = new List<AutoButtonIndication>();

		[HideInInspector]
		public bool autoLayout = true;

		private ResourceWrapper rw = null;
		private string sysLang = null;
		private string sysCountry = null;
		private int checkCount = 0;
		private GameObject indicatorPrefab = null;
		private GameObject linePrefab = null;
		private List<AutoComponentsIndication> compInd = new List<AutoComponentsIndication>();
		private List<AutoComponentsIndication> rightList = new List<AutoComponentsIndication>();
		private List<AutoComponentsIndication> leftList = new List<AutoComponentsIndication>();
		private GameObject hmd = null;
		private bool needRedraw = true;
		private GameObject emitter = null;
		private Transform body = null;
		private int leftCount = 0;
		private int rightCount = 0;
		private float displayPlaneY = 0;

		public void CreateIndicator()
		{
			if (!showIndicator) return;
			ClearResourceAndObject();
			Log.d(LOG_TAG, "create Indicator!");
			rw = ResourceWrapper.instance;
			indicatorPrefab = Resources.Load("TextInd") as GameObject;

			if (indicatorPrefab == null)
			{
				Log.i(LOG_TAG, "TextInd is not found!");
				return;
			}
			else
			{
				Log.i(LOG_TAG, "TextInd is found!");
			}

			linePrefab = Resources.Load("LineInd") as GameObject;

			if (linePrefab == null)
			{
				Log.i(LOG_TAG, "LineInd is not found!");
				return;
			}
			else
			{
				Log.d(LOG_TAG, "LineInd is found!");
			}

			if (hmd == null)
				hmd = Camera.main.gameObject;

			if (hmd == null)
			{
				Log.i(LOG_TAG, "Can't get HMD!");
				return;
			}

			var gc = transform.childCount;

			for (int i = 0; i < gc; i++)
			{
				GameObject go = transform.GetChild(i).gameObject;

				Log.i(LOG_TAG, "child name is " + go.name);
			}

			Log.i(LOG_TAG, "showIndicatorAngle: " + showIndicatorAngle + ", hideIndicatorByRoll: " + hideIndicatorByRoll + ", basedOnEmitter: " + basedOnEmitter + ", displayPlane: " + displayPlane);
			Log.i(LOG_TAG, "Line settings--\n lineStartWidth: " + lineStartWidth + ", lineEndWidth: " + lineEndWidth + ", lineColor: " + lineColor);
			Log.i(LOG_TAG, "Text settings--\n textCharacterSize: " + textCharacterSize + ", zhCharactarSize: " + zhCharactarSize + ", textFontSize: " + textFontSize + ", textColor: " + textColor);

			body = transform.Find("_[CM]_Body");
			if (body == null)
			{
				body = transform.Find("__CM__Body");
				if (body == null)
				{
					body = transform.Find("__CM__Body.__CM__Body");
					if (body == null)
					{
						body = transform.Find("Body");
					}
				}
			}

			if (body == null)
			{
				Log.w(LOG_TAG, "Body of the controller can't be found in the model!");
			}

			foreach (AutoButtonIndication bi in buttonIndicationList)
			{
				Log.i(LOG_TAG, "keyType: " + bi.keyType + ", alignment: " + bi.alignment + ", distanceBetweenButtonAndText: " + bi.distanceBetweenButtonAndText + ", distanceBetweenButtonAndLine: " + bi.distanceBetweenButtonAndLine + ", lineLengthAdjustment: " + bi.lineLengthAdjustment + ", useMultiLanguage: " + bi.useMultiLanguage + ", indicationText: " + bi.indicationText);

				// find component by name
				string partName = null;
				string partName1 = null;
				string partName2 = null;
				string indicationKey = null;
				switch (bi.keyType)
				{
					case AutoButtonIndication.KeyIndicator.Trigger:
						partName = "_[CM]_TriggerKey";
						partName1 = "__CM__TriggerKey";
						partName2 = "__CM__TriggerKey.__CM__TriggerKey";
						indicationKey = "TriggerKey";
						break;
					case AutoButtonIndication.KeyIndicator.TouchPad:
						partName = "_[CM]_TouchPad";
						partName1 = "__CM__TouchPad";
						partName2 = "__CM__TouchPad.__CM__TouchPad";
						indicationKey = "TouchPad";
						break;
					case AutoButtonIndication.KeyIndicator.Grip:
						partName = "_[CM]_Grip";
						partName1 = "__CM__Grip";
						partName2 = "__CM__Grip.__CM__Grip";
						indicationKey = "Grip";
						break;
					case AutoButtonIndication.KeyIndicator.DPad_Left:
						partName = "_[CM]_DPad_Left";
						partName1 = "__CM__DPad_Left";
						partName2 = "__CM__DPad_Left.__CM__DPad_Left";
						indicationKey = "DPad_Left";
						break;
					case AutoButtonIndication.KeyIndicator.DPad_Right:
						partName = "_[CM]_DPad_Right";
						partName1 = "__CM__DPad_Right";
						partName2 = "__CM__DPad_Right.__CM__DPad_Right";
						indicationKey = "DPad_Right";
						break;
					case AutoButtonIndication.KeyIndicator.DPad_Up:
						partName = "_[CM]_DPad_Up";
						partName1 = "__CM__DPad_Up";
						partName2 = "__CM__DPad_Up.__CM__DPad_Up";
						indicationKey = "DPad_Up";
						break;
					case AutoButtonIndication.KeyIndicator.DPad_Down:
						partName = "_[CM]_DPad_Down";
						partName1 = "__CM__DPad_Down";
						partName2 = "__CM__DPad_Down.__CM__DPad_Down";
						indicationKey = "DPad_Down";
						break;
					case AutoButtonIndication.KeyIndicator.App:
						partName = "_[CM]_AppButton";
						partName1 = "__CM__AppButton";
						partName2 = "__CM__AppButton.__CM__AppButton";
						indicationKey = "AppKey";
						break;
					case AutoButtonIndication.KeyIndicator.Home:
						partName = "_[CM]_HomeButton";
						partName1 = "__CM__HomeButton";
						partName2 = "__CM__HomeButton.__CM__HomeButton";
						indicationKey = "HomeKey";
						break;
					case AutoButtonIndication.KeyIndicator.Volume:
						partName = "_[CM]_VolumeKey";
						partName1 = "__CM__VolumeKey";
						partName2 = "__CM__VolumeKey.__CM__VolumeKey";
						indicationKey = "VolumeKey";
						break;
					case AutoButtonIndication.KeyIndicator.VolumeUp:
						partName = "_[CM]_VolumeUp";
						partName1 = "__CM__VolumeUp";
						partName2 = "__CM__VolumeUp.__CM__VolumeUp";
						indicationKey = "VolumeUp";
						break;
					case AutoButtonIndication.KeyIndicator.VolumeDown:
						partName = "_[CM]_VolumeDown";
						partName1 = "__CM__VolumeDown";
						partName2 = "__CM__VolumeDown.__CM__VolumeDown";
						indicationKey = "VolumeDown";
						break;
					case AutoButtonIndication.KeyIndicator.DigitalTrigger:
						partName = "_[CM]_DigitalTriggerKey";
						partName1 = "__CM__DigitalTriggerKey";
						partName2 = "__CM__DigitalTriggerKey.__CM__DigitalTriggerKey";
						indicationKey = "DigitalTriggerKey";
						break;
					case AutoButtonIndication.KeyIndicator.ButtonA:
						partName = "_[CM]_ButtonA";
						partName1 = "__CM__ButtonA";
						partName2 = "__CM__ButtonA.__CM__ButtonA";
						indicationKey = "ButtonA";
						break;
					case AutoButtonIndication.KeyIndicator.ButtonB:
						partName = "_[CM]_ButtonB";
						partName1 = "__CM__ButtonB";
						partName2 = "__CM__ButtonB.__CM__ButtonB";
						indicationKey = "ButtonB";
						break;
					case AutoButtonIndication.KeyIndicator.ButtonX:
						partName = "_[CM]_ButtonX";
						partName1 = "__CM__ButtonX";
						partName2 = "__CM__ButtonX.__CM__ButtonX";
						indicationKey = "ButtonX";
						break;
					case AutoButtonIndication.KeyIndicator.ButtonY:
						partName = "_[CM]_ButtonY";
						partName1 = "__CM__ButtonY";
						partName2 = "__CM__ButtonY.__CM__ButtonY";
						indicationKey = "ButtonY";
						break;
					default:
						partName = "_[CM]_unknown";
						partName1 = "__CM__unknown";
						partName2 = "__CM__unknown.__CM__unknown";
						indicationKey = "unknown";
						Log.d(LOG_TAG, "Unknown key type!");
						break;
				}

				Transform tmp = transform.Find(partName);
				if (tmp == null)
				{
					tmp = transform.Find(partName1);
					if (tmp == null)
					{
						tmp = transform.Find(partName2);
					}
				}

				if (tmp != null)
				{
					AutoComponentsIndication tmpCom = new AutoComponentsIndication();

					tmpCom.name = tmp.name;
					tmpCom.sourceObject = tmp.gameObject;
					tmpCom.indicationKey = indicationKey;
					tmpCom.alignment = bi.alignment;
					tmpCom.distanceBetweenButtonAndText = bi.distanceBetweenButtonAndText;
					tmpCom.distanceBetweenButtonAndLine = bi.distanceBetweenButtonAndLine;
					tmpCom.lineLengthAdjustment = bi.lineLengthAdjustment;
					tmpCom.useMultiLanguage = bi.useMultiLanguage;
					tmpCom.indicationText = bi.indicationText;

					compInd.Add(tmpCom);
				}
				else
				{
					Log.i(LOG_TAG, "Neither " + partName + " or " + partName1 + " or " + partName2 + " is not in the model!");
				}
			}

			Sort();
			FindDisplayPlane();
			DisplayArrangement();
			CreateLineAndText(leftList);
			CreateLineAndText(rightList);

			emitter = null;
			if (basedOnEmitter)
			{
				RenderModel wrm = this.GetComponentInChildren<RenderModel>();

				if (wrm != null)
				{
					GameObject modelObj = wrm.gameObject;

					int modelchild = modelObj.transform.childCount;
					for (int j = 0; j < modelchild; j++)
					{
						GameObject childName = modelObj.transform.GetChild(j).gameObject;
						if (childName.name == "__CM__Emitter" || childName.name == "_[CM]_Emitter")
						{
							emitter = childName;
						}
					}
				}
			}

			needRedraw = false;
		}

		void UpdateInfo(List<AutoComponentsIndication> tmpComp)
		{
			Vector3 _targetForward;
			if (basedOnEmitter && (emitter != null))
				_targetForward = emitter.transform.rotation * Vector3.forward;
			else
				_targetForward = transform.rotation * Vector3.forward;
			Vector3 _targetRight = transform.rotation * Vector3.right;
			Vector3 _targetUp = transform.rotation * Vector3.up;

			float zAngle = Vector3.Angle(_targetForward, hmd.transform.forward);
			float xAngle = Vector3.Angle(_targetRight, hmd.transform.right);
#if DEBUG
			float yAngle = Vector3.Angle(_targetUp, hmd.transform.up);

			if (Log.gpl.Print)
				Log.d(LOG_TAG, "Z: " + _targetForward + ":" + zAngle + ", X: " + _targetRight + ":" + xAngle + ", Y: " + _targetUp + ":" + yAngle);
#endif
			if ((_targetForward.y < (showIndicatorAngle / 90f)) || (zAngle < showIndicatorAngle))
			{
				foreach (AutoComponentsIndication ci in tmpComp)
				{
					if (ci.lineIndicator != null)
					{
						ci.lineIndicator.SetActive(false);
					}
					if (ci.destObject != null)
					{
						ci.destObject.SetActive(false);
					}
				}

				return;
			}

			if (hideIndicatorByRoll)
			{

				if (xAngle > 90.0f)
				{
					foreach (AutoComponentsIndication ci in tmpComp)
					{
						if (ci.lineIndicator != null)
						{
							ci.lineIndicator.SetActive(false);
						}
						if (ci.destObject != null)
						{
							ci.destObject.SetActive(false);
						}
					}
					return;
				}
			}

			foreach (AutoComponentsIndication ci in tmpComp)
			{
				if (ci.sourceObject != null)
				{
					ci.sourceObject.SetActive(true);
				}

				if (ci.lineIndicator != null)
				{
					ci.lineIndicator.SetActive(true);
				}

				if (ci.destObject != null)
				{
					ci.destObject.SetActive(true);
				}
			}
		}

		// reset for redraw
		void ResetIndicator()
		{
			if (showIndicator)
			{
				rw = ResourceWrapper.instance;
				sysLang = rw.getSystemLanguage();
				sysCountry = rw.getSystemCountry();

				needRedraw = true;
				ClearResourceAndObject();
			}
		}

		void ClearResourceAndObject()
		{
			Log.d(LOG_TAG, "clear Indicator!");
			foreach (AutoComponentsIndication ci in compInd)
			{
				if (ci.destObject != null)
				{
					Destroy(ci.destObject);
				}
				if (ci.lineIndicator != null)
				{
					Destroy(ci.lineIndicator);
				}
			}
			compInd.Clear();

			foreach (AutoComponentsIndication leftComp in leftList)
			{
				if (leftComp.destObject != null)
				{
					Destroy(leftComp.destObject);
				}
				if (leftComp.lineIndicator != null)
				{
					Destroy(leftComp.lineIndicator);
				}
			}
			leftList.Clear();

			foreach (AutoComponentsIndication rightComp in rightList)
			{
				if (rightComp.destObject != null)
				{
					Destroy(rightComp.destObject);
				}
				if (rightComp.lineIndicator != null)
				{
					Destroy(rightComp.lineIndicator);
				}
			}
			rightList.Clear();

			Resources.UnloadUnusedAssets();
		}

		void OnAdaptiveControllerModelReady(XR_Hand hand)
		{
			CreateIndicator();
		}

		void FindDisplayPlane()
		{
			if (compInd.Count == 0)
				return;

			displayPlaneY = compInd[0].sourceObject.transform.localPosition.y;
			if (displayPlane == DisplayPlane.Body_Up)
			{
				for (int i = 0; i < compInd.Count; i++)
				{
					if (displayPlaneY < compInd[i].sourceObject.transform.localPosition.y)
					{
						displayPlaneY = compInd[i].sourceObject.transform.localPosition.y;
					}
				}
			}
			if (displayPlane == DisplayPlane.Body_Middle)
			{
				displayPlaneY = body.transform.localPosition.y;
			}
			if (displayPlane == DisplayPlane.Body_Bottom)
			{
				for (int i = 0; i < compInd.Count; i++)
				{
					if (displayPlaneY > compInd[i].sourceObject.transform.localPosition.y)
					{
						displayPlaneY = compInd[i].sourceObject.transform.localPosition.y;
					}
				}
			}

		}

		void Sort()
		{
			if (compInd.Count == 0)
				return;

			leftCount = 0;
			rightCount = 0;

			leftList.Clear();
			rightList.Clear();

			if (body != null)
			{
				//Determine the priority : insertion sort by localPosition.z
				int i, j;
				float tmpZ;
				AutoComponentsIndication tmpComp;

				for (i = 0; i < compInd.Count; i++)
				{
					tmpZ = compInd[i].sourceObject.transform.localPosition.z;
					tmpComp = compInd[i];
					for (j = i; j > 0 && tmpZ > compInd[j - 1].sourceObject.transform.localPosition.z; j--)
					{
						compInd[j] = compInd[j - 1];
					}
					compInd[j] = tmpComp;
				}
				//Left or right : distance of localPosition.x between the body and the button
				for (i = 0; i < compInd.Count; i++)
				{
					if (!compInd[i].leftRightFlag)
					{
						if (System.Math.Round(compInd[i].sourceObject.transform.localPosition.x - body.transform.localPosition.x, 3) > 0)
						{
							compInd[i].alignment = AutoButtonIndication.Alignment.Right;
							compInd[i].leftRightFlag = true;
							rightCount++;
#if DEBUG
							Log.i(LOG_TAG, "Round 1 : " + compInd[i].name + "  " + compInd[i].alignment);
#endif
						}
						if (System.Math.Round(compInd[i].sourceObject.transform.localPosition.x - body.transform.localPosition.x, 3) < 0)
						{
							compInd[i].alignment = AutoButtonIndication.Alignment.Left;
							compInd[i].leftRightFlag = true;
							leftCount++;
#if DEBUG
							Log.i(LOG_TAG, "Round 1 : " + compInd[i].name + "  " + compInd[i].alignment);
#endif
						}
					}
				}
				//Left or right : user-defined
				for (i = 0; i < compInd.Count; i++)
				{
					if (!compInd[i].leftRightFlag)
					{
						if (compInd[i].alignment == AutoButtonIndication.Alignment.Right)
						{
							compInd[i].leftRightFlag = true;
							rightCount++;
#if DEBUG
							Log.i(LOG_TAG, "Round 2 : " + compInd[i].name + "  " + compInd[i].alignment);
#endif
						}
						if (compInd[i].alignment == AutoButtonIndication.Alignment.Left)
						{
							compInd[i].leftRightFlag = true;
							leftCount++;
#if DEBUG
							Log.i(LOG_TAG, "Round 2 : " + compInd[i].name + "  " + compInd[i].alignment);
#endif
						}
					}
				}
				//Left or right : auto-balance
				for (i = 0; i < compInd.Count; i++)
				{
					if (!compInd[i].leftRightFlag && compInd[i].alignment == AutoButtonIndication.Alignment.Balance)
					{
						if (rightCount <= leftCount)
						{
							compInd[i].alignment = AutoButtonIndication.Alignment.Right;
							compInd[i].leftRightFlag = true;
							rightCount++;
#if DEBUG
							Log.i(LOG_TAG, "Round 3 : " + compInd[i].name + "  " + compInd[i].alignment);
#endif
						}
						else
						{
							compInd[i].alignment = AutoButtonIndication.Alignment.Left;
							compInd[i].leftRightFlag = true;
							leftCount++;
#if DEBUG
							Log.i(LOG_TAG, "Round 3 : " + compInd[i].name + "  " + compInd[i].alignment);
#endif
						}
					}

				}
				for (i = 0; i < compInd.Count; i++)
				{
					if (compInd[i].alignment == AutoButtonIndication.Alignment.Left)
						leftList.Add(compInd[i]);
					if (compInd[i].alignment == AutoButtonIndication.Alignment.Right)
						rightList.Add(compInd[i]);
				}
			}
		}

		//Automatically arrange the z value of each component
		void DisplayArrangement()
		{
			float ratio = 0.3f;

			if (rightList.Count >= 1)
			{
				if (rightList.Count == 1)
				{
					rightList[0].zValue = rightList[0].sourceObject.transform.localPosition.z;
				}
				else
				{
					float rightRange = rightList[0].sourceObject.transform.localPosition.z - rightList[rightList.Count - 1].sourceObject.transform.localPosition.z;
					float rightZoomRange = rightRange * (1 + ratio);
					float offset = (rightZoomRange - rightRange) / 2;
					float rightOffset = rightZoomRange / (rightList.Count - 1);

					for (int i = 0; i < rightList.Count; i++)
					{
						rightList[i].zValue = (rightList[0].sourceObject.transform.localPosition.z + offset) - rightOffset * i;
					}
				}
			}

			if (leftList.Count >= 1)
			{

				if (leftList.Count == 1)
				{
					leftList[0].zValue = leftList[0].sourceObject.transform.localPosition.z;
				}
				else
				{
					float leftRange = leftList[0].sourceObject.transform.localPosition.z - leftList[leftList.Count - 1].sourceObject.transform.localPosition.z;
					float leftZoomRange = leftRange * (1 + ratio);
					float offset = (leftZoomRange - leftRange) / 2;
					float leftOffset = leftZoomRange / (leftList.Count - 1);


					for (int i = 0; i < leftList.Count; i++)
					{
						leftList[i].zValue = (leftList[0].sourceObject.transform.localPosition.z + offset) - leftOffset * i;
					}
				}
			}
		}

		void CreateLineAndText(List<AutoComponentsIndication> tmpComp)
		{
			if (tmpComp.Count == 0)
				return;

			Log.d(LOG_TAG, "CreateLineAndText");

			foreach (AutoComponentsIndication comp in tmpComp)
			{
				Quaternion spawnRot = Quaternion.identity;
				spawnRot = transform.rotation;

				// destObject :  instantiate indicator and fill in the text
				comp.destObject = null;
				Vector3 destPos = Vector3.zero;
				if (comp.alignment == AutoButtonIndication.Alignment.Right)
				{
					if (displayPlane == DisplayPlane.Button)
						destPos = transform.TransformPoint(new Vector3(comp.distanceBetweenButtonAndText, comp.sourceObject.transform.localPosition.y, comp.sourceObject.transform.localPosition.z));
					else if (displayPlane == DisplayPlane.Button_Auto)
						destPos = transform.TransformPoint(new Vector3(comp.distanceBetweenButtonAndText, comp.sourceObject.transform.localPosition.y, comp.zValue));
					else
						destPos = transform.TransformPoint(new Vector3(comp.distanceBetweenButtonAndText, displayPlaneY, comp.zValue));
				}
				else
				{
					if (displayPlane == DisplayPlane.Button)
						destPos = transform.TransformPoint(new Vector3(-comp.distanceBetweenButtonAndText, comp.sourceObject.transform.localPosition.y, comp.sourceObject.transform.localPosition.z));
					else if (displayPlane == DisplayPlane.Button_Auto)
						destPos = transform.TransformPoint(new Vector3(-comp.distanceBetweenButtonAndText, comp.sourceObject.transform.localPosition.y, comp.zValue));
					else
						destPos = transform.TransformPoint(new Vector3(-comp.distanceBetweenButtonAndText, displayPlaneY, comp.zValue));
				}

				GameObject destGO = Instantiate(indicatorPrefab, destPos, spawnRot);
				destGO.name = comp.name + "Ind";
				destGO.transform.parent = comp.sourceObject.transform;

				int childC = destGO.transform.childCount;
				for (int i = 0; i < childC; i++)
				{
					GameObject c = destGO.transform.GetChild(i).gameObject;
					if (comp.alignment == AutoButtonIndication.Alignment.Left)
					{
						float tx = c.transform.localPosition.x;
						c.transform.localPosition = new Vector3(tx * (-1), c.transform.localPosition.y, c.transform.localPosition.z);
					}
					TextMesh tm = c.GetComponent<TextMesh>();
					MeshRenderer mr = c.GetComponent<MeshRenderer>();

					if (tm == null) Log.i(LOG_TAG, " tm is null ");
					if (mr == null) Log.i(LOG_TAG, " mr is null ");

					if (tm != null && mr != null)
					{
						tm.characterSize = textCharacterSize;
						if (c.name != "Shadow")
						{
							mr.material.SetColor("_Color", textColor);
						}
						else
						{
							Log.d(LOG_TAG, " Shadow found ");
						}
						tm.fontSize = textFontSize;
						if (comp.useMultiLanguage)
						{
							sysLang = rw.getSystemLanguage();
							sysCountry = rw.getSystemCountry();
							Log.d(LOG_TAG, " System language is " + sysLang);
							if (sysLang.StartsWith("zh"))
							{
								Log.d(LOG_TAG, " Chinese language");
								tm.characterSize = zhCharactarSize;
							}

							// use default string - multi-language
							if (comp.indicationText == "system")
							{
								tm.text = rw.getString(comp.indicationKey);
								Log.i(LOG_TAG, " Name: " + destGO.name + " uses default multi-language -> " + tm.text);
							}
							else
							{
								tm.text = rw.getString(comp.indicationText);
								if (tm.text == "")
									tm.text = comp.indicationText;
								Log.i(LOG_TAG, " Name: " + destGO.name + " uses custom multi-language -> " + tm.text);
							}
						}
						else
						{
							if (comp.indicationText == "system")
								tm.text = comp.indicationKey;
							else
								tm.text = comp.indicationText;

							Log.i(LOG_TAG, " Name: " + destGO.name + " didn't uses multi-language -> " + tm.text);
						}

						if (comp.alignment == AutoButtonIndication.Alignment.Left)
						{
							tm.anchor = TextAnchor.MiddleRight;
							tm.alignment = TextAlignment.Right;
						}
					}
				}

				destGO.SetActive(false);
				comp.destObject = destGO;

				// lineIndicator : instantiate line
				comp.lineIndicator = null;
				Vector3 linePos = Vector3.zero;
				if (comp.alignment == AutoButtonIndication.Alignment.Right)
				{
					linePos = transform.TransformPoint(new Vector3(comp.sourceObject.transform.localPosition.x + comp.distanceBetweenButtonAndLine, comp.sourceObject.transform.localPosition.y, comp.sourceObject.transform.localPosition.z));
				}
				else if (comp.alignment == AutoButtonIndication.Alignment.Left)
				{
					linePos = transform.TransformPoint(new Vector3(comp.sourceObject.transform.localPosition.x - comp.distanceBetweenButtonAndLine, comp.sourceObject.transform.localPosition.y, comp.sourceObject.transform.localPosition.z));
				}

				// Create line
				GameObject lineGO = Instantiate(linePrefab, linePos, spawnRot);
				lineGO.name = comp.name + "Line";
				lineGO.transform.parent = comp.sourceObject.transform;

				float tmpLength = Vector3.Distance(comp.sourceObject.transform.position, comp.destObject.transform.position) + comp.lineLengthAdjustment;
				if (tmpLength < 0)
					tmpLength = 0;

				var li = lineGO.GetComponent<IndicatorLine>();
				li.autoLayout = autoLayout;
				li.lineColor = lineColor;
				li.lineLength = tmpLength;
				li.startWidth = lineStartWidth;
				li.endWidth = lineEndWidth;
				li.autoAlignment = comp.alignment;
				li.updateMeshSettings();

				Vector3 dir = comp.destObject.transform.position - comp.sourceObject.transform.position;

				Quaternion tmp = Quaternion.LookRotation(dir, transform.up);
				if (comp.alignment == AutoButtonIndication.Alignment.Right)
					lineGO.transform.rotation = tmp * Quaternion.AngleAxis(-90, new Vector3(0, 1, 0));
				else
					lineGO.transform.rotation = tmp * Quaternion.AngleAxis(90, new Vector3(0, 1, 0));

				lineGO.SetActive(false);
				comp.lineIndicator = lineGO;
			}
		}

		void OnEnable()
		{
			RenderModel.onRenderModelReady += OnAdaptiveControllerModelReady;
		}

		void OnDisable()
		{
			RenderModel.onRenderModelReady -= OnAdaptiveControllerModelReady;
		}

		void OnApplicationPause(bool pauseStatus)
		{
			if (pauseStatus == true)
			{
				ResetIndicator();
			}
		}

		// Use this for initialization
		void Start()
		{
		}

		// Update is called once per frame
		void Update()
		{
			if (!showIndicator) return;
			if (hmd == null) return;
			checkCount++;
			if (checkCount > 50)
			{
				checkCount = 0;
				if (rw != null)
				{
					if (rw.getSystemLanguage() != sysLang || rw.getSystemCountry() != sysCountry) ResetIndicator();
				}
			}
			if (needRedraw == true) CreateIndicator();

			UpdateInfo(leftList);
			UpdateInfo(rightList);
		}
	}
}
