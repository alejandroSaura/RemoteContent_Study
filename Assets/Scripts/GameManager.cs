using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;

public class ContentUpdateInfo
{
    public float totalDownloadSize = 0.0f;
    public List<object> keysToUpdate;
    public List<float> downloadSizes;
}

public class GameManager : MonoBehaviour
{
    string m_weaponDefinitionsTag = "WeaponDefinitions";
    List<WeaponDefinition> m_weaponDefinitions;
    Dictionary<AssetReference, List<GameObject>> m_instancedWeaponsGOs;

    void Awake()
    {
        m_instancedWeaponsGOs = new Dictionary<AssetReference, List<GameObject>>();
    }

    async void Start()
    {
        AsyncOperationHandle<IResourceLocator> initialize_op = Addressables.InitializeAsync();
        await initialize_op.Task;
    }

    async Task<List<WeaponDefinition>> LoadWeaponDefinitions()
    {
        AsyncOperationHandle<IList<WeaponDefinition>> weaponDefinitionsLoad_op = Addressables.LoadAssetsAsync<WeaponDefinition>(m_weaponDefinitionsTag, (o) => { Debug.Log($"Definition {o.name} loaded"); });
        await weaponDefinitionsLoad_op.Task;
        List<WeaponDefinition> weaponDefinitions = new List<WeaponDefinition>(weaponDefinitionsLoad_op.Result);
        Addressables.Release(weaponDefinitionsLoad_op);
        return weaponDefinitions;
    }

    public async void SpawnPrefabs()
    {
        DestroyExistingInstances();

        m_weaponDefinitions = await LoadWeaponDefinitions();

        m_instancedWeaponsGOs = new Dictionary<AssetReference, List<GameObject>>();

        for (int i = 0; i < m_weaponDefinitions.Count; i++)
        {
            WeaponDefinition weaponDefinition = m_weaponDefinitions[i];

            if(!m_instancedWeaponsGOs.ContainsKey(weaponDefinition.prefab))
            {
                m_instancedWeaponsGOs.Add(weaponDefinition.prefab, new List<GameObject>());
            }

            AsyncOperationHandle<GameObject> op = weaponDefinition.prefab.LoadAssetAsync<GameObject>();
            await op.Task;
            Debug.Assert(op.Result != null);
            GameObject instance = Instantiate(op.Result, new Vector3(0.0f, 1.0f, -i), Quaternion.identity);
        }
    }

    void DestroyExistingInstances()
    {
        foreach (var kvp in m_instancedWeaponsGOs)
        {
            AssetReference assetReference = kvp.Key;
            List<GameObject> instances = kvp.Value;

            foreach (GameObject instance in instances)
            {
                assetReference.ReleaseInstance(instance);
            }
        }
    }
}
