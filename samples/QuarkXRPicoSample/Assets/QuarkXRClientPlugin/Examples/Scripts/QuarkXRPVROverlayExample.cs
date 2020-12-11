using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuarkXRPVROverlayExample : MonoBehaviour
{
    public Pvr_UnitySDKEyeOverlay pvrOverlay;
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

        QuarkXRClientPlugin.SetBoolProperty("UseLinearColorSpace", false);

        pvrOverlay.SetTexture(texture);

        transform.localScale = new Vector3(transform.localScale.x,
                                           transform.localScale.x / fullHDRatio,
                                           transform.localScale.z);
    }
}
