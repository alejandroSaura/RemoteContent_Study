using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UITestScreen : MonoBehaviour
{
    [SerializeField] Button m_checkForUpdateButton;
    [SerializeField] Button m_downloadButton;
    [SerializeField] Button m_clearCacheButton;
    [SerializeField] Button m_spawnPrefabsButton;
    [SerializeField] Text m_infoText;
    [SerializeField] Image m_progressImage;

    [SerializeField] GameManager m_gameManager;
    [SerializeField] ContentUpdateBehaviour m_contentUpdateBehaviour;

    void Start()
    {
        m_checkForUpdateButton.onClick.AddListener(CheckForUpdate);
        m_downloadButton.onClick.AddListener(Download);
        m_clearCacheButton.onClick.AddListener(DeleteAllCachedData);
        m_spawnPrefabsButton.onClick.AddListener(SpawnPrefabs);

        m_downloadButton.interactable = false;
    }

    void OnDestroy()
    {
        m_checkForUpdateButton.onClick.RemoveListener(CheckForUpdate);
        m_downloadButton.onClick.RemoveListener(Download);
        m_clearCacheButton.onClick.RemoveListener(DeleteAllCachedData);
        m_spawnPrefabsButton.onClick.RemoveListener(SpawnPrefabs);
    }

    void SpawnPrefabs()
    {
        m_gameManager.SpawnPrefabs();
    }

    void DeleteAllCachedData()
    {
        m_contentUpdateBehaviour.DeleteAllContentCachedData();
    }

    async void CheckForUpdate()
    {
        ContentUpdateInfo contentUpdateInfo = await m_contentUpdateBehaviour.CheckRemoteContentUpdate();

        if (contentUpdateInfo != null && contentUpdateInfo.totalDownloadSize > 0)
        {
            string debugString = "Content to Update: \n\n";
            for (int i = 0; i < contentUpdateInfo.keysToUpdate.Count; i++)
            {
                debugString += $"{contentUpdateInfo.keysToUpdate[i]} - {contentUpdateInfo.downloadSizes[i]} MB \n";
            }
            debugString += $"\nTotal content pending size = {contentUpdateInfo.totalDownloadSize} MB \n";

            m_infoText.text = debugString;
        }
        else
        {
            m_infoText.text = "Content up to date";
        }
    }

    async void Download()
    {
        m_downloadSuccess = await m_contentUpdateBehaviour.DownloadRemoteContent();
    }

    bool m_downloadSuccess = false;

    private void Update()
    {
        if(m_downloadSuccess)
        {
            m_progressImage.fillAmount = 1.0f;
        }
        else
        {
            m_progressImage.fillAmount = m_contentUpdateBehaviour.GetDownloadProgress();
        }

        m_downloadButton.interactable = m_contentUpdateBehaviour.CheckedContentUpdateInfo != null;
    }
}
