using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class AddressablesBuildWindow : EditorWindow
{
    enum groupBuildlocation
    {
        LOCAL,
        REMOTE
    }

    [MenuItem("Tools/Addressables Build")]
    public static void ShowWindow()
    {
        GetWindow(typeof(AddressablesBuildWindow));
    }

    void OnGUI()
    {
        if (GUILayout.Button("Build local groups only"))
        {
            BuildLocalGroupsOnly();
        }

        if (GUILayout.Button("Build remote main content"))
        {
            BuildRemoteMainContent(new Version(Application.version));
        }

        if (GUILayout.Button("Build remote secondary content"))
        {
            BuildRemoteSecondaryContent(new Version(Application.version));
        }

        if (GUILayout.Button("Build all groups forced as local"))
        {
            //BuildMainContent();
        }

        if (GUILayout.Button("Build server flagged groups forced as local"))
        {
            //BuildMainContent();
        }
    }

    public static void BuildLocalGroupsOnly()
    {
        BuildVars.DeploymentEnvironment = "Dev";        
        BuildVars.Platform = PlatformMappingService.GetPlatform().ToString();

        bool setProfileSuccess = SetProfile("Default");
        if (!setProfileSuccess)
        {
            return;
        }        

        Dictionary<AddressableAssetGroup, bool> groupsIncludedState = SaveGroupsIncludedState();

        GroupFilter groupFilter = new GroupFilter() { buildPath = AddressableAssetSettings.kLocalBuildPath };
        List<AddressableAssetGroup> groupsToBuild = GetFilteredGroups(groupFilter);
        SetGroupsIncludedState(groupsToBuild);

        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
        settings.BuildRemoteCatalog = false;
        AddressableAssetSettings.BuildPlayerContent();
        settings.BuildRemoteCatalog = true;

        RestoreGroupsIncludedState(groupsIncludedState);
    }

    public static void BuildRemoteMainContent(Version version)
    {  
        SaveCurrentAddressablesLibraryFolder();

        BuildVars.DeploymentEnvironment = "Dev";
        BuildVars.ContentType = "main";
        BuildVars.Platform = PlatformMappingService.GetPlatform().ToString();

        bool setProfileSuccess = SetProfile("MainContent");
        if (!setProfileSuccess)
        {
            return;
        }

        Dictionary<AddressableAssetGroup, bool> groupsIncludedState = SaveGroupsIncludedState();

        GroupFilter groupFilter = new GroupFilter() { buildPath = AddressableAssetSettings.kRemoteBuildPath, contentType = ContentTypeGroupSchema.ContentType.Main };
        List<AddressableAssetGroup> groupsToBuild = GetFilteredGroups(groupFilter);
        SetGroupsIncludedState(groupsToBuild);

        DeleteRemoteTargetFolder();

        Build();

        RestoreGroupsIncludedState(groupsIncludedState);
        AssetDatabase.Refresh();

        DeleteAddressablesLibraryFolder();

        RestorePreviousAddressablesLibraryFolder();
    } 

    public static void BuildRemoteSecondaryContent(Version version)
    {
        SaveCurrentAddressablesLibraryFolder();

        BuildVars.DeploymentEnvironment = "Dev";
        BuildVars.ContentType = "secondary";
        BuildVars.Platform = PlatformMappingService.GetPlatform().ToString();

        bool setProfileSuccess = SetProfile("SecondaryContent");
        if(!setProfileSuccess)
        {
            return;
        }

        Dictionary<AddressableAssetGroup, bool> groupsIncludedState = SaveGroupsIncludedState();

        GroupFilter groupFilter = new GroupFilter() { buildPath = AddressableAssetSettings.kRemoteBuildPath, contentType = ContentTypeGroupSchema.ContentType.Secondary };
        List<AddressableAssetGroup> groupsToBuild = GetFilteredGroups(groupFilter);
        SetGroupsIncludedState(groupsToBuild);

        DeleteRemoteTargetFolder();

        Build();

        RestoreGroupsIncludedState(groupsIncludedState);
        AssetDatabase.Refresh();

        DeleteAddressablesLibraryFolder();

        RestorePreviousAddressablesLibraryFolder();
    }

    static void Build()
    {
        try
        {
            AddressableAssetSettings.BuildPlayerContent();
        }
        catch(Exception e)
        {
            Debug.LogError(e);
        }
    }

    static bool SetProfile(string profileName)
    {
        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
        string profileId = settings.profileSettings.GetProfileId(profileName);
        if (string.IsNullOrEmpty(profileId))
        {
            Debug.LogError($"Couldn't find a profile named, {profileName}");
            return false;
        }
        
        settings.activeProfileId = profileId;
        return true;
        
    }

    private static void DeleteRemoteTargetFolder()
    {
        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
        string remoteTargetFolder = settings.RemoteCatalogBuildPath.GetValue(settings);

        string assetsFolderPath = Application.dataPath;
        string projectRootPath = assetsFolderPath.Replace("/Assets", "");

        string remoteTargetFolderAbsolute = projectRootPath + "/" + remoteTargetFolder;

        FileUtil.DeleteFileOrDirectory(remoteTargetFolderAbsolute);
    }

    private static void DeleteAddressablesLibraryFolder()
    {
        string assetsFolderPath = Application.dataPath;
        string projectRootPath = assetsFolderPath.Replace("/Assets", "");
        string addressablesLibraryFolderAbsolute = projectRootPath + "/Library/com.unity.addressables";

        FileUtil.DeleteFileOrDirectory(addressablesLibraryFolderAbsolute);
    }

    private static void SaveCurrentAddressablesLibraryFolder()
    {
        string assetsFolderPath = Application.dataPath;
        string projectRootPath = assetsFolderPath.Replace("/Assets", "");
        string addressablesLibraryFolderAbsolute = projectRootPath + "/Library/com.unity.addressables";
        string addressablesLibraryFolderAbsoluteTemp = projectRootPath + "/Library/com.unity.addressables_temp";

        if(Directory.Exists(addressablesLibraryFolderAbsolute))
        {
            Directory.Move(addressablesLibraryFolderAbsolute, addressablesLibraryFolderAbsoluteTemp);
        }        
    }

    private static void RestorePreviousAddressablesLibraryFolder()
    {
        string assetsFolderPath = Application.dataPath;
        string projectRootPath = assetsFolderPath.Replace("/Assets", "");
        string addressablesLibraryFolderAbsolute = projectRootPath + "/Library/com.unity.addressables";
        string addressablesLibraryFolderAbsoluteTemp = projectRootPath + "/Library/com.unity.addressables_temp";

        FileUtil.DeleteFileOrDirectory(addressablesLibraryFolderAbsolute);

        if (Directory.Exists(addressablesLibraryFolderAbsoluteTemp))
        {
            Directory.Move(addressablesLibraryFolderAbsoluteTemp, addressablesLibraryFolderAbsolute);
        }
    }

    private static Dictionary<AddressableAssetGroup, bool> SaveGroupsIncludedState()
    {
        Dictionary<AddressableAssetGroup, bool> groupsIncludedState = new Dictionary<AddressableAssetGroup, bool>();
        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
        foreach (AddressableAssetGroup group in settings.groups)
        {
            BundledAssetGroupSchema bundledAssetGroupSchema = group.GetSchema<BundledAssetGroupSchema>();
            if (bundledAssetGroupSchema != null)
            {
                groupsIncludedState.Add(group, bundledAssetGroupSchema.IncludeInBuild);
            }
        }
        return groupsIncludedState;
    }

    private static void RestoreGroupsIncludedState(Dictionary<AddressableAssetGroup, bool> groupsIncludedState)
    {
        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
        foreach (AddressableAssetGroup group in settings.groups)
        {
            BundledAssetGroupSchema bundledAssetGroupSchema = group.GetSchema<BundledAssetGroupSchema>();
            if (bundledAssetGroupSchema != null)
            {
                bundledAssetGroupSchema.IncludeInBuild = groupsIncludedState[group];
            }
        }
    }

    private static void SetGroupsIncludedState(List<AddressableAssetGroup> groupsToBuild)
    {
        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
        foreach (AddressableAssetGroup group in settings.groups)
        {
            BundledAssetGroupSchema bundledAssetGroupSchema = group.GetSchema<BundledAssetGroupSchema>();
            if (bundledAssetGroupSchema != null)
            {
                bundledAssetGroupSchema.IncludeInBuild = groupsToBuild.Contains(group);
            }
        }
    }

    class GroupFilter
    {
        public string buildPath = "";
        public ContentTypeGroupSchema.ContentType? contentType = null;
        public bool? includeInServer = null;
    }

    static List<AddressableAssetGroup> GetFilteredGroups(GroupFilter filter)
    {
        List<AddressableAssetGroup> filteredGroups = new List<AddressableAssetGroup>();

        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
        foreach (AddressableAssetGroup group in settings.groups)
        {
            ContentTypeGroupSchema contentTypeGroupSchema = group.GetSchema<ContentTypeGroupSchema>();
            BundledAssetGroupSchema bundledAssetGroupSchema = group.GetSchema<BundledAssetGroupSchema>();
            IncludeInServerGroupSchema includeInServerGroupSchema = group.GetSchema<IncludeInServerGroupSchema>();

            if(filter.buildPath != "")
            {
                if(bundledAssetGroupSchema == null)
                {
                    continue;
                }
                
                string v = bundledAssetGroupSchema.BuildPath.GetName(settings);
                if (bundledAssetGroupSchema.BuildPath.GetName(settings) != filter.buildPath)
                {
                    continue;
                }
            }
            if (filter.contentType != null)
            {
                if (contentTypeGroupSchema == null || contentTypeGroupSchema.contentType != filter.contentType.Value)
                {
                    continue;
                }
            }
            if (filter.includeInServer != null)
            {
                if(includeInServerGroupSchema == null || includeInServerGroupSchema.includeInServer != filter.includeInServer.Value)
                {
                    continue;
                }
            }

            filteredGroups.Add(group);
        }
        return filteredGroups;
    }
}