using Wave.XR.Function;

namespace Wave.XR.Profiler
{
	public class RenderDoc
	{
		static int functionChecked = 0;

		// frameCount is at least 1 frame.  Max is 10 frame
		public delegate void CaptureFramesDelegate(uint frameCount);
		private static CaptureFramesDelegate captureFrames = null;
		public static CaptureFramesDelegate CaptureFrames
		{
			get
			{
				if (captureFrames == null && (functionChecked & 1) == 0)
				{
					captureFrames = FunctionsHelper.GetFuncPtr<CaptureFramesDelegate>("RenderDocCaptureFrames");
					functionChecked |= 1;
				}
				return captureFrames;
			}
		}

		// frameGap is the interval frame count between capture.
		public delegate void SetAutoCaptureDelegate(bool enable, uint frameGap, uint frameCount);
		private static SetAutoCaptureDelegate setAutoCapture = null;
		public static SetAutoCaptureDelegate SetAutoCapture
		{
			get
			{
				if (setAutoCapture == null && (functionChecked & 2) == 0)
				{
					setAutoCapture = FunctionsHelper.GetFuncPtr<SetAutoCaptureDelegate>("RenderDocConfigAutoCapture");
					functionChecked |= 2;
				}
				return setAutoCapture;
			}
		}

		public delegate bool IsAvailableDelegate();
		private static IsAvailableDelegate isAvailable = null;
		public static IsAvailableDelegate IsAvailable
		{
			get
			{
				if (isAvailable == null && (functionChecked & 4) == 0)
					isAvailable = FunctionsHelper.GetFuncPtr<IsAvailableDelegate>("RenderDocAvailable");
				return isAvailable;
			}
		}

#if EXAMPLE
		public void Example()
		{
			if (IsAvailable != null && IsAvailable())
			{
				SetAutoCapture?.Invoke(false, 300, 1);
			}

			if (IsAvailable != null && IsAvailable())
			{
				CaptureFrames?.Invoke(1);
			}
		}
#endif
	}
}
