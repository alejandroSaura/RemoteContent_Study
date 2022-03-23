
using System;
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

namespace ContentUpdating
{
    public class ContentUpdater
    {
        string kCacheDataFolder = Application.persistentDataPath + "/com.unity.addressables";

        string m_remoteContentCatalogAddress = "http://localhost:8080/Dev/Windows/catalog_remote_Dev.json";

        bool m_remoteCatalogLoaded;

        AsyncOperationHandle m_DownloadDependencies_Op;

        public ContentUpdater(string remoteContentCatalogAddress)
        {
            m_remoteContentCatalogAddress = remoteContentCatalogAddress;
            m_remoteCatalogLoaded = false;
        }

        public async Task LoadRemoteContentCatalog()
        {
            await LoadRemoteContentCatalog(m_remoteContentCatalogAddress);
            m_remoteCatalogLoaded = true;
        }

        async Task LoadRemoteContentCatalog(string catalogAddress)
        {
            AsyncOperationHandle contentCatalogLoad_op = Addressables.LoadContentCatalogAsync(catalogAddress); // This doesn't cache the catalog for some obscure and hidden reason
            await contentCatalogLoad_op.Task;
            Debug.Assert(contentCatalogLoad_op.Status == AsyncOperationStatus.Succeeded);

            Addressables.Release(contentCatalogLoad_op);
        }

        public async Task<ContentUpdateInfo> CheckRemoteContentUpdate()
        {
            try
            {
                if (!m_remoteCatalogLoaded)
                {
                    throw new Exception("Load the remote catalog before checking for updates");
                }
                await UpdateRemoteContentCatalog();
                ContentUpdateInfo contentUpdateInfo = await GetContentUpdateInfo();
                return contentUpdateInfo;
            }
            catch(Exception e)
            {
                Debug.LogError(e);
                return null;
            }
        }

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

        async Task UpdateRemoteContentCatalog()
        {
            Debug.Log($"Updating Remote Catalogs...");

            List<string> catalogsToUpdate = new List<string>();

            AsyncOperationHandle<List<string>> CheckForCatalogUpdates_Op = Addressables.CheckForCatalogUpdates(false); // This caches the catalog
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

        async Task<ContentUpdateInfo> GetContentUpdateInfo()
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
                if (!keys.Contains(resourceLocation.PrimaryKey))
                {
                    keys.Add(resourceLocation.PrimaryKey);
                }
            }

            foreach (var key in keys)
            {
                float size = await Addressables.GetDownloadSizeAsync(key).Task;
                if (size > 0.0f)
                {
                    //Debug.Log($"Download pending of {size * 0.000001f} MB for key {key}");
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
                //string debugString = "Content to Update: \n\n";
                //for (int i = 0; i < result.keysToUpdate.Count; i++)
                //{
                //    debugString += $"{result.keysToUpdate[i]} - {result.downloadSizes[i]} MB \n";
                //}
                //debugString += $"Total content pending size = {result.totalDownloadSize} MB \n";

                //Debug.Log(debugString);
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

        public void DeleteAllCachedData()
        {
            DeleteCacheFolder();
            UnityEngine.Caching.ClearCache();
        }

        void DeleteCacheFolder()
        {
            if (!Directory.Exists(kCacheDataFolder))
            {
                return;
            }

            string[] files = Directory.GetFiles(kCacheDataFolder);
            foreach (string file in files)
            {
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }

            Directory.Delete(kCacheDataFolder, false);
        }
    }
}
