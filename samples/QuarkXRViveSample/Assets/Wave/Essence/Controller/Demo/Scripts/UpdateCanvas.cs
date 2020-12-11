using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

namespace Wave.Essence.Samples.WaveController
{
	public class UpdateCanvas : MonoBehaviour
	{
		private static string LOG_TAG = "UpdateCanvas";

		public GameObject rightIsValid = null;
		public GameObject rightName = null;
		public GameObject rightManufacturer = null;
		public GameObject rightSerialNumber = null;
		public GameObject rightPoseTracking = null;

		private Text rightIsValidText = null;
		private Text rightNameText = null;
		private Text rightManufacturerText = null;
		private Text rightSerialNumberText = null;
		private Text rightPoseTrackingText = null;

		public GameObject leftIsValid = null;
		public GameObject leftName = null;
		public GameObject leftManufacturer = null;
		public GameObject leftSerialNumber = null;
		public GameObject leftPoseTracking = null;

		private Text leftIsValidText = null;
		private Text leftNameText = null;
		private Text leftManufacturerText = null;
		private Text leftSerialNumberText = null;
		private Text leftPoseTrackingText = null;

		void OnEnable()
		{
		}

		void OnDisable()
		{
		}

		// Start is called before the first frame update
		void Start()
		{
			if (rightIsValid != null)
			{
				rightIsValidText = rightIsValid.GetComponent<Text>();
			}

			if (rightName != null)
			{
				rightNameText = rightName.GetComponent<Text>();
			}

			if (rightManufacturer != null)
			{
				rightManufacturerText = rightManufacturer.GetComponent<Text>();
			}

			if (rightSerialNumber != null)
			{
				rightSerialNumberText = rightSerialNumber.GetComponent<Text>();
			}

			if (rightPoseTracking != null)
			{
				rightPoseTrackingText = rightPoseTracking.GetComponent<Text>();
			}

			if (leftIsValid != null)
			{
				leftIsValidText = leftIsValid.GetComponent<Text>();
			}

			if (leftName != null)
			{
				leftNameText = leftName.GetComponent<Text>();
			}

			if (leftManufacturer != null)
			{
				leftManufacturerText = leftManufacturer.GetComponent<Text>();
			}

			if (leftSerialNumber != null)
			{
				leftSerialNumberText = leftSerialNumber.GetComponent<Text>();
			}

			if (leftPoseTracking != null)
			{
				leftPoseTrackingText = leftPoseTracking.GetComponent<Text>();
			}
		}

		// Update is called once per frame
		void Update()
		{
			InputDevice rightdevice = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);

			if (rightdevice.isValid)
			{
				if (rightIsValidText != null)
					rightIsValidText.text = "Device is valid";

				if (rightNameText != null)
					rightNameText.text = "Name: " + rightdevice.name;

				if (rightManufacturerText != null)
					rightManufacturerText.text = "Manufacturer: " + rightdevice.manufacturer;

				if (rightSerialNumberText != null)
					rightSerialNumberText.text = "Serial number: " + rightdevice.serialNumber;

				bool validPoseState;

				if (InputDevices.GetDeviceAtXRNode(XRNode.RightHand).TryGetFeatureValue(new InputFeatureUsage<bool>("IsTracked"), out validPoseState)
					&& validPoseState)
				{
					if (rightPoseTrackingText != null)
						rightPoseTrackingText.text = "Pose is updated!";
				} else
				{
					if (rightPoseTrackingText != null)
						rightPoseTrackingText.text = "Pose is not available!";
				}
			} else
			{
				if (rightIsValidText != null)
					rightIsValidText.text = "Device is not valid";

				if (rightNameText != null)
					rightNameText.text = "Device is not valid";

				if (rightManufacturerText != null)
					rightManufacturerText.text = "Device is not valid";

				if (rightSerialNumberText != null)
					rightSerialNumberText.text = "Device is not valid";

				if (rightPoseTrackingText != null)
					rightPoseTrackingText.text = "Device is not valid";
			}

			InputDevice leftdevice = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);

			if (leftdevice.isValid)
			{
				if (leftIsValidText != null)
					leftIsValidText.text = "Device is valid";

				if (leftNameText != null)
					leftNameText.text = "Name: " + leftdevice.name;

				if (leftManufacturerText != null)
					leftManufacturerText.text = "Manufacturer: " + leftdevice.manufacturer;

				if (leftSerialNumberText != null)
					leftSerialNumberText.text = "Serial number: " + leftdevice.serialNumber;

				bool validPoseState;

				if (InputDevices.GetDeviceAtXRNode(XRNode.LeftHand).TryGetFeatureValue(new InputFeatureUsage<bool>("IsTracked"), out validPoseState)
					&& validPoseState)
				{
					if (leftPoseTrackingText != null)
						leftPoseTrackingText.text = "Pose is updated!";
				}
				else
				{
					if (leftPoseTrackingText != null)
						leftPoseTrackingText.text = "Pose is not available!";
				}
			}
			else
			{
				if (leftIsValidText != null)
					leftIsValidText.text = "Device is not valid";

				if (leftNameText != null)
					leftNameText.text = "Device is not valid";

				if (leftManufacturerText != null)
					leftManufacturerText.text = "Device is not valid";

				if (leftSerialNumberText != null)
					leftSerialNumberText.text = "Device is not valid";

				if (leftPoseTrackingText != null)
					leftPoseTrackingText.text = "Device is not valid";
			}
		}
	}
}
