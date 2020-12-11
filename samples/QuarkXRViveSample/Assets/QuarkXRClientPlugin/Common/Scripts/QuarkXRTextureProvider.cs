/************************************************************************************
Copyright : Copyright (c) QuarkXR INC and its affiliates. All rights reserved.
************************************************************************************/

using System.Collections;
using UnityEngine;

[RequireComponent(typeof(QuarkXRClientPlugin))]
public class QuarkXRTextureProvider : MonoBehaviour
{
    private static Texture2D m_Texture = null;

    private int m_TextureWidth  = 1920;
    private int m_TextureHeight = 1080;

    private const int QXR_PLUGIN_EVENT_ID_RENDER_INIT  = 0;
    private const int QXR_PLUGIN_EVENT_ID_RENDER_DONE  = 1;
    private const int QXR_PLUGIN_EVENT_ID_RENDER_FRAME = 2;

    private bool m_IsRendering = false;
    private bool m_IsWorking = false;

    void Start()
    {
        if (m_Texture != null) return;

        m_Texture = new Texture2D(m_TextureWidth, m_TextureHeight, TextureFormat.RGBA32, false);

        m_Texture.filterMode = FilterMode.Bilinear;
        m_Texture.wrapMode   = TextureWrapMode.Clamp;

        m_Texture.Apply();

        Debug.Log("QuarkXRTextureProvider texture created");

        if (!QuarkXRClientPlugin.SetTexture(m_Texture.GetNativeTexturePtr()))
        {
            m_Texture = null;

            Debug.LogError("QuarkXRTextureProvider texture destroyed, couldn't set it in plugin");

            return;
        }

        m_IsWorking = true;

        GL.IssuePluginEvent(QuarkXRClientPlugin.GetRenderFunction(), QXR_PLUGIN_EVENT_ID_RENDER_INIT);

        StartCoroutine("CallPluginAtEndOfFrames");
        m_IsRendering = true;
    }
    void OnApplicationQuit()
    {
        if (!m_IsWorking) return;

        StopCoroutine("CallPluginAtEndOfFrames");

        GL.IssuePluginEvent(QuarkXRClientPlugin.GetRenderFunction(), QXR_PLUGIN_EVENT_ID_RENDER_DONE);
    }
    private IEnumerator CallPluginAtEndOfFrames()
    {
        while (true)
        {
            yield return new WaitForEndOfFrame();

            GL.IssuePluginEvent(QuarkXRClientPlugin.GetRenderFunction(), QXR_PLUGIN_EVENT_ID_RENDER_FRAME);
        }
    }
    public Texture2D GetTexture()
    {
        return m_Texture;
    }
    private void OnEnable()
    {
        if (!m_IsWorking) return;

        if (!m_IsRendering && m_Texture != null)
        {
            StopCoroutine("CallPluginAtEndOfFrames");
            StartCoroutine("CallPluginAtEndOfFrames");
            m_IsRendering = true;
        }
    }
    private void OnDisable()
    {
        if (!m_IsWorking) return;

        StopCoroutine("CallPluginAtEndOfFrames");
        m_IsRendering = false;
    }
    void OnDestroy()
    {
        if (m_Texture == null) return;

        m_Texture = null;

        Debug.Log("QuarkXRTextureProvider texture destroyed");
    }
}
