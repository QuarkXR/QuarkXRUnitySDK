/************************************************************************************
Copyright : Copyright (c) QuarkXR INC and its affiliates. All rights reserved.
************************************************************************************/

using System.Collections;
using UnityEngine;

public class QuarkXROVROverlayExample : MonoBehaviour
{
    public OVROverlay             ovrOverlay;
    public QuarkXRTextureProvider textureProvider;

    private const float fullHDRatio = 1920.0f / 1080;

    private void OnDisconnectedFromServerHandler(object sender, ServerEventArgs args)
    {
        Debug.Log("QuarkXR OnDisconnectedFromServerHandler");
    }
    private void OnConnectedToServerHandler(object sender, ServerEventArgs args)
    {
        Debug.Log("QuarkXR OnConnectedToServerHandler");
    }
    private void OnServerFoundHandler(object sender, ServerEventArgs args)
    {
        Debug.Log("QuarkXR OnServerFoundHandler");
    }
    private void OnServerLostHandler(object sender, ServerEventArgs args)
    {
        Debug.Log("QuarkXR OnServerLostHandler");
    }
    private void OnConnectionToServerFailedHandler(object sender, ServerEventArgs args)
    {
        Debug.Log("QuarkXR OnConnectionToServerFailed");
    }
    private void SubscribeToEvents()
    {
        QuarkXRClientPlugin.OnServerFound += OnServerFoundHandler;
        QuarkXRClientPlugin.OnServerLost += OnServerLostHandler;
        QuarkXRClientPlugin.OnConnectedToServer += OnConnectedToServerHandler;
        QuarkXRClientPlugin.OnDisconnectedFromServer += OnDisconnectedFromServerHandler;
        QuarkXRClientPlugin.OnConnectionToServerFailed += OnConnectionToServerFailedHandler;

        Debug.Log("QuarkXR Subscribed to events");
    }
    private void UnsubscribeFromEvents()
    {
        QuarkXRClientPlugin.OnServerFound -= OnServerFoundHandler;
        QuarkXRClientPlugin.OnServerLost -= OnServerLostHandler;
        QuarkXRClientPlugin.OnConnectedToServer -= OnConnectedToServerHandler;
        QuarkXRClientPlugin.OnDisconnectedFromServer -= OnDisconnectedFromServerHandler;
        QuarkXRClientPlugin.OnConnectionToServerFailed -= OnConnectionToServerFailedHandler;

        Debug.Log("QuarkXR Unsubscribed from events");
    }
    private void OnEnable()
    {
        SubscribeToEvents();
    }
    private void OnDisable()
    {
        UnsubscribeFromEvents();
    }
    IEnumerator Start()
    {
        while (textureProvider.GetTexture() == null)
        {
            yield return new WaitForSeconds(0.05f);
        }

        Texture2D texture = textureProvider.GetTexture();

        QuarkXRClientPlugin.SetBoolProperty("UseLinearColorSpace", true);

        ovrOverlay.OverrideOverlayTextureInfo(texture, texture.GetNativeTexturePtr(), UnityEngine.XR.XRNode.LeftEye);
        ovrOverlay.OverrideOverlayTextureInfo(texture, texture.GetNativeTexturePtr(), UnityEngine.XR.XRNode.RightEye);

        ovrOverlay.layerTextureFormat   = OVRPlugin.EyeTextureFormat.R8G8B8A8;
        ovrOverlay.isAlphaPremultiplied = false;
        ovrOverlay.hidden               = false;

        transform.localScale = new Vector3(transform.localScale.x,
                                           transform.localScale.x / fullHDRatio,
                                           transform.localScale.z);
    }
}
