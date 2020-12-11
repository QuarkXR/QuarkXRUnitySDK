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
using System.Text;
using UnityEngine;
using Wave.Native;

namespace Wave.Essence.Controller
{
	public class CustomizeWaveController : MonoBehaviour
	{
		private static string LOG_TAG = "CustomizeWaveController";

		private bool mPerformSetup = false;

		public bool enableButtonEffect = true;
		public bool useSystemDefinedColor = true;
		public Color buttonEffectColor = new Color(0, 179, 227, 255);

		//for ShowIndicator
		[Header("Indication feature")]
		public bool overwriteIndicatorSettings = true;
		public bool showIndicator = false;
		public bool autoLayout = true;
		[Range(0.0f, 90.0f)]
		public float showIndicatorAngle = 30.0f;
		public bool hideIndicatorByRoll = true;
		public bool basedOnEmitter = true;

		[Header("Line customization")]
		[Range(0.01f, 0.1f)]
		public float lineLength = 0.03f;
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
		public bool useIndicatorSystemConfig = true;
		public List<ButtonIndication> buttonIndicationList = new List<ButtonIndication>();

		//For AutoLayout
		[HideInInspector]
		public DisplayPlane _displayPlane = DisplayPlane.Button_Auto;
		[HideInInspector]
		public List<AutoButtonIndication> autoButtonIndicationList = new List<AutoButtonIndication>();

		void OnEnable()
		{

		}

		// Start is called before the first frame update
		void Start()
		{

		}

		// Update is called once per frame
		void Update()
		{
			if (!mPerformSetup)
			{
				// render model
				RenderModel rm = this.gameObject.GetComponentInChildren<RenderModel>();
				if (rm == null)
				{
					PrintInfoLog("Can't get RenderModel script in the model");
					return;
				}

				GameObject spawned = rm.controllerSpawned;

				if (spawned == null)
				{
					return;
				}

				PrintInfoLog("spawned controller name: " + spawned.name);

				ButtonEffect be = spawned.GetComponent<ButtonEffect>();

				if (be == null)
				{
					be = spawned.GetComponentInChildren<ButtonEffect>();
					if (be == null)
					{
						PrintInfoLog("Can't get ButtonEffect script in the model");
						return;
					}
				}

				StringBuilder sb = new StringBuilder();
				sb.Append("Customize Wave controller model - ");
				sb.Append(rm.WhichHand);
				sb.AppendLine();

				sb.Append("ButtonEffect: ");
				sb.AppendLine();
				sb.Append("  enableButtonEffect from ");
				sb.Append(be.enableButtonEffect);
				sb.Append("  to ");
				sb.Append(this.enableButtonEffect);
				sb.AppendLine();
				sb.Append("  useSystemDefinedColor from ");
				sb.Append(be.useSystemConfig);
				sb.Append("  to ");
				sb.Append(this.useSystemDefinedColor);
				sb.AppendLine();
				sb.Append("  Color from ");
				sb.Append(be.buttonEffectColor);
				sb.Append("  to ");
				sb.Append(this.buttonEffectColor);
				sb.AppendLine();

				Log.i(LOG_TAG, sb.ToString(), true);

				bool changed = ((be.enableButtonEffect != this.enableButtonEffect) ||
								(be.useSystemConfig != this.useSystemDefinedColor) ||
								(be.buttonEffectColor != this.buttonEffectColor) ||
								this.showIndicator);

				be.enableButtonEffect = this.enableButtonEffect;
				be.useSystemConfig = this.useSystemDefinedColor;
				be.buttonEffectColor = this.buttonEffectColor;

				if (this.showIndicator)
				{
					ShowIndicator si = spawned.GetComponent<ShowIndicator>();

					if (si == null)
					{
						si = spawned.GetComponentInChildren<ShowIndicator>();
						if (si == null)
						{
							PrintInfoLog("Can't get ShowIndicator script in the model");
							return;
						}
					}

					si.showIndicator = true;
					PrintInfoLog(" ApplyIndicatorParameters ");
					ApplyIndicatorParameters(si);
					PrintInfoLog(" CreateIndicator ");
					si.createIndicator();
				}
				mPerformSetup = true;
			}
		}

		private void PrintInfoLog(string msg)
		{
			Log.i(LOG_TAG, msg, true);
		}

		private void ApplyIndicatorParameters(ShowIndicator si)
		{
			if (showIndicator != true)
			{
				PrintInfoLog("forced don't show ShowIndicator!");
				return;
			}
			si.showIndicator = showIndicator;
			si.autoLayout = autoLayout;
			si.showIndicatorAngle = showIndicatorAngle;
			si.hideIndicatorByRoll = hideIndicatorByRoll;
			si.basedOnEmitter = basedOnEmitter;
			si.lineLength = lineLength;
			si.lineStartWidth = lineStartWidth;
			si.lineEndWidth = lineEndWidth;
			si.lineColor = lineColor;
			si.textCharacterSize = textCharacterSize;
			si.zhCharactarSize = zhCharactarSize;
			si.textFontSize = textFontSize;
			si.textColor = textColor;

			si.buttonIndicationList.Clear();

			if (!autoLayout)
			{
				if (useIndicatorSystemConfig)
				{
					PrintInfoLog("uses system default button indication!");
					addButtonIndicationList();
				}
				else
				{
					PrintInfoLog("uses customized button indication!");
					if (buttonIndicationList.Count == 0)
					{
						PrintInfoLog("doesn't have button indication!");
						return;
					}
				}

				foreach (ButtonIndication bi in buttonIndicationList)
				{
					PrintInfoLog("keyType: " + bi.keyType);
					PrintInfoLog("alignment: " + bi.alignment);
					PrintInfoLog("indicationOffset: " + bi.indicationOffset);
					PrintInfoLog("useMultiLanguage: " + bi.useMultiLanguage);
					PrintInfoLog("indicationText: " + bi.indicationText);
					PrintInfoLog("followButtonRotation: " + bi.followButtonRotation);

					si.buttonIndicationList.Add(bi);
				}

				si.createIndicator();
			}

			si._displayPlane = _displayPlane;
			si.autoButtonIndicationList.Clear();
			if (autoLayout)
			{
				if (useIndicatorSystemConfig)
				{
					PrintInfoLog("uses system default button indication!");
					addAutoButtonIndicationList();
				}
				else
				{
					PrintInfoLog("uses customized button indication!");
					if (autoButtonIndicationList.Count == 0)
					{
						PrintInfoLog("doesn't have button indication!");
						return;
					}
				}

				foreach (AutoButtonIndication bi in autoButtonIndicationList)
				{
					PrintInfoLog("keyType: " + bi.keyType);
					PrintInfoLog("alignment: " + bi.alignment);
					PrintInfoLog("distanceBetweenButtonAndText: " + bi.distanceBetweenButtonAndText);
					PrintInfoLog("distanceBetweenButtonAndLine: " + bi.distanceBetweenButtonAndLine);
					PrintInfoLog("lineLengthAdjustment: " + bi.lineLengthAdjustment);
					PrintInfoLog("useMultiLanguage: " + bi.useMultiLanguage);
					PrintInfoLog("indicationText: " + bi.indicationText);

					si.autoButtonIndicationList.Add(bi);
				}

			}
		}

		private void addButtonIndicationList()
		{
			buttonIndicationList.Clear();

			ButtonIndication home = new ButtonIndication();
			home.keyType = ButtonIndication.KeyIndicator.Home;
			home.alignment = ButtonIndication.Alignment.RIGHT;
			home.indicationOffset = new Vector3(0f, 0f, 0f);
			home.useMultiLanguage = true;
			home.indicationText = "system";
			home.followButtonRotation = true;

			buttonIndicationList.Add(home);

			ButtonIndication app = new ButtonIndication();
			app.keyType = ButtonIndication.KeyIndicator.App;
			app.alignment = ButtonIndication.Alignment.LEFT;
			app.indicationOffset = new Vector3(0f, 0.0004f, 0f);
			app.useMultiLanguage = true;
			app.indicationText = "system";
			app.followButtonRotation = true;

			buttonIndicationList.Add(app);

			ButtonIndication grip = new ButtonIndication();
			grip.keyType = ButtonIndication.KeyIndicator.Grip;
			grip.alignment = ButtonIndication.Alignment.RIGHT;
			grip.indicationOffset = new Vector3(0f, 0f, 0.01f);
			grip.useMultiLanguage = true;
			grip.indicationText = "system";
			grip.followButtonRotation = true;

			buttonIndicationList.Add(grip);

			ButtonIndication trigger = new ButtonIndication();
			trigger.keyType = ButtonIndication.KeyIndicator.Trigger;
			trigger.alignment = ButtonIndication.Alignment.RIGHT;
			trigger.indicationOffset = new Vector3(0f, 0f, 0f);
			trigger.useMultiLanguage = true;
			trigger.indicationText = "system";
			trigger.followButtonRotation = true;

			buttonIndicationList.Add(trigger);

			ButtonIndication dt = new ButtonIndication();
			dt.keyType = ButtonIndication.KeyIndicator.DigitalTrigger;
			dt.alignment = ButtonIndication.Alignment.RIGHT;
			dt.indicationOffset = new Vector3(0f, 0f, 0f);
			dt.useMultiLanguage = true;
			dt.indicationText = "system";
			dt.followButtonRotation = true;

			buttonIndicationList.Add(dt);

			ButtonIndication bumper = new ButtonIndication();
			bumper.keyType = ButtonIndication.KeyIndicator.Bumper;
			bumper.alignment = ButtonIndication.Alignment.RIGHT;
			bumper.indicationOffset = new Vector3(0f, 0f, 0f);
			bumper.useMultiLanguage = true;
			bumper.indicationText = "system";
			bumper.followButtonRotation = true;

			buttonIndicationList.Add(bumper);

			ButtonIndication touchpad = new ButtonIndication();
			touchpad.keyType = ButtonIndication.KeyIndicator.TouchPad;
			touchpad.alignment = ButtonIndication.Alignment.LEFT;
			touchpad.indicationOffset = new Vector3(0f, 0f, 0f);
			touchpad.useMultiLanguage = true;
			touchpad.indicationText = "system";
			touchpad.followButtonRotation = true;

			buttonIndicationList.Add(touchpad);

			ButtonIndication vol = new ButtonIndication();
			vol.keyType = ButtonIndication.KeyIndicator.Volume;
			vol.alignment = ButtonIndication.Alignment.RIGHT;
			vol.indicationOffset = new Vector3(0f, 0f, 0f);
			vol.useMultiLanguage = true;
			vol.indicationText = "system";
			vol.followButtonRotation = true;

			buttonIndicationList.Add(vol);

			ButtonIndication buttonA = new ButtonIndication();
			buttonA.keyType = ButtonIndication.KeyIndicator.ButtonA;
			buttonA.alignment = ButtonIndication.Alignment.LEFT;
			buttonA.indicationOffset = new Vector3(0f, 0f, 0f);
			buttonA.useMultiLanguage = true;
			buttonA.indicationText = "system";
			buttonA.followButtonRotation = true;

			buttonIndicationList.Add(buttonA);

			ButtonIndication buttonB = new ButtonIndication();
			buttonB.keyType = ButtonIndication.KeyIndicator.ButtonB;
			buttonB.alignment = ButtonIndication.Alignment.LEFT;
			buttonB.indicationOffset = new Vector3(0f, 0f, 0f);
			buttonB.useMultiLanguage = true;
			buttonB.indicationText = "system";
			buttonB.followButtonRotation = true;

			buttonIndicationList.Add(buttonB);

			ButtonIndication buttonX = new ButtonIndication();
			buttonX.keyType = ButtonIndication.KeyIndicator.ButtonX;
			buttonX.alignment = ButtonIndication.Alignment.RIGHT;
			buttonX.indicationOffset = new Vector3(0f, 0f, 0f);
			buttonX.useMultiLanguage = true;
			buttonX.indicationText = "system";
			buttonX.followButtonRotation = true;

			buttonIndicationList.Add(buttonX);

			ButtonIndication buttonY = new ButtonIndication();
			buttonY.keyType = ButtonIndication.KeyIndicator.ButtonY;
			buttonY.alignment = ButtonIndication.Alignment.RIGHT;
			buttonY.indicationOffset = new Vector3(0f, 0f, 0f);
			buttonY.useMultiLanguage = true;
			buttonY.indicationText = "system";
			buttonY.followButtonRotation = true;

			buttonIndicationList.Add(buttonY);
		}

		private void addAutoButtonIndicationList()
		{
			autoButtonIndicationList.Clear();

			AutoButtonIndication home = new AutoButtonIndication();
			home.keyType = AutoButtonIndication.KeyIndicator.Home;
			home.alignment = AutoButtonIndication.Alignment.Balance;
			home.distanceBetweenButtonAndText = 0.035f;
			home.distanceBetweenButtonAndLine = 0.0f;
			home.lineLengthAdjustment = 0.0f;
			home.useMultiLanguage = true;
			home.indicationText = "system";

			autoButtonIndicationList.Add(home);

			AutoButtonIndication app = new AutoButtonIndication();
			app.keyType = AutoButtonIndication.KeyIndicator.App;
			app.alignment = AutoButtonIndication.Alignment.Balance;
			app.distanceBetweenButtonAndText = 0.035f;
			app.distanceBetweenButtonAndLine = 0.0f;
			app.lineLengthAdjustment = 0.0f;
			app.useMultiLanguage = true;
			app.indicationText = "system";

			autoButtonIndicationList.Add(app);

			AutoButtonIndication grip = new AutoButtonIndication();
			grip.keyType = AutoButtonIndication.KeyIndicator.Grip;
			grip.alignment = AutoButtonIndication.Alignment.Balance;
			grip.distanceBetweenButtonAndText = 0.035f;
			grip.distanceBetweenButtonAndLine = 0.0f;
			grip.lineLengthAdjustment = 0.0f;
			grip.useMultiLanguage = true;
			grip.indicationText = "system";

			autoButtonIndicationList.Add(grip);

			AutoButtonIndication trigger = new AutoButtonIndication();
			trigger.keyType = AutoButtonIndication.KeyIndicator.Trigger;
			trigger.alignment = AutoButtonIndication.Alignment.Balance;
			trigger.distanceBetweenButtonAndText = 0.035f;
			trigger.distanceBetweenButtonAndLine = 0.0f;
			trigger.lineLengthAdjustment = 0.0f;
			trigger.useMultiLanguage = true;
			trigger.indicationText = "system";

			autoButtonIndicationList.Add(trigger);

			AutoButtonIndication dt = new AutoButtonIndication();
			dt.keyType = AutoButtonIndication.KeyIndicator.DigitalTrigger;
			dt.alignment = AutoButtonIndication.Alignment.Balance;
			dt.distanceBetweenButtonAndText = 0.035f;
			dt.distanceBetweenButtonAndLine = 0.0f;
			dt.lineLengthAdjustment = 0.0f;
			dt.useMultiLanguage = true;
			dt.indicationText = "system";

			autoButtonIndicationList.Add(dt);

			AutoButtonIndication bumper = new AutoButtonIndication();
			bumper.keyType = AutoButtonIndication.KeyIndicator.Bumper;
			bumper.alignment = AutoButtonIndication.Alignment.Balance;
			bumper.distanceBetweenButtonAndText = 0.035f;
			bumper.distanceBetweenButtonAndLine = 0.0f;
			bumper.lineLengthAdjustment = 0.0f;
			bumper.useMultiLanguage = true;
			bumper.indicationText = "system";

			autoButtonIndicationList.Add(bumper);

			AutoButtonIndication touchpad = new AutoButtonIndication();
			touchpad.keyType = AutoButtonIndication.KeyIndicator.TouchPad;
			touchpad.alignment = AutoButtonIndication.Alignment.Balance;
			touchpad.distanceBetweenButtonAndText = 0.035f;
			touchpad.distanceBetweenButtonAndLine = 0.0f;
			touchpad.lineLengthAdjustment = 0.0f;
			touchpad.useMultiLanguage = true;
			touchpad.indicationText = "system";

			autoButtonIndicationList.Add(touchpad);

			AutoButtonIndication vol = new AutoButtonIndication();
			vol.keyType = AutoButtonIndication.KeyIndicator.Volume;
			vol.alignment = AutoButtonIndication.Alignment.Balance;
			vol.distanceBetweenButtonAndText = 0.035f;
			vol.distanceBetweenButtonAndLine = 0.0f;
			vol.lineLengthAdjustment = 0.0f;
			vol.useMultiLanguage = true;
			vol.indicationText = "system";

			autoButtonIndicationList.Add(vol);

			AutoButtonIndication buttonA = new AutoButtonIndication();
			buttonA.keyType = AutoButtonIndication.KeyIndicator.ButtonA;
			buttonA.alignment = AutoButtonIndication.Alignment.Balance;
			buttonA.distanceBetweenButtonAndText = 0.035f;
			buttonA.distanceBetweenButtonAndLine = 0.0f;
			buttonA.lineLengthAdjustment = 0.0f;
			buttonA.useMultiLanguage = true;
			buttonA.indicationText = "system";

			autoButtonIndicationList.Add(buttonA);

			AutoButtonIndication buttonB = new AutoButtonIndication();
			buttonB.keyType = AutoButtonIndication.KeyIndicator.ButtonB;
			buttonB.alignment = AutoButtonIndication.Alignment.Balance;
			buttonB.distanceBetweenButtonAndText = 0.035f;
			buttonB.distanceBetweenButtonAndLine = 0.0f;
			buttonB.lineLengthAdjustment = 0.0f;
			buttonB.useMultiLanguage = true;
			buttonB.indicationText = "system";

			autoButtonIndicationList.Add(buttonB);

			AutoButtonIndication buttonX = new AutoButtonIndication();
			buttonX.keyType = AutoButtonIndication.KeyIndicator.ButtonX;
			buttonX.alignment = AutoButtonIndication.Alignment.Balance;
			buttonX.distanceBetweenButtonAndText = 0.035f;
			buttonX.distanceBetweenButtonAndLine = 0.0f;
			buttonX.lineLengthAdjustment = 0.0f;
			buttonX.useMultiLanguage = true;
			buttonX.indicationText = "system";

			autoButtonIndicationList.Add(buttonX);

			AutoButtonIndication buttonY = new AutoButtonIndication();
			buttonY.keyType = AutoButtonIndication.KeyIndicator.ButtonY;
			buttonY.alignment = AutoButtonIndication.Alignment.Balance;
			buttonY.distanceBetweenButtonAndText = 0.035f;
			buttonY.distanceBetweenButtonAndLine = 0.0f;
			buttonY.lineLengthAdjustment = 0.0f;
			buttonY.useMultiLanguage = true;
			buttonY.indicationText = "system";

			autoButtonIndicationList.Add(buttonY);
		}
	}
}
