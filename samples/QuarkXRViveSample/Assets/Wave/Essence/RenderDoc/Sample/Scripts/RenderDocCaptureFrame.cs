using UnityEngine;
using UnityEngine.XR;

namespace Wave.XR.Profiler
{
	public class RenderDocCaptureFrame : MonoBehaviour
	{
		public bool captureByButton = false;
		public XRNode hand = XRNode.RightHand;
		public string triggerButton = "MenuButton";

		[Range(1, 10)]
		public int CaptureFrameCount = 1;

		public bool useAutoCapture = false;

		[Range(150, 4500)]
		public int AutoCaptureFrameInterval = 300;

		private void Start()
		{
			if (RenderDoc.SetAutoCapture == null)
				return;

			if (useAutoCapture)
				RenderDoc.SetAutoCapture(true, (uint)AutoCaptureFrameInterval, (uint)CaptureFrameCount);
			else
				RenderDoc.SetAutoCapture(false, (uint)AutoCaptureFrameInterval, (uint)CaptureFrameCount);
		}

		private void OnEnable()
		{
			if (!RenderDoc.IsAvailable())
				enabled = false;
		}

		bool buttonState = false;
		void Update()
		{
			if (RenderDoc.CaptureFrames == null)
				return;

			if (captureByButton)
			{
				InputDevice device = InputDevices.GetDeviceAtXRNode(hand);
				if (device.TryGetFeatureValue(new InputFeatureUsage<bool>(triggerButton), out bool newButtonState))
				{
					// capture on button down
					if (buttonState && buttonState != newButtonState)
						RenderDoc.CaptureFrames((uint)CaptureFrameCount);
					buttonState = newButtonState;
				}
			}
		}

		void OnValidate()
		{
			if (hand != XRNode.LeftHand && hand != XRNode.RightHand)
				hand = XRNode.RightHand;
		}
	}
}
