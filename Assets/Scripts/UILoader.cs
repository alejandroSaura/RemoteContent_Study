using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class UILoader : MonoBehaviour
{
    public List<RectTransform> panelLocations;     

    void Start()
    {
        
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.KeypadPlus))
        {
            Load();
        }

        if (Input.GetKeyDown(KeyCode.KeypadMinus))
        {
            Unload();
        }
    }

    async Task Load()
    {
        await CheckForUpdates();
        await LoadAllUIPanels();
    }

    async Task CheckForUpdates()
    {
        AsyncOperationHandle<List<string>> CheckForCatalogUpdates_op = Addressables.CheckForCatalogUpdates();
        await CheckForCatalogUpdates_op.Task;

        if (CheckForCatalogUpdates_op.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogError("CheckForCatalogUpdates_op failed");
        }

        if (CheckForCatalogUpdates_op.Result != null && CheckForCatalogUpdates_op.Result.Count > 0)
        {
            Debug.Log("There are content updates available");

            var UpdateCatalogs_op = Addressables.UpdateCatalogs();
            await UpdateCatalogs_op.Task;

            if (UpdateCatalogs_op.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError("UpdateCatalogs_op failed");
            }

            Debug.Log("Content catalogs updated");
        }
    }

    async Task LoadAllUIPanels()
    {
        var loadMainPanels_op = Addressables.LoadAssetsAsync<GameObject>("UIMainPanels", (o) => { Debug.Log($"Prefab {o.name} loaded"); });
        await loadMainPanels_op.Task;

        if(loadMainPanels_op.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogError("loadMainPanels_op failed");
        }

        for (int i = 0; i < loadMainPanels_op.Result.Count; i++)
        {
            GameObject prefab = loadMainPanels_op.Result[i];
            GameObject instance = Instantiate(prefab, panelLocations[i]);

            RectTransform rt = instance.GetComponent<RectTransform>();
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
        }
    }

    void Unload()
    {

    }
}
