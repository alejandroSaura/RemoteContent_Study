using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace ContentUpdating
{
    public class AddressablesBuildWindow : EditorWindow
    {
        enum groupBuildlocation
        {
            LOCAL,
            REMOTE
        }

        static string m_deploymentEnvironment = "Dev";

        static string m_localCDNProfileName = "LocalCDN";
        static string m_remoteCDNProfileName = "RemoteCDN";

        [MenuItem("Tools/Addressables Build")]
        public static void ShowWindow()
        {
            GetWindow(typeof(AddressablesBuildWindow));
        }

        void OnGUI()
        {
            if (GUILayout.Button("Build local content"))
            {
                BuildLocalGroupsOnly();
            }

            if (GUILayout.Button("Build remote content (Local CDN)"))
            {
                BuildRemoteContent(m_localCDNProfileName);
            }

            if (GUILayout.Button("Build remote content (Remote CDN)"))
            {
                BuildRemoteContent(m_remoteCDNProfileName);
            }

            if (GUILayout.Button("Build all groups forced as local (for local debug builds)"))
            {
                BuildAllGroupsForcedAslocal();
            }

            if (GUILayout.Button("Build server flagged groups forced as local (for server builds)"))
            {
                BuildServerGroups();
            }
        }

        public async static void BuildLocalGroupsOnly()
        {
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
            StripMaterialsAndMeshesFromPrefabs();
            Build();
            settings.BuildRemoteCatalog = true;

            RestoreMaterialsAndMeshesFromPrefabs();
            RestoreGroupsIncludedState(groupsIncludedState);
            AssetDatabase.Refresh();
        }        

        private void BuildAllGroupsForcedAslocal()
        {           
            bool setProfileSuccess = SetProfile("Default");
            if (!setProfileSuccess)
            {
                return;
            }

            Dictionary<AddressableAssetGroup, bool> groupsIncludedState = SaveGroupsIncludedState();           

            GroupFilter groupFilter = new GroupFilter() {  };
            List<AddressableAssetGroup> groupsToBuild = GetFilteredGroups(groupFilter);
            SetGroupsIncludedState(groupsToBuild);

            Dictionary<AddressableAssetGroup, GroupRemoteState> groupsRemoteState = SaveGroupsRemoteState();
            ForceLocalBuildPath(groupsToBuild);

            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            settings.BuildRemoteCatalog = false;
            StripMaterialsAndMeshesFromPrefabs();
            Build();
            settings.BuildRemoteCatalog = true;

            RestoreMaterialsAndMeshesFromPrefabs();
            RestoreGroupsIncludedState(groupsIncludedState);
            RestoreGroupsRemoteState(groupsRemoteState);
            AssetDatabase.Refresh();
        }

        private void BuildServerGroups()
        {
            bool setProfileSuccess = SetProfile("Default");
            if (!setProfileSuccess)
            {
                return;
            }

            Dictionary<AddressableAssetGroup, bool> groupsIncludedState = SaveGroupsIncludedState();

            GroupFilter groupFilter = new GroupFilter() { includeInServer = true };
            List<AddressableAssetGroup> groupsToBuild = GetFilteredGroups(groupFilter);
            SetGroupsIncludedState(groupsToBuild);

            Dictionary<AddressableAssetGroup, GroupRemoteState> groupsRemoteState = SaveGroupsRemoteState();
            ForceLocalBuildPath(groupsToBuild);

            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            settings.BuildRemoteCatalog = false;
            StripMaterialsAndMeshesFromPrefabs();
            Build();
            settings.BuildRemoteCatalog = true;

            RestoreMaterialsAndMeshesFromPrefabs();
            RestoreGroupsIncludedState(groupsIncludedState);
            RestoreGroupsRemoteState(groupsRemoteState);
            AssetDatabase.Refresh();
        }

        public async static void BuildRemoteContent(string profileName)
        {
            SaveCurrentAddressablesLibraryFolder();

            BuildVars.DeploymentEnvironment = m_deploymentEnvironment;
            BuildVars.Platform = PlatformMappingService.GetPlatform().ToString();

            bool setProfileSuccess = SetProfile(profileName);
            if (!setProfileSuccess)
            {
                return;
            }

            Dictionary<AddressableAssetGroup, bool> groupsIncludedState = SaveGroupsIncludedState();

            GroupFilter groupFilter = new GroupFilter() { buildPath = AddressableAssetSettings.kRemoteBuildPath };
            List<AddressableAssetGroup> groupsToBuild = GetFilteredGroups(groupFilter);
            SetGroupsIncludedState(groupsToBuild);

            DeleteRemoteTargetFolder();

            StripMaterialsAndMeshesFromPrefabs();
            await Task.Yield();

            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            settings.BuildRemoteCatalog = true;
            Build();

            RestoreMaterialsAndMeshesFromPrefabs();

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
            catch (Exception e)
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

            if (Directory.Exists(addressablesLibraryFolderAbsolute))
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

        class GroupRemoteState
        {
            public string buildPath;
            public string loadPath;
        }

        private static Dictionary<AddressableAssetGroup, GroupRemoteState> SaveGroupsRemoteState()
        {
            Dictionary<AddressableAssetGroup, GroupRemoteState> groupsRemoteState = new Dictionary<AddressableAssetGroup, GroupRemoteState>();
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            foreach (AddressableAssetGroup group in settings.groups)
            {
                BundledAssetGroupSchema bundledAssetGroupSchema = group.GetSchema<BundledAssetGroupSchema>();
                if (bundledAssetGroupSchema != null)
                {
                    groupsRemoteState.Add(group, new GroupRemoteState() { buildPath = bundledAssetGroupSchema.BuildPath.GetName(settings), loadPath = bundledAssetGroupSchema.LoadPath.GetName(settings) });
                }
            }
            return groupsRemoteState;
        }

        private static void RestoreGroupsRemoteState(Dictionary<AddressableAssetGroup, GroupRemoteState> groupsRemoteState)
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            foreach (AddressableAssetGroup group in settings.groups)
            {
                BundledAssetGroupSchema bundledAssetGroupSchema = group.GetSchema<BundledAssetGroupSchema>();
                if (bundledAssetGroupSchema != null)
                {
                    bundledAssetGroupSchema.BuildPath.SetVariableByName(settings, groupsRemoteState[group].buildPath);
                    bundledAssetGroupSchema.LoadPath.SetVariableByName(settings, groupsRemoteState[group].loadPath);
                }
            }
        }

        private static void ForceLocalBuildPath(List<AddressableAssetGroup> groupsToForce)
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            foreach (AddressableAssetGroup group in groupsToForce)
            {
                BundledAssetGroupSchema bundledAssetGroupSchema = group.GetSchema<BundledAssetGroupSchema>();
                if (bundledAssetGroupSchema != null)
                {
                    bundledAssetGroupSchema.BuildPath.SetVariableByName(settings, AddressableAssetSettings.kLocalBuildPath);
                    bundledAssetGroupSchema.LoadPath.SetVariableByName(settings, AddressableAssetSettings.kLocalLoadPath);
                }
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
            List<AddressableAssetGroup> excludedGroups = new List<AddressableAssetGroup>();

            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            foreach (AddressableAssetGroup group in settings.groups)
            {
                BundledAssetGroupSchema bundledAssetGroupSchema = group.GetSchema<BundledAssetGroupSchema>();
                if (bundledAssetGroupSchema != null)
                {
                    bundledAssetGroupSchema.IncludeInBuild = groupsToBuild.Contains(group);
                    if (!bundledAssetGroupSchema.IncludeInBuild)
                    {
                        excludedGroups.Add(group);
                    }
                }
            }
        }

        class GroupFilter
        {
            public string buildPath = "";
            public bool? includeInServer = null;
        }

        static List<AddressableAssetGroup> GetFilteredGroups(GroupFilter filter)
        {
            List<AddressableAssetGroup> filteredGroups = new List<AddressableAssetGroup>();

            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            foreach (AddressableAssetGroup group in settings.groups)
            {
                BundledAssetGroupSchema bundledAssetGroupSchema = group.GetSchema<BundledAssetGroupSchema>();
                IncludeInServerGroupSchema includeInServerGroupSchema = group.GetSchema<IncludeInServerGroupSchema>();

                if (filter.buildPath != "")
                {
                    if (bundledAssetGroupSchema == null)
                    {
                        continue;
                    }

                    string v = bundledAssetGroupSchema.BuildPath.GetName(settings);
                    if (bundledAssetGroupSchema.BuildPath.GetName(settings) != filter.buildPath)
                    {
                        continue;
                    }
                }
                if (filter.includeInServer != null) // Only exclude when the group has the component and is different than our filter's value
                {
                    if (includeInServerGroupSchema != null && includeInServerGroupSchema.includeInServer != filter.includeInServer.Value)
                    {
                        continue;
                    }
                }

                filteredGroups.Add(group);
            }
            return filteredGroups;
        }

        static List<IAddressablesLoader> GetLoadersFromAllPrefabs()
        {
            List<IAddressablesLoader> addressablesLoaders = new List<IAddressablesLoader>();

            string[] guids = AssetDatabase.FindAssets("t:prefab");
            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                //addressablesLoaders.AddRange(prefab.GetComponents<IAddressablesLoader>().ToList());
                addressablesLoaders.AddRange(prefab.GetComponentsInChildren<IAddressablesLoader>().ToList());
            }

            return addressablesLoaders;
        }

        static void StripMaterialsAndMeshesFromPrefabs()
        {
            List<IAddressablesLoader> addressablesLoaders = GetLoadersFromAllPrefabs();
            foreach (var loader in addressablesLoaders)
            {
                loader.SetEditorPrefabPreview(false);
                EditorUtility.SetDirty(loader as Component);
            }
        }

        static void RestoreMaterialsAndMeshesFromPrefabs()
        {
            List<IAddressablesLoader> addressablesLoaders = GetLoadersFromAllPrefabs();
            foreach (var loader in addressablesLoaders)
            {
                loader.SetEditorPrefabPreview(true);
            }
        }
    }
}