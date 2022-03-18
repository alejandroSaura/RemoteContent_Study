
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class MaterialLoader : MonoBehaviour, IAddressablesLoader
{
    [SerializeField] private MeshRenderer meshRenderer = null;
    [SerializeField] private AssetReference addressablesMaterial = null;
    private AsyncOperationHandle<Material> _asyncOperationHandle;

    private void Awake()
    {
        Debug.Log("AddressablesMeshLoader: Awake");
        _asyncOperationHandle = addressablesMaterial.LoadAssetAsync<Material>();
        _asyncOperationHandle.Completed += handle =>
        {
            meshRenderer.sharedMaterial = handle.Result;
        };
    }

    private void OnDestroy()
    {
        Debug.Log("AddressablesMeshLoader: OnDestroy?");
        if (_asyncOperationHandle.IsValid())
        {
            Debug.Log("AddressablesMeshLoader: OnDestroy!");
            Addressables.Release(_asyncOperationHandle);
        }
    }

    public void SetEditorPrefabPreview(bool on)
    {
#if UNITY_EDITOR
        var targetMaterial = on ? addressablesMaterial.editorAsset as Material : null;
        meshRenderer.sharedMaterial = targetMaterial;
#endif
    }
}

