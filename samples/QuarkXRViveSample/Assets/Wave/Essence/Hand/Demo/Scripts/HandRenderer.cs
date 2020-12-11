using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wave.Essence.Hand.Demo
{
	public enum HandColliderType { None, Trigger, Collider }

	[DisallowMultipleComponent]
	[HelpURL("https://hub.vive.com/storage/tracking/unity/usage.html#draw-detected-hands-as-skeletons")]
	class HandRenderer : MonoBehaviour
	{
		private void DEBUG(string msg)
		{
			Debug.Log("HandRenderer: " + msg);
		}
		private const float minAlpha = 0.2f;
		// color look-up for different gestures
		private static Color32[] gesture_colors = new Color32[] {
			new Color32(20, 20, 20, 255), new Color32(255, 255, 255, 255), new Color32(91, 44, 111, 255),
			new Color32(0, 255, 255, 255), new Color32(255, 20, 147, 255), new Color32(255, 215, 0, 255),
			new Color32(255, 128, 64, 255),
		};

		private int GetGestureIndex()
		{
			ulong gesture_value = this.IsLeft ?
				HandManager.Instance.GetHandGestureLeft() :
				HandManager.Instance.GetHandGestureRight();

			if ((gesture_value & ((ulong)HandManager.StaticGestures.UNKNOWN)) != 0)
				return 1;
			if ((gesture_value & ((ulong)HandManager.StaticGestures.FIST)) != 0)
				return 2;
			if ((gesture_value & ((ulong)HandManager.StaticGestures.FIVE)) != 0)
				return 3;
			if ((gesture_value & ((ulong)HandManager.StaticGestures.OK)) != 0)
				return 4;
			if ((gesture_value & ((ulong)HandManager.StaticGestures.THUMBUP)) != 0)
				return 5;
			if ((gesture_value & ((ulong)HandManager.StaticGestures.INDEXUP)) != 0)
				return 6;

			return 0;
		}

		// Links between keypoints, 2*i & 2*i+1 forms a link.
		// keypoint index: 0: palm, 1-4: thumb, 5-8: index, 9-12: middle, 13-16: ring, 17-20: pinky
		// fingers are counted from bottom to top
		private static int[] Connections = new int[] {
			0, 1, 0, 5, 0, 9, 0, 13, 0, 17, // palm and finger starts
			2, 5, 5, 9, 9, 13, 13, 17, // finger starts
			1, 2, 2, 3, 3, 4, // thumb
			5, 6, 6, 7, 7, 8, // index
			9, 10, 10, 11, 11, 12, // middle
			13, 14, 14, 15, 15, 16, // ring
			17, 18, 18, 19, 19, 20, // pinky
		};

		[Tooltip("Draw left hand if true, right hand otherwise")]
		public bool IsLeft = false;
		[Tooltip("Default color of hand points")]
		public Color pointColor = Color.blue;
		[Tooltip("Default color of links between keypoints in skeleton mode")]
		public Color linkColor = Color.white;
		[Tooltip("Show gesture color on points (2D/3D mode) or links (skeleton mode)")]
		public bool showGestureColor = false;
		[Tooltip("Use hand confidence as alpha, low confidence hand becomes transparent")]
		public bool showConficenceAsAlpha = true;
		[Tooltip("Material for hand points and links")]
		[SerializeField]
		private Material material = null;
		[Tooltip("Collider type created with hand. The layer of the object is same as this object.")]
		public HandColliderType colliderType = HandColliderType.None;

		// list of points created (1 for 3D/2D point, 21 for skeleton)
		private List<GameObject> points = new List<GameObject>();
		// list of links created (only for skeleton)
		private List<GameObject> links = new List<GameObject>();
		// trigger collider object, only used in skeleton mode
		private GameObject colliderObject = null;
		// shared material for all point objects
		private Material pointMat = null;
		// shared material for all link objects
		private Material linkMat = null;

		IEnumerator Start()
		{
			// wait until detection is started, so we know what mode we are using
			while (HandManager.Instance == null || HandManager.Instance.GetHandGestureStatus() != HandManager.HandGestureStatus.AVAILABLE)
				yield return null;

			DEBUG("Start()");
			pointMat = new Material(material);
			pointMat.color = pointColor;
			if (true/*GestureProvider.HaveSkeleton*/)
			{
				linkMat = new Material(material);
				linkMat.color = linkColor;
			}

			// create game objects for points, number of points is determined by mode
			int count = true/*GestureProvider.HaveSkeleton*/ ? 21 : 1;
			for (int i = 0; i < count; i++)
			{
				var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
				go.name = "point" + i;
				go.transform.parent = transform;
				go.transform.localScale = Vector3.one * 0.012f;
				go.SetActive(false);
				points.Add(go);

				// handle layer
				go.layer = gameObject.layer;

				// handle material
				go.GetComponent<Renderer>().sharedMaterial = pointMat;

				// handle collider, GameObject.CreatePrimitive returns object with a non-trigger collider
				if (colliderType != HandColliderType.Collider)
				{
					var collider = go.GetComponent<Collider>();
					// for trigger collider in skeleton mode, we create an extra game object with one collider
					if (false/*!GestureProvider.HaveSkeleton*/ && colliderType == HandColliderType.Trigger)
						collider.isTrigger = true;
					else
						GameObject.Destroy(collider);
				}
			}

			// create game objects for links between keypoints, only used in skeleton mode
			if (true/*GestureProvider.HaveSkeleton*/)
			{
				for (int i = 0; i < Connections.Length; i += 2)
				{
					var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
					go.name = "link" + i;
					go.transform.parent = transform;
					go.transform.localScale = Vector3.one * 0.005f;
					go.SetActive(false);
					links.Add(go);

					// handle layer
					go.layer = gameObject.layer;

					// handle material
					go.GetComponent<Renderer>().sharedMaterial = linkMat;

					// handle collider
					if (colliderType != HandColliderType.Collider)
						GameObject.Destroy(go.GetComponent<Collider>());
				}
			}

			// create a large trigger collider for skeleton
			if (colliderType == HandColliderType.Trigger && true/*GestureProvider.HaveSkeleton*/)
			{
				colliderObject = new GameObject("Collider");
				colliderObject.transform.parent = transform;
				colliderObject.layer = gameObject.layer;
				var collider = colliderObject.AddComponent<BoxCollider>();
				collider.isTrigger = true;
				colliderObject.SetActive(false);
			}
		}

		void Update()
		{
			// hide points and links if no hand is detected
			//var hand = IsLeft ? GestureProvider.LeftHand : GestureProvider.RightHand;
			bool valid_pose = IBonePose.Instance.IsHandPoseValid((this.IsLeft ? HandManager.HandType.LEFT : HandManager.HandType.RIGHT));
			if (!valid_pose/*hand == null*/)
			{
				foreach (var p in points)
					p.SetActive(false);
				foreach (var l in links)
					l.SetActive(false);
				if (colliderObject != null)
					colliderObject.SetActive(false);
				return;
			}

			// update base position for collision detection
			transform.position = IBonePose.Instance.GetBoneTransform(0, this.IsLeft).pos;// hand.position;
			transform.rotation = IBonePose.Instance.GetBoneTransform(0, this.IsLeft).rot;// hand.rotation;

			// update gesture color on points for non skeleton mode
			if (showGestureColor)
			{
				if (true/*GestureProvider.HaveSkeleton*/)
					linkMat.color = gesture_colors[GetGestureIndex()/*(int)hand.gesture*/];
				//else
				//pointMat.color = gesture_colors[GetGestureIndex()/*(int)hand.gesture*/];
			}
			// update alpha
			if (showConficenceAsAlpha)
			{
				var color = this.pointColor;
				if (pointMat != null)
					color = pointMat.color;
				float hand_confidence = IBonePose.Instance.GetHandConfidence((this.IsLeft ? HandManager.HandType.LEFT : HandManager.HandType.RIGHT));
				color.a = hand_confidence/*hand.confidence*/ > minAlpha ? hand_confidence/*hand.confidence*/ : minAlpha;
				if (pointMat != null)
					pointMat.color = color;
				if (linkMat != null/*GestureProvider.HaveSkeleton*/)
				{
					color = linkMat.color;
					color.a = hand_confidence/*hand.confidence*/ > minAlpha ? hand_confidence/*hand.confidence*/ : minAlpha;
					linkMat.color = color;
				}
			}

			// update points and links position
			for (int i = 0; i < points.Count; i++)
			{
				var go = points[i];
				go.transform.position = IBonePose.Instance.GetBoneTransform(i, this.IsLeft).pos;// hand.points[i];
				go.SetActive(IsValidGesturePoint(go.transform.position)/*go.transform.position.IsValidGesturePoint()*/);
			}

			for (int i = 0; i < links.Count; i++)
			{
				var link = links[i];
				link.SetActive(false);

				int startIndex = Connections[i * 2];
				var pose1 = IBonePose.Instance.GetBoneTransform(startIndex, this.IsLeft).pos;// hand.points[startIndex];
				if (!IsValidGesturePoint(pose1)/*pose1.IsValidGesturePoint()*/)
					continue;

				var pose2 = IBonePose.Instance.GetBoneTransform(Connections[i * 2 + 1], this.IsLeft).pos;// hand.points[Connections[i * 2 + 1]];
				if (!IsValidGesturePoint(pose2)/*pose2.IsValidGesturePoint()*/)
					continue;

				// calculate link position and rotation based on points on both end
				link.SetActive(true);
				link.transform.position = (pose1 + pose2) / 2;
				var direction = pose2 - pose1;
				link.transform.rotation = Quaternion.FromToRotation(Vector3.up, direction);
				link.transform.localScale = new Vector3(0.006f, direction.magnitude / 2f - 0.0051f, 0.006f);
			}

			if (colliderObject == null)
				return;

			// update trigger collider bounds in skeleton mode
			var bounds = new Bounds(transform.position, Vector3.zero);
			foreach (var renderer in transform.GetComponentsInChildren<Renderer>())
				bounds.Encapsulate(renderer.bounds);
			colliderObject.transform.position = bounds.center;
			colliderObject.transform.rotation = Quaternion.identity;
			colliderObject.transform.localScale = bounds.size;
			colliderObject.SetActive(true);
		}

		private static bool IsValidGesturePoint(Vector3 point)
		{
			return point.x != 0 || point.y != 0 || point.z != 0;
		}
	}
}
