using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIMainScreen : MonoBehaviour
{
    [SerializeField] Button m_checkForUpdateButton;
    [SerializeField] Button m_downloadButton;
    [SerializeField] Button m_clearCacheButton;
    [SerializeField] Text m_infoText;
    [SerializeField] Image m_progressImage;

    [SerializeField] GameManager m_gameManager;

    void Start()
    {
        m_checkForUpdateButton.onClick.AddListener(CheckForUpdate);
        m_downloadButton.onClick.AddListener(Download);
        m_clearCacheButton.onClick.AddListener(DeleteAllCachedData);

        m_downloadButton.interactable = false;
    }

    void OnDestroy()
    {
        m_checkForUpdateButton.onClick.RemoveListener(CheckForUpdate);
        m_downloadButton.onClick.RemoveListener(Download);
        m_clearCacheButton.onClick.RemoveListener(DeleteAllCachedData);
    }

    void DeleteAllCachedData()
    {
        m_gameManager.DeleteAllCachedData();
    }

    async void CheckForUpdate()
    {
        m_checkForUpdateButton.interactable = false;

        await m_gameManager.UpdateRemoteContentCatalogs();
        ContentUpdateInfo contentUpdateInfo = await m_gameManager.GetContentUpdateInfo();

        if (contentUpdateInfo.totalDownloadSize > 0)
        {
            string debugString = "Content to Update: \n\n";
            for (int i = 0; i < contentUpdateInfo.keysToUpdate.Count; i++)
            {
                debugString += $"{contentUpdateInfo.keysToUpdate[i]} - {contentUpdateInfo.downloadSizes[i]} MB \n";
            }
            debugString += $"\nTotal content pending size = {contentUpdateInfo.totalDownloadSize} MB \n";

            m_infoText.text = debugString;

            m_downloadButton.interactable = true;
        }
        else
        {
            m_infoText.text = "Content up to date";
        }
    }

    async void Download()
    {
        ContentUpdateInfo contentUpdateInfo = await m_gameManager.GetContentUpdateInfo();
        m_downloadSuccess = await m_gameManager.DownloadContent(contentUpdateInfo);
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
            m_progressImage.fillAmount = m_gameManager.GetDownloadProgress();
        }        
    }
}
