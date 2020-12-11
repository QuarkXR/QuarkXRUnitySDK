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
using UnityEngine.EventSystems;
using Wave.Native;

namespace Wave.Essence.InputModule
{
	/// <summary>
	/// To draw a ring pointer to indicate the space on which is gazed.
	/// </summary>
	[DisallowMultipleComponent]
	[RequireComponent(typeof(MeshRenderer), typeof(MeshFilter), typeof(PhysicsRaycaster))]
	sealed class GazePointer : MonoBehaviour
	{
		const string LOG_TAG = "Wave.Essence.InputModule.GazePointer";
		private void DEBUG(string msg)
		{
			if (Log.EnableDebugLog)
				Log.d(LOG_TAG, msg, true);
		}

		#region Customized Settings
		// ----------- Width of ring -----------
		const float kRingWidthDefault = 0.005f;
		const float kRingWidthMinimal = 0.001f;
		[Tooltip("Set the width of the pointer's ring.")]
		[SerializeField]
		private float m_PointerRingWidth = kRingWidthDefault;
		public float PointerRingWidth { get { return m_PointerRingWidth; } set { m_PointerRingWidth = value; } }

		// ----------- Radius of inner circle -----------
		const float kInnerCircleRadiusDefault = 0.005f;
		const float kInnerCircleRadiusMinimal = 0.001f;
		[Tooltip("Set the radius of the pointer's inner circle.")]
		[SerializeField]
		private float m_PointerCircleRadius = kInnerCircleRadiusDefault;
		public float PointerCircleRadius { get { return m_PointerCircleRadius; } set { m_PointerCircleRadius = value; } }

		// ----------- Z distance of ring -----------
		const float kPointerDistanceDefault = 1;
		const float kPointerDistanceMinimal = 0.1f;
		[Tooltip("Set the z-coordinate of the pointer.")]
		[SerializeField]
		private float m_PointerDistance = kPointerDistanceDefault;
		public float PointerDistance { get { return m_PointerDistance; } set { m_PointerDistance = value; } }

		/// The offset from the pointer to the pointer-mounted object.
		private Vector3 pointerOffset = Vector3.zero;
		/// The offset from the pointer to the pointer-mounted object in every frame.
		private Vector3 pointerFrameOffset = Vector3.zero;
		/// The pointer distance in every frame.
		private float pointerFrameDistance = kPointerDistanceDefault;
		/// The pointer world position.
		private Vector3 pointerWorldPosition = Vector3.zero;

		// ----------- Color of ring -----------
		/// Color of ring background.
		[Tooltip("Set the ring background color.")]
		[SerializeField]
		private Color m_PointerColor = Color.white;
		public Color PointerColor { get { return m_PointerColor; } set { m_PointerColor = value; } }

		/// Color of ring foreground
		[Tooltip("Set the ring foreground progess color.")]
		[SerializeField]
		private Color m_ProgressColor = new Color(0, 245, 255);
		public Color ProgressColor { get { return m_ProgressColor; } set { m_ProgressColor = value; } }

		// ----------- Material and Mesh -----------
		private Mesh m_Mesh = null;

		const string kPointerMaterial = "Materials/GazePointer01";
		[Tooltip("Empty for using the default material or set a customized material.")]
		[SerializeField]
		private Material m_PointerMaterial = null;
		public Material PointerMaterial { get { return m_PointerMaterial; } set { m_PointerMaterial = value; } }
		private Material pointerMaterialInstance = null;

		private MeshFilter m_MeshFilter = null;
		private MeshRenderer m_MeshRenderer = null;

		const int kMaterialRenderQueueMin = 1000;
		const int kMaterialRenderQueueMax = 5000;
		/// The material's renderQueue.
		[Tooltip("Set the Material's renderQueue.")]
		[SerializeField]
		private int m_PointerRenderQueue = kMaterialRenderQueueMax;
		public int PointerRenderQueue { get { return m_PointerRenderQueue; } set { m_PointerRenderQueue = value; } }

		/// The MeshRenderer's sortingOrder.
		[Tooltip("Set the MeshRenderer's sortingOrder.")]
		[SerializeField]
		private int m_PointerSortingOrder = 32767;
		public int PointerSortingOrder { get { return m_PointerSortingOrder; } set { m_PointerSortingOrder = value; } }

		private bool m_ShowPointer = true;
		public bool ShowPointer { get { return m_ShowPointer; } set { m_ShowPointer = value; } }

		private int m_RingPercent = 0;
		public int RingPercent { get { return m_RingPercent; } set { m_RingPercent = value; } }

		private bool ValidateParameters()
		{
			if (m_MeshFilter == null || m_MeshRenderer == null)
				return false;

			if (m_PointerRingWidth < kRingWidthMinimal)
				m_PointerRingWidth = kRingWidthDefault;

			if (m_PointerCircleRadius < kInnerCircleRadiusMinimal)
				m_PointerCircleRadius = kInnerCircleRadiusDefault;

			if (m_PointerDistance < kPointerDistanceMinimal)
				m_PointerDistance = kPointerDistanceDefault;

			if (m_PointerRenderQueue < kMaterialRenderQueueMin ||
				m_PointerRenderQueue > kMaterialRenderQueueMax)
				m_PointerRenderQueue = kMaterialRenderQueueMax;

			return true;
		}
		#endregion

		private bool isHovering = false;

		#region MonoBehaviour overrides
		private bool mEnabled = false;
		void OnEnable()
		{
			if (!mEnabled)
			{
				DEBUG("OnEnable()");
				// 1. Texture or Mesh < Material < < MeshFilter < MeshRenderer, we don't use the texture.
				if (m_Mesh == null)
					m_Mesh = new Mesh();
				if (m_Mesh != null)
					m_Mesh.name = gameObject.name + " Mesh";

				// 2. Load the Material RingUnlitTransparentMat.
				if (m_PointerMaterial == null)
					m_PointerMaterial = Resources.Load(kPointerMaterial) as Material;
				if (m_PointerMaterial != null)
				{
					pointerMaterialInstance = Instantiate(m_PointerMaterial);
					DEBUG("OnEnable() Loaded resource " + pointerMaterialInstance.name);
				}

				// 3. Get the MeshFilter.
				m_MeshFilter = GetComponent<MeshFilter>();

				// 4. Get the MeshRenderer.
				m_MeshRenderer = GetComponent<MeshRenderer>();
				m_MeshRenderer.sortingOrder = m_PointerSortingOrder;
				m_MeshRenderer.material = pointerMaterialInstance;
				m_MeshRenderer.material.renderQueue = PointerRenderQueue;

				// 5. Create the pointer.
				SetPointerActive(m_ShowPointer);

				mEnabled = true;
			}
		}

		void OnDisable()
		{
			if (mEnabled)
			{
				Mesh mesh = m_MeshFilter.mesh;
				mesh.Clear();
				Destroy(pointerMaterialInstance);

				mEnabled = false;
				DEBUG("OnDisable()");
			}
		}

		void Update()
		{
			if (!ValidateParameters())
				return;

			if (m_MeshRenderer.enabled != m_ShowPointer)
				SetPointerActive(m_ShowPointer);

			if (Log.gpl.Print)
				DEBUG("Update() Pointer is " + (m_MeshRenderer.enabled ? "shown." : "hidden."));

			// Do nothing if pointer disabled.
			if (!m_MeshRenderer.enabled)
				return;

			pointerFrameOffset = pointerOffset;
			pointerFrameDistance = pointerFrameOffset.z;

			if (pointerFrameOffset == Vector3.zero)
				pointerFrameDistance = m_PointerDistance;
			pointerFrameDistance = pointerFrameDistance < kPointerDistanceMinimal ? m_PointerDistance : pointerFrameDistance;

			pointerFrameOffset.z = pointerFrameDistance;

			float calcRingWidth = m_PointerRingWidth * (1 + ((pointerFrameDistance / kPointerDistanceDefault) * 0.1f));
			float calcInnerCircleRadius = m_PointerCircleRadius * (1 + ((pointerFrameDistance / kPointerDistanceDefault) * 0.1f));

			if (pointerOffset != Vector3.zero)
				pointerWorldPosition = transform.position + pointerFrameOffset;
			else
			{
				pointerWorldPosition = transform.position + transform.forward.normalized * pointerFrameDistance;
			}

			DrawRingRoll(calcRingWidth + calcInnerCircleRadius, calcInnerCircleRadius, pointerFrameOffset, m_RingPercent, isHovering);
		}
		#endregion

		private void SetPointerActive(bool active)
		{
			if (m_MeshRenderer.enabled != active)
			{
				m_MeshRenderer.enabled = active;
				DEBUG("SetPointerActive() " + m_MeshRenderer.enabled);
				if (m_MeshRenderer.enabled)
				{
					m_MeshRenderer.sortingOrder = m_PointerSortingOrder;
					if (pointerMaterialInstance != null)
					{
						m_MeshRenderer.material = pointerMaterialInstance;
						m_MeshRenderer.material.renderQueue = PointerRenderQueue;
					}
					// The MeshFilter's mesh is updated in DrawRingRoll(), not here.
				}
			}
		}

		const int kRingVertexCount = 400;       // 100 percents * 2 + 2, ex: 80% ring -> 80 * 2 + 2
		private Vector3[] ringVert = new Vector3[kRingVertexCount];
		private Color[] ringColor = new Color[kRingVertexCount];

		const int kRingTriangleCount = 100 * 6; // 100 percents * 6, ex: 80% ring -> 80 * 6
		private int[] ringTriangle = new int[kRingTriangleCount];
		private Vector2[] ringUv = new Vector2[kRingVertexCount];

		const float kPercentAngle = 3.6f;    // 100% = 100 * 3.6f = 360 degrees.

		private void DrawRingRoll(float radius, float innerRadius, Vector3 offset, int percent, bool active)
		{
			// vertices and colors
			float start_angle = 90;             // Start angle of drawing ring.
			for (int i = 0; i < kRingVertexCount; i += 2)
			{
				float radian_cur = start_angle * Mathf.Deg2Rad;
				float cosA = Mathf.Cos(radian_cur);
				float sinA = Mathf.Sin(radian_cur);

				ringVert[i].x = offset.x + radius * cosA;
				ringVert[i].y = offset.y + radius * sinA;
				ringVert[i].z = offset.z;

				ringColor[i] = (i <= (percent * 2) && i > 0) ? m_ProgressColor : m_PointerColor;

				ringVert[i + 1].x = offset.x + innerRadius * cosA;
				ringVert[i + 1].y = offset.y + innerRadius * sinA;
				ringVert[i + 1].z = offset.z;

				ringColor[i + 1] = (i <= (percent * 2) && i > 0) ? m_ProgressColor : m_PointerColor;

				start_angle -= kPercentAngle;
			}

			// triangles
			for (int i = 0, vi = 0; i < kRingTriangleCount; i += 6, vi += 2)
			{
				ringTriangle[i] = vi;
				ringTriangle[i + 1] = vi + 3;
				ringTriangle[i + 2] = vi + 1;

				ringTriangle[i + 3] = vi + 2;
				ringTriangle[i + 4] = vi + 3;
				ringTriangle[i + 5] = vi;
			}

			// uv
			for (int i = 0; i < kRingVertexCount; i++)
			{
				ringUv[i].x = ringVert[i].x / radius / 2 + 0.5f;
				ringUv[i].y = ringVert[i].z / radius / 2 + 0.5f;
			}

			m_Mesh.Clear();

			m_Mesh.vertices = ringVert;
			m_Mesh.colors = ringColor;
			m_Mesh.triangles = ringTriangle;
			m_Mesh.uv = ringUv;
			m_MeshFilter.mesh = m_Mesh;
		}

		#region External Functions
		public Vector3 GetPointerPosition()
		{
			return pointerWorldPosition;
		}

		public void OnHover(bool hovering, Vector3 intersecPosition)
		{
			pointerOffset = intersecPosition;
			OnHover(hovering);
		}

		public void OnHover(bool hovering)
		{
			isHovering = hovering;
		}
		#endregion
	}
}
