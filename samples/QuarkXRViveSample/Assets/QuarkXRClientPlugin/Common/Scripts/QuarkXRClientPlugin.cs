/************************************************************************************
Copyright : Copyright (c) QuarkXR INC and its affiliates. All rights reserved.
************************************************************************************/

using System;
using System.Runtime.InteropServices;
using System.Collections.Concurrent;
using UnityEngine;

public class ServerEventArgs : EventArgs
{
    public long Id { get; set; }
    public string Name { get; set; }
    public string Host { get; set; }
    public ushort Port { get; set; }
};

public class QuarkXRClientPlugin : MonoBehaviour
{
#if UNITY_ANDROID
    private const string PLUGIN_NAME = "quarkxrunityplugin";
#else
    private const string PLUGIN_NAME = "QuarkXRUnityPlugin";
#endif

    private const string MISSING_PLUGIN_MESSAGE = "Missing instance of QuarkXRClientPlugin.cs, Please attach the script to a GameObject.";

    private delegate void PluginEventHandler(
        IntPtr context,
        [MarshalAs(UnmanagedType.LPStr)] string eventName,
        [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] long[] args,
        uint size
        );

    [DllImport(PLUGIN_NAME)]
    private static extern void QXRPlugin_Log([MarshalAs(UnmanagedType.LPStr)] String msg);

    [DllImport(PLUGIN_NAME)]
    private static extern bool QXRPlugin_Init([MarshalAs(UnmanagedType.LPStr)] String path, IntPtr context, PluginEventHandler callback);

    [DllImport(PLUGIN_NAME)]
    private static extern void QXRPlugin_Done();

    [DllImport(PLUGIN_NAME)]
    private static extern void QXRPlugin_Resume();

    [DllImport(PLUGIN_NAME)]
    private static extern void QXRPlugin_Pause();

    [DllImport(PLUGIN_NAME)]
    private static extern void QXRPlugin_Enable();

    [DllImport(PLUGIN_NAME)]
    private static extern void QXRPlugin_Disable();

#if UNITY_ANDROID
    [DllImport(PLUGIN_NAME)]
    private static extern void QXRPlugin_AndroidInit(IntPtr activity);

    [DllImport(PLUGIN_NAME)]
    private static extern void QXRPlugin_AndroidDone(IntPtr activity);
#endif

    [DllImport(PLUGIN_NAME)]
    private static extern bool QXRPlugin_SetBoolProperty(string property, bool value);

    [DllImport(PLUGIN_NAME)]
    private static extern void QXRPlugin_ConnectToServer(long id);

    [DllImport(PLUGIN_NAME)]
    private static extern void QXRPlugin_DisconnectFromServer();

    [DllImport(PLUGIN_NAME)]
    private static extern IntPtr QXRPlugin_GetRenderFunction();

    [DllImport(PLUGIN_NAME)]
    private static extern void QXRPlugin_SetTexture(IntPtr texture2D);

    [DllImport(PLUGIN_NAME)]
    private static extern bool QXRPlugin_ChangeResolution(uint width, uint height, uint framerate);

    [DllImport(PLUGIN_NAME)]
    private static extern bool QXRPlugin_EnableUserInput();

    [DllImport(PLUGIN_NAME)]
    private static extern bool QXRPlugin_MoveMouse(int x, int y, bool relative);

    [DllImport(PLUGIN_NAME)]
    private static extern bool QXRPlugin_ScrollMouse(int delta, bool horizontal);

    [DllImport(PLUGIN_NAME)]
    private static extern bool QXRPlugin_PressMouseButton(int button, bool down);

    [DllImport(PLUGIN_NAME)]
    private static extern bool QXRPlugin_PressKeyboardKey(int key, bool down);

    [DllImport(PLUGIN_NAME)]
    private static extern bool QXRPlugin_TypeKeyboardChar(int ch);

    private static PluginEventHandler m_EventCallback;

    public static event EventHandler<ServerEventArgs> OnServerFound;
    public static event EventHandler<ServerEventArgs> OnServerLost;

    public static event EventHandler<ServerEventArgs> OnConnectedToServer;
    public static event EventHandler<ServerEventArgs> OnDisconnectedFromServer;
    public static event EventHandler<ServerEventArgs> OnConnectionToServerFailed;

    #region Singleton
    public static QuarkXRClientPlugin Instance
    {
        get
        {
            return GetInstance();
        }
    }
    private static QuarkXRClientPlugin m_Instance = null;
    private static object m_Lock = new object();

    private static bool m_Initialized = false;

    private static QuarkXRClientPlugin GetInstance()
    {
        lock (m_Lock)
        {
            return m_Instance;
        }
    }
    private static QuarkXRClientPlugin GetAndDestroyInstance()
    {
        lock (m_Lock)
        {
            QuarkXRClientPlugin instance = m_Instance;

            m_Instance = null;

            return instance;
        }
    }
    private static void InitInstance(QuarkXRClientPlugin instance)
    {
        lock (m_Lock)
        {
            m_Instance = instance;
        }
    }
    #endregion

    private abstract class BaseEventQueueEntry
    {
        public QuarkXRClientPlugin Sender { get; set; }
        public abstract void FireEvent();
    }
    private class ServerEventQueueEntry : BaseEventQueueEntry
    {
        public ServerEventArgs Args { get; set; }
        public EventHandler<ServerEventArgs> Handler { get; set; }

        public override void FireEvent()
        {
            Handler?.Invoke(Sender, Args);
        }
    }

    private static readonly ConcurrentQueue<BaseEventQueueEntry> m_EventsQueue = new ConcurrentQueue<BaseEventQueueEntry>();

    private static ServerEventArgs GetServerEventArgs(long[] args)
    {
        long id = args[0];
        string name = Marshal.PtrToStringAnsi((IntPtr)args[1]);
        string host = Marshal.PtrToStringAnsi((IntPtr)args[2]);
        ushort port = (ushort)args[3];

        return new ServerEventArgs() { Id = id, Name = name, Host = host, Port = port };
    }

    [AOT.MonoPInvokeCallback(typeof(PluginEventHandler))]
    static void HandlePluginEvent(
        IntPtr context,
        [MarshalAs(UnmanagedType.LPStr)] string eventName,
        [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] long[] args,
        uint size
        )
    {
        QXRPlugin_Log("OnEvent: " + eventName);

        QuarkXRClientPlugin _this = GetInstance();

        if (_this == null) return;

        BaseEventQueueEntry queueEntry;

        if (eventName == "onServerFound")
        {
            queueEntry = new ServerEventQueueEntry() { Sender = _this, Args = GetServerEventArgs(args), Handler = OnServerFound };
        }
        else if (eventName == "onServerLost")
        {
            queueEntry = new ServerEventQueueEntry() { Sender = _this, Args = GetServerEventArgs(args), Handler = OnServerLost };
        }
        else if (eventName == "onConnectedToServer")
        {
            queueEntry = new ServerEventQueueEntry() { Sender = _this, Args = GetServerEventArgs(args), Handler = OnConnectedToServer };
        }
        else if (eventName == "onDisconnectedFromServer")
        {
            queueEntry = new ServerEventQueueEntry() { Sender = _this, Args = GetServerEventArgs(args), Handler = OnDisconnectedFromServer };
        }
        else if (eventName == "onConnectionToServerFailed")
        {
            queueEntry = new ServerEventQueueEntry() { Sender = _this, Args = GetServerEventArgs(args), Handler = OnConnectionToServerFailed };
        }
        else
        {
            return;
        }

        m_EventsQueue.Enqueue(queueEntry);
    }

    void Awake()
    {
        QXRPlugin_Log("OnAwake");

        InitInstance(this);

        if (m_Initialized) return;

#if UNITY_ANDROID
        AndroidJavaClass jc = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        AndroidJavaObject jo = jc.GetStatic<AndroidJavaObject>("currentActivity");
        QXRPlugin_AndroidInit(jo.GetRawObject());
#endif
        m_EventCallback = new PluginEventHandler(HandlePluginEvent);

        QXRPlugin_Init(Application.streamingAssetsPath, IntPtr.Zero, m_EventCallback);

        Application.quitting += OnAppQuitting;

        m_Initialized = true;

        QXRPlugin_Log("Initializing framework");
    }
    private void OnDestroy()
    {
        QXRPlugin_Log("OnDestroy");
    }
    static void OnAppQuitting()
    {
        GetAndDestroyInstance();

        QXRPlugin_Log("OnAppQuitting");

        QXRPlugin_Done();

        m_EventCallback = null;

#if UNITY_ANDROID
        AndroidJavaClass jc = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        AndroidJavaObject jo = jc.GetStatic<AndroidJavaObject>("currentActivity");
        QXRPlugin_AndroidDone(jo.GetRawObject());
#endif

        m_Initialized = false;
    }
    void OnApplicationPause(bool pauseStatus)
    {
        if (this != GetInstance()) return;

        if (pauseStatus)
        {
            QXRPlugin_Log("OnAppPause");

            QXRPlugin_Pause();
        }
        else
        {
            QXRPlugin_Log("OnAppResume");

            QXRPlugin_Resume();
        }
    }
    void OnApplicationFocus(bool hasFocus)
    {
        if (this != GetInstance()) return;

        if (hasFocus)
        {
            QXRPlugin_Log("OnAppGainFocus");
        }
        else
        {
            QXRPlugin_Log("OnAppKillFocus");
        }
    }
    void OnEnable()
    {
        if (this != GetInstance()) return;

        QXRPlugin_Log("OnEnable");

        QXRPlugin_Enable();
    }
    void OnDisable()
    {
        if (this != GetInstance()) return;

        QXRPlugin_Log("OnDisable");

        QXRPlugin_Disable();
    }
    void Start()
    {
        if (this != GetInstance()) return;

        QXRPlugin_Log("OnStart");

        QXRPlugin_EnableUserInput();
    }

    private void Update()
    {
        if (this != GetInstance()) return;

        BaseEventQueueEntry queueEntry;

        while (m_EventsQueue.TryDequeue(out queueEntry))
        {
            queueEntry.FireEvent();
        }
    }

    void OnApplicationQuit()
    {
        if (this != GetInstance()) return;

        QXRPlugin_Log("OnAppQuit");
    }

    public static bool SetTexture(IntPtr texture2D)
    {
        if (GetInstance() == null)
        {
            Debug.LogError(MISSING_PLUGIN_MESSAGE);
            return false;
        }

        QXRPlugin_ChangeResolution(1920, 1080, (uint)Screen.currentResolution.refreshRate);

        QXRPlugin_SetTexture(texture2D);

        return true;
    }

    public static IntPtr GetRenderFunction()
    {
        if (GetInstance() == null) return IntPtr.Zero;

        return QXRPlugin_GetRenderFunction();
    }

    public static bool SetBoolProperty(string property, bool value)
    {
        if (GetInstance() == null) return false;

        return QXRPlugin_SetBoolProperty(property, value);
    }

    public void ConnectToServer(long id = -1)
    {
        if (GetInstance() == null)
        {
            Debug.LogError(MISSING_PLUGIN_MESSAGE);
            return;
        }

        QXRPlugin_ConnectToServer(id);
    }

    public void DisconnectFromServer()
    {
        if (GetInstance() == null) return;

        QXRPlugin_DisconnectFromServer();
    }

    public bool RemoteMoveMouse(int x, int y, bool relative)
    {
        if (GetInstance() == null) return false;

        return QXRPlugin_MoveMouse(x, y, relative);
    }

    public bool RemoteScrollMouse(int delta, bool horizontal)
    {
        if (GetInstance() == null) return false;

        return QXRPlugin_ScrollMouse(delta, horizontal);
    }

    /*
    Valid Values:
    KeyCode.Mouse0 - Left Button
    KeyCode.Mouse1 - Right Button
    KeyCode.Mouse2 - Middle Button
    KeyCode.Mouse3 - First Additional Button
    KeyCode.Mouse4 - Second Additional Button
    */
    public bool RemotePressMouseButton(KeyCode key, bool down)
    {
        if (GetInstance() == null) return false;

        return QXRPlugin_PressMouseButton((int)key, down);
    }

    /*
    Valid Values:
    KeyCode key < 320 (KeyCode.Menu) - Only physical keyboard keys
    */
    public bool RemotePressKeyboardKey(KeyCode key, bool down)
    {
        if (GetInstance() == null) return false;

        return QXRPlugin_PressKeyboardKey((int)key, down);
    }

    /*
    Valid Values
    Any printable Unicode Character
    */
    public bool RemoteTypeKeyboardChar(char ch)
    {
        if (GetInstance() == null) return false;

        return QXRPlugin_TypeKeyboardChar((int)ch);
    }
}
