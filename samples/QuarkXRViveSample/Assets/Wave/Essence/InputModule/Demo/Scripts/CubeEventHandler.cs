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
using System.Collections;
using UnityEngine.EventSystems;
using Wave.Native;

namespace Wave.Essence.InputModule.Demo
{
	[DisallowMultipleComponent]
	public class CubeEventHandler : MonoBehaviour,
		IPointerEnterHandler,
		IPointerExitHandler,
		IPointerDownHandler,
		IBeginDragHandler,
		IDragHandler,
		IEndDragHandler,
		IDropHandler,
		IPointerUpHandler,
		IPointerHoverHandler,
		IPointerClickHandler
	{
		const string LOG_TAG = "Wave.Essence.InputModule.Demo.CubeEventHandler";
		private void DEBUG(string msg)
		{
			if (Log.EnableDebugLog)
				Log.d(LOG_TAG, msg, true);
		}

		private WaitForEndOfFrame waitForEndOfFrame = new WaitForEndOfFrame();
		private Vector3 goPosition;
		private float goPositionZ;

		private void TeleportRandomly()
		{
			Vector3 direction = Random.onUnitSphere;
			direction.y = Mathf.Clamp(direction.y, 0.5f, 1f);
			direction.z = Mathf.Clamp(direction.z, 3f, 10f);
			float distance = 2 * UnityEngine.Random.value + 1.5f;
			transform.localPosition = direction * distance;
		}

		private void Rotate()
		{
			transform.Rotate(72 * (10 * Time.deltaTime), 0, 0);
			transform.Rotate(0, 72 * (10 * Time.deltaTime), 0);
		}

		#region Override event handling function
		public void OnPointerEnter(PointerEventData eventData)
		{
			DEBUG("OnPointerEnter, camera: " + eventData.enterEventCamera);
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			// Do nothing
		}

		public void OnPointerDown(PointerEventData eventData)
		{
			DEBUG("OnPointerDown()");
		}

		public void OnPointerUp(PointerEventData eventData)
		{
			DEBUG("OnPointerUp()");
		}

		public void OnBeginDrag(PointerEventData eventData)
		{
			goPosition = transform.position;
			goPositionZ = transform.position.z;

			DEBUG("OnBeginDrag() position: " + goPosition);

			StartCoroutine("TrackPointer");
		}

		public void OnDrag(PointerEventData eventData)
		{
			Camera _cam = eventData.enterEventCamera;
			if (_cam != null)
				goPosition = _cam.ScreenToWorldPoint(new Vector3(eventData.position.x, eventData.position.y, goPositionZ));
		}

		// Called when the pointer exits our GUI component.
		// Stop tracking the mouse
		public void OnEndDrag(PointerEventData eventData)
		{
			DEBUG("OnEndDrag() position: " + goPosition);

			StopCoroutine("TrackPointer");
		}

		public void OnDrop(PointerEventData eventData)
		{
			goPosition = eventData.enterEventCamera.ScreenToWorldPoint(new Vector3(eventData.position.x, eventData.position.y, goPositionZ));

			DEBUG("OnDrop() position: " + goPosition);
		}

		public void OnHover(PointerEventData eventData)
		{
			transform.Rotate(0, 12 * (10 * Time.deltaTime), 0);
		}

		public void OnPointerClick(PointerEventData eventData)
		{
			goPosition = eventData.enterEventCamera.ScreenToWorldPoint(new Vector3(eventData.position.x, eventData.position.y, goPositionZ));

			DEBUG("OnPointerClick() position: " + goPosition);

			StopCoroutine("TrackPointer");
			TeleportRandomly();
		}
		#endregion

		IEnumerator TrackPointer()
		{
			while (true)
			{
				yield return waitForEndOfFrame;

				transform.position = goPosition;
			}
		}

		public void OnShowCube()
		{
			Log.d(LOG_TAG, "OnShowButton");
			transform.gameObject.SetActive(true);
		}

		public void OnHideCube()
		{
			Log.d(LOG_TAG, "OnHideButton");
			transform.gameObject.SetActive(false);
		}

		public void OnTrigger()
		{
			TeleportRandomly();
		}
	}
}
