// "Wave SDK 
// © 2020 HTC Corporation. All Rights Reserved.
//
// Unless otherwise required by copyright law and practice,
// upon the execution of HTC SDK license agreement,
// HTC grants you access to and use of the Wave SDK(s).
// You shall fully comply with all of HTC’s SDK license agreement terms and
// conditions signed by you and all SDK and API requirements,
// specifications, and documentation provided by HTC to You."

using System.Collections.Generic;
using UnityEngine;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using AOT;
using UnityEngine.Rendering;
using Wave.Native;
using Wave.Essence;

namespace Wave.Essence.CameraTexture
{
	public class CameraTextureManager
	{
		private static string LOG_TAG = "CameraTexture";

		#region syncObject
		public class Message
		{
			public bool isFree = true;
		}

		public class MessagePool
		{
			private readonly List<Message> pool = new List<Message>(2) { };
			private int index = 0;

			public MessagePool() { }

			private int Next(int value)
			{
				if (++value >= pool.Count)
					value = 0;
				return value;
			}

			public T Obtain<T>() where T : Message, new()
			{
				int c = pool.Count;
				int i = index;
				for (int j = 0; j < c; i++, j++)
				{
					if (i >= c)
						i = 0;
					if (pool[i].isFree)
					{
						//Debug.LogError("Obtain idx=" + i);
						index = i;
						return (T)pool[i];
					}
				}
				index = Next(i);
				var newItem = new T()
				{
					isFree = true
				};
				pool.Insert(index, newItem);
				//Debug.LogError("Obtain new one.  Pool.Count=" + pool.Count);
				return newItem;
			}

			public void Lock(Message msg)
			{
				msg.isFree = false;
			}

			public void Release(Message msg)
			{
				msg.isFree = true;
			}
		}

		public class PreAllocatedQueue : MessagePool
		{
			private readonly List<Message> list = new List<Message>(2) { null, null };
			private int queueBegin = 0;
			private int queueEnd = 0;

			public PreAllocatedQueue() : base() { }

			private int Next(int value)
			{
				if (++value >= list.Count)
					value = 0;
				return value;
			}

			public void Enqueue(Message msg)
			{
				Lock(msg);
				queueEnd = Next(queueEnd);

				if (queueEnd == queueBegin)
				{
					list.Insert(queueEnd, msg);
					queueBegin++;
				}
				else
				{
					list[queueEnd] = msg;
				}
			}

			public Message Dequeue()
			{
				queueBegin = Next(queueBegin);
				return list[queueBegin];
			}
		}

		// Run a lambda/delegate code in RenderThread
		public class RenderThreadTask
		{
			// In Windows, Marshal.GetFunctionPointerForDelegate() will cause application hang
			private static IntPtr GetFunctionPointerForDelegate(Delegate del)
			{
#if UNITY_EDITOR && UNITY_ANDROID
				return IntPtr.Zero;
#elif UNITY_ANDROID
			return Marshal.GetFunctionPointerForDelegate(del);
#else
			return IntPtr.Zero;
#endif
			}

			public delegate void RenderEventDelegate(int e);
			private static readonly RenderEventDelegate handle = new RenderEventDelegate(RunSyncObjectInRenderThread);
			private static readonly IntPtr handlePtr = GetFunctionPointerForDelegate(handle);

			public delegate void Receiver(PreAllocatedQueue dataQueue);

			private static List<RenderThreadTask> CommandList = new List<RenderThreadTask>();

			private readonly PreAllocatedQueue queue = new PreAllocatedQueue();
			public PreAllocatedQueue Queue { get { return queue; } }

			private readonly Receiver receiver;
			private readonly int id;

			public RenderThreadTask(Receiver render)
			{
				receiver = render;
				if (receiver == null)
					throw new ArgumentNullException("receiver should not be null");

				CommandList.Add(this);
				id = CommandList.IndexOf(this);
			}

			~RenderThreadTask()
			{
				try { CommandList.RemoveAt(id); } finally { }
			}

			void IssuePluginEvent(IntPtr callback, int eventID)
			{
#if UNITY_EDITOR && UNITY_ANDROID
				if (Application.isEditor)
				{
					receiver(queue);
					return;
				}
#endif

#if UNITY_ANDROID
				GL.IssuePluginEvent(callback, eventID);
				return;
#else
			receiver(queue);
			return;
#endif
			}

			void IssuePluginEvent(CommandBuffer cmdBuf, IntPtr callback, int eventID)
			{
#if UNITY_EDITOR && UNITY_ANDROID
				if (Application.isEditor)
					throw new NotImplementedException("Should not use this function in Windows");
#endif

#if UNITY_ANDROID
				cmdBuf.IssuePluginEvent(callback, eventID);
				return;
#else
			throw new NotImplementedException("Should not use this function in Windows");
#endif
			}

			// Run in GameThread
			public void IssueEvent()
			{
				// Let the render thread run the RunSyncObjectInRenderThread(id)
				IssuePluginEvent(handlePtr, id);
			}

			public void IssueInCommandBuffer(CommandBuffer cmdBuf)
			{
				// Let the render thread run the RunSyncObjectInRenderThread(id)
				IssuePluginEvent(cmdBuf, handlePtr, id);
			}

			// Called by RunSyncObjectInRenderThread()
			private void Receive()
			{
				receiver(queue);
			}

			[MonoPInvokeCallback(typeof(RenderEventDelegate))]
			private static void RunSyncObjectInRenderThread(int id)
			{
				CommandList[id].Receive();
			}
		}
		#endregion

		public class FrameBufferDesc : Message
		{
			public IntPtr textureId;
			public WVR_CameraImageFormat imgFormat;
			public IntPtr frameBuffer;
			public uint bufferSize;
			public uint imgWidth;
			public uint imgHeight;
		}

		RenderThreadTask drawTextureTask = new RenderThreadTask(
			// receiver
			(queue) => {
				lock (queue)
				{
					// Run in RenderThread
					var msg = (FrameBufferDesc)queue.Dequeue();

					IntPtr nativeTexId = msg.textureId;
					uint bufferSize = msg.bufferSize;
					IntPtr framebuffer = msg.frameBuffer;
					uint width = msg.imgWidth;
					uint height = msg.imgHeight;
					WVR_CameraImageFormat imgFormat = msg.imgFormat;

					bool updated = Interop.WVR_DrawTextureWithBuffer(nativeTexId, imgFormat, framebuffer, bufferSize, width, height);
					Log.i(LOG_TAG, "Run in render thread to draw texture: " + nativeTexId + ", result: " + updated);

					UpdateCameraCompletedDelegate?.Invoke(nativeTexId, updated);
					queue.Release(msg);
				}
			}
		);

		RenderThreadTask releaseOpenGLTask = new RenderThreadTask(
			// receiver
			(queue) => {
				lock (queue)
				{
					Log.i(LOG_TAG, "Run in render thread to release");
					Interop.WVR_ReleaseCameraTexture();
				}
			}
		);

		private WVR_CameraInfo_t camerainfo;
		private bool mStarted = false;
		private IntPtr nativeTextureId = IntPtr.Zero;
		private IntPtr mframeBuffer = IntPtr.Zero;
		private IntPtr threadframeBuffer = IntPtr.Zero;
		private bool syncPose = false;
		private WVR_PoseState_t[] mPoseState = new WVR_PoseState_t[1];
		private WVR_PoseOriginModel origin = WVR_PoseOriginModel.WVR_PoseOriginModel_OriginOnGround;
		private Thread mthread;
		private bool toThreadStop = false;
		private bool updateFramebuffer = false;

		public bool isStarted
		{
			get
			{
				return mStarted;
			}
		}

		public delegate void UpdateCameraCompleted(System.IntPtr nativeTextureId, bool result);
		public static event UpdateCameraCompleted UpdateCameraCompletedDelegate = null;

		public delegate void StartCameraCompleted(bool result);
		public static event StartCameraCompleted StartCameraCompletedDelegate = null;

		private static CameraTextureManager mInstance = null;
		private const bool DEBUG = false;

		private void PrintDebugLog(string msg)
		{
			if (DEBUG)
			{
				Log.d(LOG_TAG, msg);
			}
		}

		public static CameraTextureManager instance
		{
			get
			{
				if (mInstance == null)
				{
					mInstance = new CameraTextureManager();
				}

				return mInstance;
			}
		}

		public void startCamera(bool enable)
		{
			if (mStarted) return;
			syncPose = enable;

			if (syncPose)
			{
				mPoseState[0] = new WVR_PoseState_t();
				mStarted = Interop.WVR_StartCamera(ref camerainfo);

				Log.i(LOG_TAG, "startCamera, result = " + mStarted + " format: " + camerainfo.imgFormat + " size: " + camerainfo.size
				+ " width: " + camerainfo.width + " height: " + camerainfo.height);
				PrintDebugLog("allocate frame buffer");
				mframeBuffer = Marshal.AllocHGlobal((int)camerainfo.size);

				//zero out buffer
				for (int i = 0; i < camerainfo.size; i++)
				{
					Marshal.WriteByte(mframeBuffer, i, 0);
				}
				StartCameraCompletedDelegate?.Invoke(mStarted);
			}
			else
			{
				mthread = new Thread(() => CameraThread());
				if (mthread.IsBackground == false)
					mthread.IsBackground = true;
				toThreadStop = false;
				mthread.Start();
			}
		}

		void CameraThread()
		{
			mStarted = Interop.WVR_StartCamera(ref camerainfo);

			Log.i(LOG_TAG, "startCamera, result = " + mStarted + " format: " + camerainfo.imgFormat + " size: " + camerainfo.size
			+ " width: " + camerainfo.width + " height: " + camerainfo.height);

			StartCameraCompletedDelegate?.Invoke(mStarted);
			if (!mStarted)
			{
				Log.i(LOG_TAG, "Camera start failed, camera thread stop.");
				return;
			}

			//Keep call WVR_GetFrameBufferWithPoseState
			Log.i(LOG_TAG, "Start CameraThread, Camera is Started? " + mStarted.ToString() + "CameraThread.ThreadState=" + mthread.ThreadState + "CameraThread.IsBackground=" + mthread.IsBackground);
			PrintDebugLog("allocate frame buffer");

			threadframeBuffer = Marshal.AllocHGlobal((int)camerainfo.size);

			//zero out buffer
			for (int i = 0; i < camerainfo.size; i++)
			{
				Marshal.WriteByte(threadframeBuffer, i, 0);
			}
			updateFramebuffer = false;
			int counter = 0;

			while (!toThreadStop)
			{
				if (threadframeBuffer != IntPtr.Zero)
				{
					updateFramebuffer = Interop.WVR_GetCameraFrameBuffer(threadframeBuffer, camerainfo.size);
					if (!updateFramebuffer)
					{
						counter++;
						if (counter > 100)
						{
							Log.i(LOG_TAG, "get framebuffer failed, break while ");
							break;
						}
						Log.i(LOG_TAG, "counter : " + counter);
					}
					else
					{
						counter = 0;
					}
				} else
				{
					Log.i(LOG_TAG, "threadframeBuffer = null, break while ");
					break;
				}
			}
			updateFramebuffer = false;

			if (threadframeBuffer != IntPtr.Zero)
			{
				Marshal.FreeHGlobal(threadframeBuffer);
				threadframeBuffer = IntPtr.Zero;
			}

			Interop.WVR_StopCamera();
			mStarted = false;

			Log.i(LOG_TAG, "End of CameraThread");
		}

		public WVR_CameraImageType getImageType()
		{
			if (!mStarted) return WVR_CameraImageType.WVR_CameraImageType_Invalid;
			return camerainfo.imgType;
		}

		public WVR_CameraImageFormat getImageFormat()
		{
			if (!mStarted) return WVR_CameraImageFormat.WVR_CameraImageFormat_Invalid;
			return camerainfo.imgFormat;
		}

		public uint getImageWidth()
		{
			if (!mStarted) return 0;
			return camerainfo.width;
		}

		public uint getImageHeight()
		{
			if (!mStarted) return 0;
			return camerainfo.height;
		}

		public uint getImageSize()
		{
			if (!mStarted) return 0;
			return camerainfo.size;
		}

		public bool isEnableSyncPose()
		{
			if (!mStarted) return false;
			return syncPose;
		}

		public void stopCamera()
		{
			if (!mStarted) return;

			if (syncPose)
			{
				RenderFunctions.SetPoseUsedOnSubmit(null);
				Interop.WVR_StopCamera();
				if (mframeBuffer != IntPtr.Zero)
				{
					Marshal.FreeHGlobal(mframeBuffer);
					mframeBuffer = IntPtr.Zero;
				}
				mStarted = false;
			}
			else
			{
				if (mthread != null && mthread.IsAlive)
				{
					toThreadStop = true;
					Log.i(LOG_TAG, "to thread stop");
				}
			}

			Log.i(LOG_TAG, "Release native texture resources");
			releaseNativeResources();
		}

		private void releaseNativeResources()
		{
			releaseOpenGLTask.IssueEvent();
		}

		public bool getFramePose(ref WVR_PoseState_t pose)
		{
			if (!syncPose) return false;
			pose = mPoseState[0];
			return true;
		}

		private void drawTextureInRenderThread()
		{
			if (!mStarted) return;
			var queue = drawTextureTask.Queue;

			lock (queue)
			{
				var msg = queue.Obtain<FrameBufferDesc>();
				msg.textureId = nativeTextureId;
				msg.imgFormat = camerainfo.imgFormat;
				msg.imgHeight = camerainfo.height;
				msg.imgWidth = camerainfo.width;
				msg.bufferSize = camerainfo.size;
				if (syncPose)
				{
					msg.frameBuffer = mframeBuffer;
				}
				else
				{
					msg.frameBuffer = threadframeBuffer;
				}

				queue.Enqueue(msg);
			}

			drawTextureTask.IssueEvent();
		}


		public void updateTexture(IntPtr textureId)
		{
			if (!mStarted)
			{
				Log.w(LOG_TAG, "Camera not start yet");
				return;
			}

			nativeTextureId = textureId;

			ClientInterface.GetOrigin(ref origin);

			if (syncPose)
			{
				if (mframeBuffer != IntPtr.Zero)
				{
					uint predictInMs = 0;
					PrintDebugLog("updateTexture frameBuffer and PoseState, predict time:" + predictInMs);

					Interop.WVR_GetFrameBufferWithPoseState(mframeBuffer, camerainfo.size, origin, predictInMs, ref mPoseState[0]);

					PrintDebugLog("Sync camera frame buffer with poseState, timeStamp: " + mPoseState[0].PoseTimestamp_ns);
					RenderFunctions.SetPoseUsedOnSubmit(mPoseState);
					drawTextureInRenderThread();
				}
			}
			else
			{
				if (updateFramebuffer && (threadframeBuffer != IntPtr.Zero))
				{
					PrintDebugLog("updateFramebuffer camera frame buffer");
					nativeTextureId = textureId;
					drawTextureInRenderThread();
					updateFramebuffer = false;
				}
				else
				{
					// thread frame buffer is not updated and native texture is not updated, send complete delegate back
					UpdateCameraCompletedDelegate?.Invoke(nativeTextureId, false);
				}
			}
		}
	}
}
