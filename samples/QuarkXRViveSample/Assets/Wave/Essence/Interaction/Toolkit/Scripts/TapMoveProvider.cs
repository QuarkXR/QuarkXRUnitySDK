using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

namespace Wave.Essence.Interaction.Toolkit
{
	[System.Obsolete("DO NOT USE!!")]
	/// <summary>
	/// The tap move provider is a locomotion provider that allows the user to move rig using a specified 2d axis input.
	/// the provider can take input from two different devices (eg: L & R hands). 
	/// </summary>
	public class TapMoveProvider : LocomotionProvider
	{
		/// <summary>
		/// This is the list of possible valid "InputAxes" that we allow users to read from.
		/// </summary>
		public enum InputAxes
		{
			Primary2DAxis = 0,
			Secondary2DAxis = 1,
		};

		// Mapping of the above InputAxes to actual common usage values
		static readonly InputFeatureUsage<Vector2>[] m_Vec2UsageList = new InputFeatureUsage<Vector2>[] {
			CommonUsages.primary2DAxis,
			CommonUsages.secondary2DAxis,
		};

		[SerializeField]
		[Tooltip("The 2D Input Axis on the primary devices that will be used to trigger a snap turn.")]
		InputAxes m_MoveUsage = InputAxes.Primary2DAxis;
		/// <summary>
		/// The 2D Input Axis on the primary device that will be used to trigger a movement.
		/// </summary>
		public InputAxes moveUsage { get { return m_MoveUsage; } set { m_MoveUsage = value; } }

		[SerializeField]
		[Tooltip("A list of controllers that allow Snap Turn.  If an XRController is not enabled, or does not have input actions enabled.  Snap Turn will not work.")]
		List<XRController> m_Controllers = new List<XRController>();
		/// <summary>
		/// The XRControllers that allow SnapTurn.  An XRController must be enabled in order to Snap Turn.
		/// </summary>
		public List<XRController> controllers { get { return m_Controllers; } set { m_Controllers = value; } }

		List<bool> m_ControllersWereActive = new List<bool>();

		[SerializeField]
		[Tooltip("The speed multiplier of tap axis.")]
		[Range(1, 100)]
		uint m_AxisMultiplier = 10;
		/// <summary>
		/// When tapping on MoveUsage key, the move offset will be calculated from the tapping axis multiplying AxisMultiplier.
		/// </summary>
		public uint axisMultiplier { get { return m_AxisMultiplier; } set { m_AxisMultiplier = value; } }

		private float m_Fps = 0;

		private bool m_Tapped = false;
		private Vector2[] m_TapOffset = new Vector2[m_Vec2UsageList.Length];
		private Vector2 m_CurrentAxis = Vector2.zero;
		private Vector3 m_MoveOffset = Vector3.zero;
		// Distance between wall and player in meter.
		private const float SAFE_DISTANCE = 0.6f;
		private Vector3 m_MovePosition = Vector3.zero;

		void Update()
		{
			if (m_Controllers.Count == 0)
				return;

			m_Fps = 1 / Time.deltaTime;

			EnsureControllerDataListSize();

			for (int i = 0; i < m_Controllers.Count; i++)
			{
				XRController controller = m_Controllers[i];
				if (controller != null)
				{
					if (controller.enableInputActions && m_ControllersWereActive[i])
					{
						if (controller.inputDevice.TryGetFeatureValue(m_Vec2UsageList[(int)m_MoveUsage], out m_CurrentAxis))
						{
							if (ValidateAxis(m_CurrentAxis))
							{
								m_CurrentAxis += m_TapOffset[(int)m_MoveUsage];
								m_CurrentAxis.x /= m_Fps;
								m_CurrentAxis.y /= m_Fps;
								GetTapOffset(m_CurrentAxis, out m_MoveOffset);
								Move(m_MoveOffset);
							}
						}
					}
					else //This adds a 1 frame delay when enabling input actions.
					{
						m_ControllersWereActive[i] = controller.enableInputActions;
					}
				}
			}
		}

		void EnsureControllerDataListSize()
		{
			if (m_Controllers.Count != m_ControllersWereActive.Count)
			{
				while (m_ControllersWereActive.Count < m_Controllers.Count)
				{
					m_ControllersWereActive.Add(false);
				}

				while (m_ControllersWereActive.Count < m_Controllers.Count)
				{
					m_ControllersWereActive.RemoveAt(m_ControllersWereActive.Count - 1);
				}
			}
		}

		private bool ValidateAxis(in Vector2 axis)
		{
			if (axis != Vector2.zero)
			{
				if (m_Tapped)
					return true;
				else
				{
					m_TapOffset[(int)m_MoveUsage] = -axis;
					m_Tapped = true;
				}
			}
			else
			{
				m_Tapped = false;
			}

			return false;
		}

		private Vector3 m_RigRight = Vector3.zero, m_RigForward = Vector3.zero;
		/// <summary> Calculate the move offset from the tap axis. </summary>
		private void GetTapOffset(Vector2 axis, out Vector3 offset)
		{
			var xrRig = system.xrRig.gameObject;
			m_RigRight = xrRig.transform.TransformDirection(Vector3.right);
			m_RigForward = xrRig.transform.TransformDirection(Vector3.forward);

			offset = Vector3.zero;
			offset += m_RigRight * (axis.x * m_AxisMultiplier);   // x
			offset += m_RigForward * (axis.y * m_AxisMultiplier); // z
		}

		/// <summary> Move rig with the offset. </summary>
		protected virtual void Move(Vector3 offset)
		{
			var xrRig = system.xrRig.gameObject;

			// Check if new position is on ground.
			float groundHeight = 0;
			bool onGround = CalculateGroundHeight(xrRig.transform.position + offset, out groundHeight);

			// Check if current position is too close to the wall.
			Vector3 wallPosition = Vector3.zero;
			GetFrontWallPosition(xrRig.transform.position + offset, out wallPosition);
			bool hitFrontWall = Mathf.Abs(xrRig.transform.position.z - wallPosition.z) <= SAFE_DISTANCE;

			if (onGround && !hitFrontWall)
			{
				m_MovePosition += offset;
				m_MovePosition.y = groundHeight;
				xrRig.transform.localPosition = m_MovePosition;
			}
		}

		private RaycastHit downRaycastHit = new RaycastHit();
		/// <summary> Calculates the height of ground. False means not on the ground. </summary>
		private bool CalculateGroundHeight(Vector3 position, out float groundHeight)
		{
			groundHeight = 0;

			if (Physics.Raycast(position, -Vector3.up, out downRaycastHit))
			{
				groundHeight = downRaycastHit.point.y;
				return true;
			}

			return false;
		}

		private RaycastHit forwardRaycastHit = new RaycastHit();
		/// <summary> Calculates the position of wall in front of rig. Zero means no wall. </summary>
		private void GetFrontWallPosition(Vector3 position, out Vector3 wallPosition)
		{
			wallPosition = Vector3.zero;
			if (Physics.Raycast(position, Vector3.forward, out forwardRaycastHit))
				wallPosition = forwardRaycastHit.point;
		}
	}
}
