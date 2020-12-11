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
using UnityEngine.UI;
using Wave.Native;

namespace Wave.Essence.InputModule.Demo
{
	[DisallowMultipleComponent]
	[RequireComponent(typeof(Dropdown))]
	public class DropdownTest : MonoBehaviour
	{
		private static string LOG_TAG = "Wave.Essence.InputModule.Demo.DropdownTest";
		private Dropdown m_Dropdown = null;
		private Text m_Text = null;
		private string[] textStrings = new string[] { "aaa", "bbb", "ccc" };
		private Color m_Color = new Color(26, 7, 253, 255);

		void Start()
		{
			m_Dropdown = GetComponentInChildren<Dropdown>();
			m_Text = GetComponentInChildren<Text>();
			// clear all option item
			m_Dropdown.options.Clear();

			// fill the dropdown menu OptionData
			foreach (string c in textStrings)
			{
				m_Dropdown.options.Add(new Dropdown.OptionData() { text = c });
			}
			// this swith from 1 to 0 is only to refresh the visual menu
			m_Dropdown.value = 1;
			m_Dropdown.value = 0;
		}

		void Update()
		{
			m_Text.text = textStrings[m_Dropdown.value];

			Canvas dropdown_canvas = m_Dropdown.gameObject.GetComponentInChildren<Canvas>();
			Button[] buttons = m_Dropdown.gameObject.GetComponentsInChildren<Button>();
			if (dropdown_canvas != null)
			{
				dropdown_canvas.gameObject.tag = "EventCanvas";
				foreach (Button btn in buttons)
				{
					Log.d(LOG_TAG, "set button " + btn.name + " color.");
					ColorBlock cb = btn.colors;
					cb.normalColor = this.m_Color;
					btn.colors = cb;
				}
			}
		}

		public void ChangeColor()
		{
			Image img = gameObject.GetComponent<Image>();
			img.color = img.color == Color.yellow ? Color.green : Color.yellow;
		}
	}
}
