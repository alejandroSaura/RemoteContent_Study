using ContentUpdating;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class ContentUpdateBehaviour : MonoBehaviour
{
    string m_mainRemoteContentCatalogAddress = "http://localhost:8080/Dev/Windows/Main/catalog_remote_Dev_main.json";
    string m_secondaryRemoteContentCatalogAddress = "http://localhost:8080/Dev/Windows/Secondary/catalog_remote_Dev_secondary.json";

    ContentUpdater m_contentUpdater;

    ContentUpdateInfo m_checkedContentUpdateInfo = null;
    public ContentUpdateInfo CheckedContentUpdateInfo { get => m_checkedContentUpdateInfo; }

    private void Start()
    {
        m_contentUpdater = new ContentUpdater(m_mainRemoteContentCatalogAddress, m_secondaryRemoteContentCatalogAddress);
    }

    public async Task<ContentUpdateInfo> CheckRemoteContentUpdate()
    {
        m_checkedContentUpdateInfo = await m_contentUpdater.CheckRemoteContentUpdate();
        return m_checkedContentUpdateInfo;
    }

    public async Task<bool> DownloadRemoteContent()
    {
        bool success = await m_contentUpdater.DownloadContent(m_checkedContentUpdateInfo);
        if (success)
        {
            m_checkedContentUpdateInfo = null;
        }
        return success;
    }

    public float GetDownloadProgress()
    {
        return m_contentUpdater.GetDownloadProgress();
    }

    public void DeleteAllContentCachedData()
    {
        m_contentUpdater.DeleteAllCachedData();
    }
}
