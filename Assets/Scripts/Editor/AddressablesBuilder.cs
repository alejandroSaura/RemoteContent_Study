//using UnityEngine;
//using UnityEditor;
//using System.Collections;
//using UnityEditor.AddressableAssets.Settings;
//using UnityEditor.AddressableAssets;
//using System;
//using UnityEditor.AddressableAssets.Build;
//using UnityEditor.AddressableAssets.Settings.GroupSchemas;
//using System.Threading.Tasks;
//using System.IO;

//[CreateAssetMenu(fileName = "AddressablesBuilder", menuName = "Tools/AddressablesBuilder")]
//public class AddressablesBuilder : ScriptableObject
//{    
//    public enum 

//    TaskCompletionSource<bool> m_buildTask;

//    public async void BuildMainContent()
//    {
//        ContentTypeGroupSchema.ContentType contentType = ContentTypeGroupSchema.ContentType.Main;
//        string currentProfileName = GetAddressablesProfile();
//        SetAddressablesProfile("MainContent");        
//        await BuildContent(contentType);
//        SetAddressablesProfile(currentProfileName);

//        SetGroupsIncludedInBuild(ContentTypeGroupSchema.ContentType.Main, true);
//        SetGroupsIncludedInBuild(m_groupsToBuildAsSecondaryContent, false);

//        CleanRemoteDirectory();

//        m_buildTask = new TaskCompletionSource<bool>();
//        BuildScript.buildCompleted += OnBuildCompleted;
//        AddressableAssetSettings.BuildPlayerContent();
//        await m_buildTask.Task;
//    }

//    void SetProfile(string profileName)
//    {
//        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
//        string profileId = settings.profileSettings.GetProfileId(profileName);
//        Debug.Assert(!String.IsNullOrEmpty(profileId));
//        settings.activeProfileId = profileId;
//    }

//    public async void BuildSecondaryContent()
//    {
//        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
//        string profileId = settings.profileSettings.GetProfileId(m_secondaryContentProfileName);
//        Debug.Assert(!String.IsNullOrEmpty(profileId));
//        settings.activeProfileId = profileId;

//        await SetSecondaryFolders();

//        SetGroupsIncludedInBuild(m_groupsToBuildAsMainContent, false);
//        SetGroupsIncludedInBuild(m_groupsToBuildAsSecondaryContent, true);

//        CleanRemoteDirectory();

//        m_buildTask = new TaskCompletionSource<bool>();

//        BuildScript.buildCompleted += OnBuildCompleted;
//        AddressableAssetSettings.BuildPlayerContent();

//        await m_buildTask.Task;

//        await RestoreMainFolders();

//        profileId = settings.profileSettings.GetProfileId(m_mainContentProfileName);
//        Debug.Assert(!String.IsNullOrEmpty(profileId));
//        settings.activeProfileId = profileId;
//    }

//    private void OnBuildCompleted(AddressableAssetBuildResult result)
//    {
//        BuildScript.buildCompleted -= OnBuildCompleted;

//        bool success = string.IsNullOrEmpty(result.Error);
//        if (!success)
//        {
//            Debug.LogError("Addressables build error encountered: " + result.Error);
//        }
//        else
//        {
//            Debug.Log("Addressables build success");
//        }

//        m_buildTask.SetResult(success);
//    }

//    void SetGroupsIncludedInBuild(AddressableAssetGroup[] groups, bool included)
//    {
//        foreach (AddressableAssetGroup group in groups)
//        {
//            BundledAssetGroupSchema bundledAssetGroupSchema = group.GetSchema<BundledAssetGroupSchema>();
//            if(bundledAssetGroupSchema == null)
//            {
//                continue;
//            }
//            bundledAssetGroupSchema.IncludeInBuild = included;
//            EditorUtility.SetDirty(group);
//        }
//    }

//    void CleanRemoteDirectory()
//    {
//        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
//        string profileId = settings.profileSettings.GetProfileId(m_mainContentProfileName);
//        var remoteBuildFolder = Application.dataPath + "/../" + settings.RemoteCatalogBuildPath.GetValue(settings);
//        FileUtil.DeleteFileOrDirectory(remoteBuildFolder);
//    }

//    async Task SetSecondaryFolders()
//    {
//        var contentStatePath = Application.dataPath.Replace("/Assets", "") + "/" + ContentUpdateScript.GetContentStateDataPath(false);
//        DirectoryInfo directoryInfo = Directory.GetParent(contentStatePath);

//        m_originalContentStateFolder = directoryInfo.FullName;
//        m_tempContentStateFolder = directoryInfo.FullName + "_temp";
//        FileUtil.DeleteFileOrDirectory(m_tempContentStateFolder);
//        FileUtil.DeleteFileOrDirectory(m_tempContentStateFolder + ".meta");
//        FileUtil.MoveFileOrDirectory(m_originalContentStateFolder, m_tempContentStateFolder);

//        m_originalLibraryAAFolder = Application.dataPath + "/../" + "Library/com.unity.addressables";
//        m_tempLibraryAAFolder = m_originalLibraryAAFolder + "_temp";
//        FileUtil.DeleteFileOrDirectory(m_tempLibraryAAFolder);
//        FileUtil.MoveFileOrDirectory(m_originalLibraryAAFolder, m_tempLibraryAAFolder);
//    }

//    string m_originalContentStateFolder;
//    string m_originalLibraryAAFolder;
//    string m_tempContentStateFolder;
//    string m_tempLibraryAAFolder;

//    async Task RestoreMainFolders()
//    {
//        string secondaryContentStateFolder = m_originalContentStateFolder + "_Secondary";
//        FileUtil.DeleteFileOrDirectory(secondaryContentStateFolder);
//        FileUtil.MoveFileOrDirectory(m_originalContentStateFolder, secondaryContentStateFolder);
//        FileUtil.DeleteFileOrDirectory(m_originalContentStateFolder);
//        FileUtil.MoveFileOrDirectory(m_tempContentStateFolder, m_originalContentStateFolder);
//        FileUtil.DeleteFileOrDirectory(m_tempContentStateFolder);
//        FileUtil.DeleteFileOrDirectory(m_tempContentStateFolder + ".meta");

//        string secondaryLibraryAAFolder = m_originalLibraryAAFolder + "_Secondary";
//        FileUtil.DeleteFileOrDirectory(secondaryLibraryAAFolder);
//        FileUtil.MoveFileOrDirectory(m_originalLibraryAAFolder, secondaryLibraryAAFolder);
//        FileUtil.DeleteFileOrDirectory(m_originalLibraryAAFolder);
//        FileUtil.MoveFileOrDirectory(m_tempLibraryAAFolder, m_originalLibraryAAFolder);
//        FileUtil.DeleteFileOrDirectory(m_tempLibraryAAFolder);
//    }

//    internal void IncrementalBuildMainContent()
//    {
//        throw new NotImplementedException();
//    }

//    internal void IncrementalBuildSecondaryContent()
//    {
//        throw new NotImplementedException();
//    }
//}

//[CustomEditor(typeof(AddressablesBuilder))]
//public class AddressablesBuilderEditor : Editor
//{
//    public override void OnInspectorGUI()
//    {
//        DrawDefaultInspector();

//        AddressablesBuilder builder = (AddressablesBuilder)target;
//        if (GUILayout.Button("Build Main Content (Discard previous build)"))
//        {
//            builder.BuildMainContent();
//        }
//        if (GUILayout.Button("Incremental Build Main Content (Update previous build)"))
//        {
//            builder.IncrementalBuildMainContent();
//        }
//        if (GUILayout.Button("Build Secondary Content (Discard previous build)"))
//        {
//            builder.BuildSecondaryContent();
//        }
//        if (GUILayout.Button("Incremental Build Secondary Content (Update previous build)"))
//        {
//            builder.IncrementalBuildSecondaryContent();
//        }
//    }
//}
