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
using Wave.Essence.Controller;

#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(CustomizeWaveController))]
public class CustomizeWaveControllerEditor : Editor
{
	//For ShowIndicator
	private bool _buttonList = false;
	private bool _element = false;

	public override void OnInspectorGUI()
	{
		CustomizeWaveController myScript = target as CustomizeWaveController;

		EditorGUILayout.LabelField("Button Effect");

			myScript.enableButtonEffect = EditorGUILayout.Toggle("  Enable button effect", myScript.enableButtonEffect);
			if (true == myScript.enableButtonEffect)
			{
				myScript.useSystemDefinedColor = EditorGUILayout.Toggle("  Apply system config", myScript.useSystemDefinedColor);
				if (true != myScript.useSystemDefinedColor)
				{
					myScript.buttonEffectColor = EditorGUILayout.ColorField("    Button effect color", myScript.buttonEffectColor);
				}
			}

		EditorGUILayout.Space();
		EditorGUILayout.LabelField("  Indication feature");
		myScript.overwriteIndicatorSettings = true;
		myScript.showIndicator = EditorGUILayout.Toggle("    Show Indicator", myScript.showIndicator);
		if (myScript.showIndicator)
		{
			myScript.autoLayout = EditorGUILayout.Toggle("    Auto Layout", myScript.autoLayout);
			if (!myScript.autoLayout)
			{
				myScript.showIndicatorAngle = EditorGUILayout.Slider("    Show Indicator Angle", myScript.showIndicatorAngle, 0.0f, 90.0f);
				myScript.hideIndicatorByRoll = EditorGUILayout.Toggle("    Hide Indicator By Roll", myScript.hideIndicatorByRoll);
				myScript.basedOnEmitter = EditorGUILayout.Toggle("    Based On Emitter", myScript.basedOnEmitter);
				EditorGUILayout.Space();

				EditorGUILayout.LabelField("  Line customization");
				myScript.lineLength = EditorGUILayout.Slider("    Line Length", myScript.lineLength, 0.01f, 0.1f);
				myScript.lineStartWidth = EditorGUILayout.Slider("    Line Start Width", myScript.lineStartWidth, 0.0001f, 0.1f);
				myScript.lineEndWidth = EditorGUILayout.Slider("    Line End Width", myScript.lineEndWidth, 0.0001f, 0.1f);
				myScript.lineColor = EditorGUILayout.ColorField("    Line Color", myScript.lineColor);
				EditorGUILayout.Space();

				EditorGUILayout.LabelField("  Text customization");
				myScript.textCharacterSize = EditorGUILayout.Slider("    Text Character Size", myScript.textCharacterSize, 0.01f, 0.2f);
				myScript.zhCharactarSize = EditorGUILayout.Slider("    Zh Charactar Size", myScript.zhCharactarSize, 0.01f, 0.2f);
				myScript.textFontSize = EditorGUILayout.IntSlider("    Text Font Size", myScript.textFontSize, 50, 200);
				myScript.textColor = EditorGUILayout.ColorField("    Text Color", myScript.textColor);
				EditorGUILayout.Space();
			}
			else
			{
				myScript.showIndicatorAngle = EditorGUILayout.Slider("    Show Indicator Angle", myScript.showIndicatorAngle, 0.0f, 90.0f);
				myScript.hideIndicatorByRoll = EditorGUILayout.Toggle("    Hide Indicator By Roll", myScript.hideIndicatorByRoll);
				myScript.basedOnEmitter = EditorGUILayout.Toggle("    Based On Emitter", myScript.basedOnEmitter);
				myScript._displayPlane = (DisplayPlane)EditorGUILayout.EnumPopup("    Display Plane", myScript._displayPlane);
				EditorGUILayout.Space();

				EditorGUILayout.LabelField("  Line customization");
				myScript.lineStartWidth = EditorGUILayout.Slider("    Line Start Width", myScript.lineStartWidth, 0.0001f, 0.1f);
				myScript.lineEndWidth = EditorGUILayout.Slider("    Line End Width", myScript.lineEndWidth, 0.0001f, 0.1f);
				myScript.lineColor = EditorGUILayout.ColorField("    Line Color", myScript.lineColor);
				EditorGUILayout.Space();

				EditorGUILayout.LabelField("  Text customization");
				myScript.textCharacterSize = EditorGUILayout.Slider("    Text Character Size", myScript.textCharacterSize, 0.01f, 0.2f);
				myScript.zhCharactarSize = EditorGUILayout.Slider("    Zh Charactar Size", myScript.zhCharactarSize, 0.01f, 0.2f);
				myScript.textFontSize = EditorGUILayout.IntSlider("    Text Font Size", myScript.textFontSize, 50, 200);
				myScript.textColor = EditorGUILayout.ColorField("    Text Color", myScript.textColor);
				EditorGUILayout.Space();
			}

			myScript.useIndicatorSystemConfig = EditorGUILayout.Toggle("  Use system config", myScript.useIndicatorSystemConfig);
			if (false == myScript.useIndicatorSystemConfig)
			{
				if (!myScript.autoLayout)
				{
					EditorGUILayout.LabelField("  Indications");
					_buttonList = EditorGUILayout.Foldout(_buttonList, "    Button Indication List");
					if (_buttonList)
					{
						var list = myScript.buttonIndicationList;

						int newCount = Mathf.Max(0, EditorGUILayout.IntField("      Size", list.Count));

						while (newCount < list.Count)
							list.RemoveAt(list.Count - 1);
						while (newCount > list.Count)
							list.Add(new ButtonIndication());

						for (int i = 0; i < list.Count; i++)
						{
							_element = EditorGUILayout.Foldout(_element, "      Element " + i);
							if (_element)
							{
								myScript.buttonIndicationList[i].keyType = (ButtonIndication.KeyIndicator)EditorGUILayout.EnumPopup("  Key Type", myScript.buttonIndicationList[i].keyType);
								myScript.buttonIndicationList[i].alignment = (ButtonIndication.Alignment)EditorGUILayout.EnumPopup("  Alignment", myScript.buttonIndicationList[i].alignment);
								myScript.buttonIndicationList[i].indicationOffset = EditorGUILayout.Vector3Field("  Indication offset", myScript.buttonIndicationList[i].indicationOffset);
								myScript.buttonIndicationList[i].useMultiLanguage = EditorGUILayout.Toggle("  Use multi-language", myScript.buttonIndicationList[i].useMultiLanguage);
								myScript.buttonIndicationList[i].indicationText = EditorGUILayout.TextField("  Indication text", myScript.buttonIndicationList[i].indicationText);
								myScript.buttonIndicationList[i].followButtonRotation = EditorGUILayout.Toggle("  Follow button rotation", myScript.buttonIndicationList[i].followButtonRotation);
								EditorGUILayout.Space();
							}
						}
					}
				}

				else
				{
					EditorGUILayout.LabelField("  Indications");
					_buttonList = EditorGUILayout.Foldout(_buttonList, "    Button Indication List");
					if (_buttonList)
					{
						var list = myScript.autoButtonIndicationList;

						int newCount = Mathf.Max(0, EditorGUILayout.IntField("      Size", list.Count));

						while (newCount < list.Count)
							list.RemoveAt(list.Count - 1);
						while (newCount > list.Count)
							list.Add(new AutoButtonIndication());

						for (int i = 0; i < list.Count; i++)
						{
							_element = EditorGUILayout.Foldout(_element, "      Element " + i);
							if (_element)
							{
								myScript.autoButtonIndicationList[i].keyType = (AutoButtonIndication.KeyIndicator)EditorGUILayout.EnumPopup("        Key Type", myScript.autoButtonIndicationList[i].keyType);
								myScript.autoButtonIndicationList[i].alignment = (AutoButtonIndication.Alignment)EditorGUILayout.EnumPopup("        Alignment", myScript.autoButtonIndicationList[i].alignment);
								myScript.autoButtonIndicationList[i].distanceBetweenButtonAndText = EditorGUILayout.Slider("        Distance Between Button And Text", myScript.autoButtonIndicationList[i].distanceBetweenButtonAndText, 0.0f, 0.1f);
								myScript.autoButtonIndicationList[i].distanceBetweenButtonAndLine = EditorGUILayout.Slider("        Distance Between Button And Line", myScript.autoButtonIndicationList[i].distanceBetweenButtonAndLine, 0.0f, 0.1f);
								myScript.autoButtonIndicationList[i].lineLengthAdjustment = EditorGUILayout.Slider("        Line Length Adjustment", myScript.autoButtonIndicationList[i].lineLengthAdjustment, -0.1f, 0.1f);
								myScript.autoButtonIndicationList[i].useMultiLanguage = EditorGUILayout.Toggle("        Use multi-language", myScript.autoButtonIndicationList[i].useMultiLanguage);
								myScript.autoButtonIndicationList[i].indicationText = EditorGUILayout.TextField("        Indication text", myScript.autoButtonIndicationList[i].indicationText);
								EditorGUILayout.Space();
							}
						}
					}
				}
			}

		}

		if (GUI.changed)
				EditorUtility.SetDirty((CustomizeWaveController)target);
		}
	}
#endif
