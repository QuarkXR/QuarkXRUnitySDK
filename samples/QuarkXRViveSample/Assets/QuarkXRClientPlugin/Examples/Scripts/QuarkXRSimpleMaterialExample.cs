/************************************************************************************
Copyright : Copyright (c) QuarkXR INC and its affiliates. All rights reserved.
************************************************************************************/

using System.Collections;
using UnityEngine;

public class QuarkXRSimpleMaterialExample : MonoBehaviour
{
    public QuarkXRTextureProvider m_TextureProvider;
    public Texture2D              m_LoadingScreenTexture;
    public Material               m_StreamingMaterial;

    private const float FULL_HD_RATIO = 1920.0f / 1080;

    private Texture2D m_StreamingTexture;

    private void OnDisconnectedFromServerHandler(object sender, ServerEventArgs args)
    {
        Debug.Log("QuarkXR OnDisconnectedFromServerHandler");

        if(m_LoadingScreenTexture != null)
        {
            m_StreamingMaterial.SetTexture("_MainTex", m_LoadingScreenTexture);
        }
    }
    private void OnConnectedToServerHandler(object sender, ServerEventArgs args)
    {
        Debug.Log("QuarkXR OnConnectedToServerHandler");

        m_StreamingMaterial.SetTexture("_MainTex", m_StreamingTexture);
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
        QuarkXRClientPlugin.OnServerFound               += OnServerFoundHandler;
        QuarkXRClientPlugin.OnServerLost                += OnServerLostHandler;
        QuarkXRClientPlugin.OnConnectedToServer         += OnConnectedToServerHandler;
        QuarkXRClientPlugin.OnDisconnectedFromServer    += OnDisconnectedFromServerHandler;
        QuarkXRClientPlugin.OnConnectionToServerFailed  += OnConnectionToServerFailedHandler;

        Debug.Log("QuarkXR Subscribed to events");
    }
    private void UnsubscribeFromEvents()
    {
        QuarkXRClientPlugin.OnServerFound               -= OnServerFoundHandler;
        QuarkXRClientPlugin.OnServerLost                -= OnServerLostHandler;
        QuarkXRClientPlugin.OnConnectedToServer         -= OnConnectedToServerHandler;
        QuarkXRClientPlugin.OnDisconnectedFromServer    -= OnDisconnectedFromServerHandler;
        QuarkXRClientPlugin.OnConnectionToServerFailed  -= OnConnectionToServerFailedHandler;

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
        while (m_TextureProvider.GetTexture() == null)
        {
            yield return new WaitForSeconds(0.05f);
        }

        QuarkXRClientPlugin.SetBoolProperty("UseLinearColorSpace", true);

        m_StreamingTexture = m_TextureProvider.GetTexture();

        m_StreamingMaterial.SetTexture("_MainTex", (m_LoadingScreenTexture != null)
                                                    ?
                                                    m_LoadingScreenTexture
                                                    :
                                                    m_StreamingTexture);

        transform.localScale = new Vector3(transform.localScale.x,
                                           transform.localScale.x / FULL_HD_RATIO,
                                           transform.localScale.z);
    }
}
