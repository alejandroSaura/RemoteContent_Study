using ContentUpdating;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class ContentUpdateBehaviour : MonoBehaviour
{
    string m_remoteContentCatalogAddress = "http://localhost:8080/Dev/Windows/catalog_remote_Dev.json";

    ContentUpdater m_contentUpdater;

    ContentUpdateInfo m_checkedContentUpdateInfo = null;
    public ContentUpdateInfo CheckedContentUpdateInfo { get => m_checkedContentUpdateInfo; }

    private async void Start()
    {
        m_contentUpdater = new ContentUpdater(m_remoteContentCatalogAddress);
    }

    // Call this on every startup as long as the player is not in the tutorial. If not called, the game will use only the local catalog
    public async Task LoadRemoteContentCatalog()
    {
        await m_contentUpdater.LoadRemoteContentCatalog();
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
