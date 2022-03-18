using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using static UnityEngine.AddressableAssets.Addressables;

public class ContentUpdateInfo
{
    public float totalDownloadSize = 0.0f;
    public List<object> keysToUpdate;
    public List<float> downloadSizes;
}

public class GameManager : MonoBehaviour
{
    string m_weaponDefinitionsTag = "WeaponDefinitions";

    string m_mainRemoteContentCatalogAddress = "http://localhost:8080/Dev/Windows/Main/catalog_remote_Dev_main.json";
    string m_secondaryRemoteContentCatalogAddress = "http://localhost:8080/Dev/Windows/Secondary/catalog_remote_Dev_secondary.json";

    List<WeaponDefinition> m_weaponDefinitions;

    async void Start()
    {
        AsyncOperationHandle<IResourceLocator> initialize_op = Addressables.InitializeAsync();
        await initialize_op.Task;

        //m_weaponDefinitions = await LoadWeaponDefinitions();
        
        await LoadRemoteContentCatalog(m_secondaryRemoteContentCatalogAddress);
        await LoadRemoteContentCatalog(m_mainRemoteContentCatalogAddress);

        //await UpdateRemoteContentCatalogs();

        //await LoadRemoteContentCatalog(m_secondaryRemoteContentCatalogAddress, "Secondary");

        //Addressables.LoadContentCatalogAsync

        //await LoadSecondaryContentCatalog();

        //await GetContentUpdateInfo();

        //m_weaponDefinitions = await LoadWeaponDefinitions();
    }

    public void DeleteAllCachedData()
    {
        DeleteCacheFolder();
        UnityEngine.Caching.ClearCache();
    }

    void DeleteCacheFolder()
    {
        string catalogPath = Application.persistentDataPath + "/com.unity.addressables";
        if (Directory.Exists(catalogPath))
        {
            Directory.Delete(catalogPath);
        }
    }

    async Task LoadRemoteContentCatalog(string catalogAddress)
    {
        AsyncOperationHandle contentCatalogLoad_op = Addressables.LoadContentCatalogAsync(catalogAddress);
        await contentCatalogLoad_op.Task;
        Debug.Assert(contentCatalogLoad_op.Status == AsyncOperationStatus.Succeeded);
        Addressables.Release(contentCatalogLoad_op);

        //bool success = true;
        //bool needsToLoadCatalog = false;

        //int lastIndexOfSeparator = catalogAddress.LastIndexOf('/') + 1;
        //string catalogFilePath = Application.persistentDataPath + "/com.unity.addressables/" + catalogAddress.Substring(lastIndexOfSeparator, catalogAddress.Length - lastIndexOfSeparator);
        //if (!File.Exists(catalogFilePath))
        //{
        //    success = await DownloadFile(catalogAddress, catalogFilePath);

        //    string catalogHashAddress = catalogAddress.Replace(".json", ".hash");
        //    string catalogHashFilePath = catalogFilePath.Replace(".json", ".hash");
        //    success = success && await DownloadFile(catalogHashAddress, catalogHashFilePath);

        //    needsToLoadCatalog = true;
        //}                

        //if (needsToLoadCatalog && success)
        //{
        //    AsyncOperationHandle contentCatalogLoad_op = Addressables.LoadContentCatalogAsync(catalogFilePath);
        //    await contentCatalogLoad_op.Task;
        //    Debug.Assert(contentCatalogLoad_op.Status == AsyncOperationStatus.Succeeded);
        //    Addressables.Release(contentCatalogLoad_op);
        //}
    }

    async Task<bool> DownloadFile(string address, string cachePath)
    {
        bool success = false;

        if(File.Exists(cachePath))
        {
            return true;
        }

        Debug.Log($"{address} not cached, downloading from {cachePath}");

        // TO-DO: if this fails it creates an empty file!!

        UnityWebRequest webRequest = new UnityWebRequest(address, UnityWebRequest.kHttpVerbGET);
        webRequest.downloadHandler = new DownloadHandlerFile(cachePath);
        TaskCompletionSource<bool> taskCompletionSource = new TaskCompletionSource<bool>();

        UnityWebRequestAsyncOperation downloadOperation = webRequest.SendWebRequest();
        downloadOperation.completed += (op) =>
        {
            taskCompletionSource.SetResult(webRequest.error == null);
        };

        await taskCompletionSource.Task;

        if (taskCompletionSource.Task.Result == true)
        {
            Debug.Log($"{address} downloaded");
            return true;
        }
        else
        {
            File.Delete(cachePath);
            Debug.LogError($"{address} failed downloading");
            return false;
        }
    }

    public async Task UpdateRemoteContentCatalogs()
    {
        Debug.Log($"Updating Remote Catalogs...");

        List<string> catalogsToUpdate = new List<string>();

        AsyncOperationHandle<List<string>> CheckForCatalogUpdates_Op = Addressables.CheckForCatalogUpdates(false);
        await CheckForCatalogUpdates_Op.Task;

        if (!CheckForCatalogUpdates_Op.IsValid())
        {
            Debug.LogError("Error CheckForCatalogUpdates: " + CheckForCatalogUpdates_Op.OperationException.Message);
        }
        else
        {
            catalogsToUpdate = CheckForCatalogUpdates_Op.Result;
        }

        Addressables.Release(CheckForCatalogUpdates_Op);

        if (catalogsToUpdate != null && catalogsToUpdate.Count > 0)
        {
            foreach (var item in catalogsToUpdate)
            {
                Debug.Log($"Catalog {item} new version detected");
            }

            List<IResourceLocator> resourceLocators = new List<IResourceLocator>();

            AsyncOperationHandle<List<IResourceLocator>> UpdateCatalogs_Op = Addressables.UpdateCatalogs(catalogsToUpdate, false);
            await UpdateCatalogs_Op.Task;
            Debug.Assert(UpdateCatalogs_Op.IsValid());

            foreach (var item in catalogsToUpdate)
            {
                Debug.Log($"Catalog {item} updated");
            }

            Addressables.Release(UpdateCatalogs_Op);
        }
        else
        {
            Debug.Log($"All remote catalogs are up to date");
        }
    }

    public async Task<ContentUpdateInfo> GetContentUpdateInfo()
    {
        // Check if there's any content pending to download

        Debug.Log($"Checking if there's content pending to download...");

        ContentUpdateInfo result = new ContentUpdateInfo()
        {
            totalDownloadSize = 0.0f,
            keysToUpdate = new List<object>(),
            downloadSizes = new List<float>()
        };

        List<string> keys = new List<string>();

        List<IResourceLocation> resourceLocations = GetAddressablesResourceLocations();
        foreach (IResourceLocation resourceLocation in resourceLocations)
        {
            if(!keys.Contains(resourceLocation.PrimaryKey))
            {
                keys.Add(resourceLocation.PrimaryKey);
            }
        }

        foreach (var key in keys)
        {
            float size = await Addressables.GetDownloadSizeAsync(key).Task;
            if (size > 0.0f)
            {
                Debug.Log($"Download pending of {size * 0.000001f} MB for key {key}");
                result.keysToUpdate.Add(key);
                result.downloadSizes.Add(size * 0.000001f);
            }
            result.totalDownloadSize += size * 0.000001f;
        }

        if (result.totalDownloadSize == 0.0f)
        {
            Debug.Log($"All content is up to date");
        }
        else
        {
            string debugString = "Content to Update: \n\n";
            for (int i = 0; i < result.keysToUpdate.Count; i++)
            {
                debugString += $"{result.keysToUpdate[i]} - {result.downloadSizes[i]} MB \n";
            }
            debugString += $"Total content pending size = {result.totalDownloadSize} MB \n";

            Debug.Log(debugString);
        }

        return result;
    }


    List<IResourceLocation> GetAddressablesResourceLocations()
    {
        var allLocations = new List<IResourceLocation>();
        foreach (var resourceLocator in Addressables.ResourceLocators)
        {
            if (resourceLocator is ResourceLocationMap map)
            {
                foreach (var locations in map.Locations.Values)
                {
                    allLocations.AddRange(locations);
                }
            }
        }
        return allLocations;
    }

    AsyncOperationHandle m_DownloadDependencies_Op;

    public async Task<bool> DownloadContent(ContentUpdateInfo contentUpdateInfo)
    {
        bool downloadSuccessful = false;

        if (contentUpdateInfo.totalDownloadSize == 0.0f)
        {
            Debug.Log($"Nothing to download");
        }

        if (contentUpdateInfo.keysToUpdate.Count > 0)
        {
            // Download content

            Debug.Log($"Downloading pending content...");

            m_DownloadDependencies_Op = Addressables.DownloadDependenciesAsync(contentUpdateInfo.keysToUpdate, MergeMode.Union, false);
            Debug.Assert(m_DownloadDependencies_Op.IsValid());
            await m_DownloadDependencies_Op.Task;

            Debug.Log($"Finished downloading");

            Addressables.Release(m_DownloadDependencies_Op);
            downloadSuccessful = true;
        }

        return downloadSuccessful;
    }

    public float GetDownloadProgress()
    {
        if (!m_DownloadDependencies_Op.IsValid())
        {
            return 0.0f;
        }

        if (m_DownloadDependencies_Op.IsDone)
        {
            return 1.0f;
        }

        return m_DownloadDependencies_Op.PercentComplete;
    }

    async Task<List<WeaponDefinition>> LoadWeaponDefinitions()
    {
        AsyncOperationHandle<IList<WeaponDefinition>> weaponDefinitionsLoad_op = Addressables.LoadAssetsAsync<WeaponDefinition>(m_weaponDefinitionsTag, (o) => { Debug.Log($"Definition {o.name} loaded"); });
        await weaponDefinitionsLoad_op.Task;
        List<WeaponDefinition> weaponDefinitions = new List<WeaponDefinition>(weaponDefinitionsLoad_op.Result);
        return weaponDefinitions;
    }

    private void Update()
    {
        
    }
}
