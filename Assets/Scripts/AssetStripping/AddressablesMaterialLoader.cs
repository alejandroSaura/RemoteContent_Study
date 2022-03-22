
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class AddressablesMaterialLoader : MonoBehaviour, IAddressablesLoader
{
    [SerializeField] private MeshRenderer meshRenderer = null;
    [SerializeField] private AssetReference addressablesMaterial = null;
    private AsyncOperationHandle<Material> _asyncOperationHandle;

    private void Awake()
    {
        _asyncOperationHandle = addressablesMaterial.LoadAssetAsync<Material>();
        _asyncOperationHandle.Completed += handle =>
        {
            meshRenderer.sharedMaterial = handle.Result;
        };
    }

    private void OnDestroy()
    {
        if (_asyncOperationHandle.IsValid())
        {
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

