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

        if (GUILayout.Button("Update remote main content"))
        {
            UpdateRemoteMainContent(new Version(Application.version));
        }

        if (GUILayout.Button("[DANGER] Build remote secondary content"))
        {
            BuildRemoteSecondaryContent(new Version(Application.version));
        }

        if (GUILayout.Button("Update remote secondary content"))
        {
            UpdateRemoteSecondaryContent(new Version(Application.version));
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
        // TO DO cleanup entire pipeline!!
        // TO DO delete library addressables folder!!

        DeleteAddressablesLibraryFolder();

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
        SaveContentStateBinFile(version, ContentTypeGroupSchema.ContentType.Main);        

        RestoreGroupsIncludedState(groupsIncludedState);
        AssetDatabase.Refresh();

        DeleteAddressablesLibraryFolder();
    }    

    public static void UpdateRemoteMainContent(Version version)
    {
        DeleteAddressablesLibraryFolder();

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

        Version previousVersionBuilt = GetPreviousVersionBuilt(version, ContentTypeGroupSchema.ContentType.Main);
        if(previousVersionBuilt == null)
        {
            Debug.LogError("Could not find a previous version Content State Bin file");
        }

        Debug.Log($"Building addressables update for Main content with base on version {previousVersionBuilt}");
        bool setSuccessfully = SetContentStateBinFile(previousVersionBuilt, ContentTypeGroupSchema.ContentType.Main);
        if (!setSuccessfully)
        {
            Debug.LogError("Could not set Content State Bin file");
        }
        else
        {
            UpdatePreviousBuild();
            SaveContentStateBinFile(version, ContentTypeGroupSchema.ContentType.Main, $"[updates{previousVersionBuilt}]");
        }

        RestoreGroupsIncludedState(groupsIncludedState);

        Debug.Log($"Built addressables update for Main content with base on version {previousVersionBuilt}");
        AssetDatabase.Refresh();

        DeleteAddressablesLibraryFolder();
    }   

    public static void BuildRemoteSecondaryContent(Version version)
    {
        DeleteAddressablesLibraryFolder();

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
        SaveContentStateBinFile(version, ContentTypeGroupSchema.ContentType.Secondary);

        RestoreGroupsIncludedState(groupsIncludedState);
        AssetDatabase.Refresh();

        DeleteAddressablesLibraryFolder();
    }

    public static void UpdateRemoteSecondaryContent(Version version)
    {
        DeleteAddressablesLibraryFolder();

        BuildVars.DeploymentEnvironment = "Dev";
        BuildVars.ContentType = "secondary";
        BuildVars.Platform = PlatformMappingService.GetPlatform().ToString();

        bool setProfileSuccess = SetProfile("SecondaryContent");
        if (!setProfileSuccess)
        {
            return;
        }

        Dictionary<AddressableAssetGroup, bool> groupsIncludedState = SaveGroupsIncludedState();

        GroupFilter groupFilter = new GroupFilter() { buildPath = AddressableAssetSettings.kRemoteBuildPath, contentType = ContentTypeGroupSchema.ContentType.Secondary };
        List<AddressableAssetGroup> groupsToBuild = GetFilteredGroups(groupFilter);
        SetGroupsIncludedState(groupsToBuild);

        Version previousVersionBuilt = GetPreviousVersionBuilt(version, ContentTypeGroupSchema.ContentType.Secondary);
        if (previousVersionBuilt == null)
        {
            Debug.LogError("Could not find a previous version Content State Bin file");
        }

        Debug.Log($"Building addressables update for Secondary content with base on version {previousVersionBuilt}");
        bool setSuccessfully = SetContentStateBinFile(previousVersionBuilt, ContentTypeGroupSchema.ContentType.Secondary);
        if (!setSuccessfully)
        {
            Debug.LogError("Could not set Content State Bin file");
        }
        else
        {
            UpdatePreviousBuild();
            SaveContentStateBinFile(version, ContentTypeGroupSchema.ContentType.Secondary, $"[updates{previousVersionBuilt}]");
        }

        RestoreGroupsIncludedState(groupsIncludedState);

        Debug.Log($"Built addressables update for Secondary content with base on version {previousVersionBuilt}");

        AssetDatabase.Refresh();

        DeleteAddressablesLibraryFolder();
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

    static void UpdatePreviousBuild()
    {
        try
        {
            string contentStateDataPath = ContentUpdateScript.GetContentStateDataPath(false);
            if (!File.Exists(contentStateDataPath))
            {
                throw new Exception("Previous Content State Data missing");
            }
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            //List<AddressableAssetEntry> modifiedEntries = ContentUpdateScript.GatherModifiedEntries(settings, contentStateDataPath);
            //ContentUpdateScript.CreateContentUpdateGroup(settings, modifiedEntries, "Content_Update");
            ContentUpdateScript.BuildContentUpdate(settings, contentStateDataPath);
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }        
    }

    static Version GetPreviousVersionBuilt(Version version, ContentTypeGroupSchema.ContentType contentType)
    {
        string rootFolderPath = Application.dataPath + $"/AddressableRemoteStates";
        if (!Directory.Exists(rootFolderPath))
        {
            Debug.LogError($"No previous remote bundles build found for content type {contentType}, version {version}");
            return null;
        }
        string[] versionFolders = Directory.GetDirectories(Application.dataPath + $"/AddressableRemoteStates", "*", System.IO.SearchOption.TopDirectoryOnly);


        Dictionary<int, List<int>> detectedVersions = new Dictionary<int, List<int>>();

        foreach (string versionFolderPath in versionFolders)
        {
            string currentPlatformFolderPath = versionFolderPath + $"/{PlatformMappingService.GetPlatform()}";
            if (!Directory.Exists(currentPlatformFolderPath))
            {
                continue;
            }

            List<string> contentTypeFolders = Directory.GetDirectories(currentPlatformFolderPath, "*", System.IO.SearchOption.TopDirectoryOnly).ToList();
            List<string> contentTypeFoldersNames = new List<string>();

            // Clean folders' names
            for (int i = 0; i < contentTypeFolders.Count; i++)
            {
                DirectoryInfo contentTypeFolderInfo = new DirectoryInfo(contentTypeFolders[i]);
                contentTypeFoldersNames.Add(contentTypeFolderInfo.Name.ToLowerInvariant());
            }

            if (!contentTypeFoldersNames.Contains(contentType.ToString().ToLowerInvariant()))
            {
                continue;
            }

            string contentFolderPath = contentTypeFolders[contentTypeFoldersNames.IndexOf(contentType.ToString().ToLowerInvariant())];

            string stateFilePath = GetFileWithNameContaining(contentFolderPath, "addressables_content_state");
            if (stateFilePath == "")
            {
                continue;
            }

            //string stateBinFilePath = contentFolderPath + "/addressables_content_state.bin";
            //if (!File.Exists(stateBinFilePath))
            //{
            //    continue;
            //}

            DirectoryInfo directoryInfo = new DirectoryInfo(versionFolderPath);
            string versionFolderName = directoryInfo.Name;

            var pointPos = versionFolderName.IndexOf('.');
            if(pointPos == -1)
            {
                Debug.LogError($"Folder {versionFolderName} found with a wrong version convention");
                continue;
            }
            int.TryParse(versionFolderName.Substring(0, pointPos), out int major);
            int.TryParse(versionFolderName.Substring(pointPos + 1, versionFolderName.Count() - (pointPos + 1)), out int minor);
            //Debug.Log($"Folder {folderName} parsed with version {major}[major].{minor}[minor]");

            if(detectedVersions.ContainsKey(major))
            {
                detectedVersions[major].Add(minor);
            }
            else
            {
                detectedVersions.Add(major, new List<int>() { minor });
            }
        }

        Version previousVersion = null;

        // Try to get the previous minor version inside the same major
        if (detectedVersions.ContainsKey(version.Major))
        {
            List<int> minorVersions = detectedVersions[version.Major];
            int previousMinorVersion = GetPreviousNumber(minorVersions, version.Minor);
            if(previousMinorVersion < version.Minor)
            {
                previousVersion = new Version(version.Major, previousMinorVersion);
            }
        }

        if(previousVersion == null)
        {
            int previousMajorVersion = GetPreviousNumber(detectedVersions.Keys.ToList(), version.Major);
            if(previousMajorVersion < version.Major)
            {
                List<int> minorVersions = detectedVersions[previousMajorVersion];
                minorVersions.Sort();
                previousVersion = new Version(previousMajorVersion, minorVersions[minorVersions.Count-1]);
            }
        }

        if (previousVersion == null)
        {
            Debug.LogError("No previous version found");
        }
        else
        {
            Debug.Log($"Previous version: {previousVersion}");
        }

        return previousVersion;
    }

    static int GetPreviousNumber(List<int> numbers, int referenceNumber)
    {
        numbers.Sort();
        int result = referenceNumber;
        foreach (int number in numbers)
        {
            if(number < referenceNumber)
            {
                result = number;
            }
        }
        return result;
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

    private static bool SetContentStateBinFile(Version version, ContentTypeGroupSchema.ContentType contentType)
    {
        string rootFolderPath = Application.dataPath + $"/AddressableRemoteStates";
        if (!Directory.Exists(rootFolderPath))
        {
            return false;
        }
        string versionFolderPath = rootFolderPath + $"/{version}";
        if (!Directory.Exists(versionFolderPath))
        {
            return false;
        }
        string platformFolderPath = versionFolderPath + $"/{PlatformMappingService.GetPlatform()}";
        if (!Directory.Exists(platformFolderPath))
        {
            return false;
        }
        string contentTypeFolderPath = platformFolderPath + $"/{contentType}";
        if (!Directory.Exists(contentTypeFolderPath))
        {
            return false;
        }

        string stateFilePath = GetFileWithNameContaining(contentTypeFolderPath, "addressables_content_state");
        if (stateFilePath == "")
        {
            return false;
        }

        string stateBinStandardPath = Application.dataPath + $"/AddressableAssetsData/{PlatformMappingService.GetPlatform()}/addressables_content_state.bin";
        if (File.Exists(stateBinStandardPath))
        {
            FileUtil.DeleteFileOrDirectory(stateBinStandardPath);
        }

        FileUtil.CopyFileOrDirectory(stateFilePath, stateBinStandardPath);

        return true;
    }

    private static void SaveContentStateBinFile(Version version, ContentTypeGroupSchema.ContentType contentType, string suffix = "")
    {
        string rootFolderPath = Application.dataPath + $"/AddressableRemoteStates";
        if (!Directory.Exists(rootFolderPath))
        {
            Directory.CreateDirectory(rootFolderPath);
        }
        string versionFolderPath = rootFolderPath + $"/{version}";
        if (!Directory.Exists(versionFolderPath))
        {
            Directory.CreateDirectory(versionFolderPath);
        }
        string platformFolderPath = versionFolderPath + $"/{PlatformMappingService.GetPlatform()}";
        if (!Directory.Exists(platformFolderPath))
        {
            Directory.CreateDirectory(platformFolderPath);
        }
        string contentFolderPath = platformFolderPath + $"/{contentType}";
        if (!Directory.Exists(contentFolderPath))
        {
            Directory.CreateDirectory(contentFolderPath);
        }

        string existingContentStateBin = GetFileWithNameContaining(contentFolderPath, "addressables_content_state");
        if (existingContentStateBin != "")
        {
            FileUtil.DeleteFileOrDirectory(existingContentStateBin);
        }

        string stateBinStandardPath = Application.dataPath + $"/AddressableAssetsData/{PlatformMappingService.GetPlatform()}/addressables_content_state.bin";
        string targetPath = contentFolderPath + $"/addressables_content_state{suffix}.bin";
        FileUtil.CopyFileOrDirectory(stateBinStandardPath, targetPath);
    }

    static string GetFileWithNameContaining(string folderPath, string partialName)
    {
        List<string> filePaths = Directory.GetFiles(folderPath).ToList();
        foreach (string filePath in filePaths)
        {
            string fileName = Path.GetFileName(filePath);
            if(fileName.Contains(partialName))
            {
                return filePath;
            }
        }
        return "";
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

    void StripAllPrefabsFromAssets()
    {
        AssetDatabase.FindAssets("t:prefab ");
    }

    void RestoreAllStrippedPrefabs()
    {

    }
}