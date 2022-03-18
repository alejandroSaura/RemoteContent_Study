using UnityEditor.AddressableAssets.Settings;

public class ContentTypeGroupSchema : AddressableAssetGroupSchema
{
    public enum ContentType
    {
        Main,
        Secondary
    }

    public ContentType contentType;
}