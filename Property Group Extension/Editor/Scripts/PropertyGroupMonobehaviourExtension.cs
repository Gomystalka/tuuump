using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Reflection;
using System.Text;
using Tom.PropertyGroups.Runtime;

/// <summary>
/// Class used for overriding the Inspector of all Monobehaviours
/// This class parses the PropertyGroupAttribute and sorts variables by group.
/// The states of each tick box persist
/// 
/// Created by Tomasz Galka | E-Mail: tommy.galk@gmail.com | Github: GomysTalka
/// </summary>

namespace Tom.PropertyGroups.Editor
{
    [System.Serializable]
    [CustomEditor(typeof(MonoBehaviour), true)]
    public class PropertyGroupMonobehaviourExtension : UnityEditor.Editor
    {
        private const string kDataFilePath = "Assets/Property Group Extension/Editor/Settings/PropertyGroupProfile.asset";
        public static PropertyGroupData CurrentData { get; private set; }

        public List<string> taggedProperties;
        public List<PropertyGroup> propertyGroups;

        public StringBuilder groupPrefText;

        private void Awake()
        {
            EditorApplication.quitting -= OnQuit;
            EditorApplication.quitting += OnQuit;
        }

        private void OnQuit()
        {
            if(CurrentData)
                EditorUtility.SetDirty(CurrentData);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private void OnEnable()
        {
            if (taggedProperties == null || propertyGroups == null)
            {

                taggedProperties = new List<string>();
                propertyGroups = new List<PropertyGroup>();
            }
            FindTaggedProperties();
            if(!CurrentData)
                LoadData();
        }

        private void OnDisable()
        {
            if (CurrentData)
            {
                EditorUtility.SetDirty(CurrentData);
                //AssetDatabase.SaveAssets();
                //AssetDatabase.Refresh();
            }
        }

        public override void OnInspectorGUI()
        {
            DrawPropertiesExcluding(serializedObject, taggedProperties.ToArray());
            if (propertyGroups == null) return;

            for (int i = 0; i < propertyGroups.Count; i++)
            {
                PropertyGroup group = propertyGroups[i];
                bool shown = false;
                string fullKey = $"{group.groupLabel}§{target.GetType()}";
                Data propData = CurrentData.FindGroup(fullKey);
                if (propData != null)
                    shown = propData.isShown;
                else
                {
                    propData = new Data() { 
                        groupLabel = fullKey
                    };
                    CurrentData.AddGroup(propData);
                }
                if (group.fieldsInGroup == null) continue;
                EditorGUI.BeginChangeCheck();
                if (shown = GUILayout.Toggle(shown, group.groupLabel))
                {
                    foreach (string fieldName in group.fieldsInGroup)
                    {
                        if (serializedObject.FindProperty(fieldName) is SerializedProperty prop)
                            EditorGUILayout.PropertyField(prop);
                    }
                }
                propData.isShown = shown;
                if (EditorGUI.EndChangeCheck() && CurrentData)
                    EditorUtility.SetDirty(CurrentData);

                //EditorPrefs.SetBool(fullKey, shown);
            }
            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// Finds all properties on the Monobehaviour with the PropertyGroupAttribute
        /// </summary>
        private void FindTaggedProperties()
        {
            FieldInfo[] fields = target.GetType().GetFields();
            foreach (FieldInfo info in fields)
            {
                if (System.Attribute.GetCustomAttribute(info, typeof(PropertyGroupAttribute)) is PropertyGroupAttribute attr)
                {
                    if (string.IsNullOrEmpty(attr.GroupLabel) || string.IsNullOrWhiteSpace(attr.GroupLabel)) {
                        Debug.LogWarning($"A PropertyGroup with an empty label has been created in {target.GetType()}! This is not allowed. The group will not function.");
                        continue;
                    }
                    if (attr.GroupLabel.Contains("§")) {
                        Debug.LogWarning("The reserved character [§] cannot be used in PropertyGroup labels! The group will not function.");
                        continue;
                    }
                    if (taggedProperties.Contains(info.Name)) continue;
                    taggedProperties.Add(info.Name);
                    CreateGroupOrAddFieldToExistingGroup(attr.GroupLabel, info.Name);
                }
            }
            CleanCurrentPropertyGroupData();
            //CleanEditorPrefs();
        }

        /// <summary>
        /// Purges the PropertyGroupProfile of all deleted groups.
        /// </summary>
        private void CleanCurrentPropertyGroupData() {
            if (!CurrentData) return;
            string[] groupsInDataFile = CurrentData.SavedGroups;
            if (groupsInDataFile == null) return;

            for (int i = 0; i < groupsInDataFile.Length; i++) {
                string group = groupsInDataFile[i];

                int index = group.IndexOf('§');
                if (index <= 0) continue;

                string type = group.Substring(index + 1);
                string groupOnly = group.Substring(0, index);
                PropertyGroup gr = FindGroup(groupOnly);
                if (gr == null && type == target.GetType().ToString()) {
                    //Debug.Log($"Removed group: {group}");
                    CurrentData.RemoveGroupByName(group);
                }
                    //CurrentData.data.RemoveAt(i);
            }
            EditorUtility.SetDirty(CurrentData);
        }

        /// <summary>
        /// Cleans the Editor Prefs if groups are deleted to keep the registry clean.
        /// </summary>
        [System.Obsolete("Obsolete due to the creation of messy registry keys. Replaced with Scriptable Object.")]
        private void CleanEditorPrefs()
        {
            if (groupPrefText == null) groupPrefText = new StringBuilder();
            string fullKey = $"C::{target.GetType()}::";
            if (EditorPrefs.HasKey(fullKey) && !string.IsNullOrEmpty(EditorPrefs.GetString(fullKey)))
                groupPrefText.Append(EditorPrefs.GetString(fullKey));
            else
            {
                for (int i = 0; i < propertyGroups.Count; i++)
                    groupPrefText.Append($"{propertyGroups[i].groupLabel}{(i == propertyGroups.Count - 1 ? "" : "\n")}");
                EditorPrefs.SetString(fullKey, groupPrefText.ToString());
            }
            string[] props = groupPrefText.ToString().Split('\n');
            if (props.Length == 0 && string.IsNullOrEmpty(props[0]))
            {
                Debug.Log("All groups exist. No cleaning required.");
                return;
            }
            groupPrefText.Clear();
            for (int p = 0; p < props.Length; p++)
            {
                string prop = props[p];
                if (FindGroup(prop) == null)
                {
                    string groupKey = $"{prop}:{target.GetType()}";
                    if (EditorPrefs.HasKey(groupKey))
                        EditorPrefs.DeleteKey(groupKey);
                }
                else
                    groupPrefText.Append($"{prop}{(p == propertyGroups.Count - 1 ? "" : "\n")}");
            }
            EditorPrefs.SetString(fullKey, groupPrefText.ToString());
        }

        /// <summary>
        /// Used to create a new Property Group. If called with a group which already exists, the field will be added to the existing group and the existing group will not be reset.
        /// </summary>
        /// <param name="groupName">The name of the group to create/add to.</param>
        /// <param name="fieldName">The name of the field to be added to the group.</param>
        private void CreateGroupOrAddFieldToExistingGroup(string groupName, string fieldName)
        {
            PropertyGroup group = null;

            group = FindGroup(groupName);
            bool groupExists = true;
            if (group == null)
            {
                group = new PropertyGroup()
                {
                    groupLabel = groupName
                };
                groupExists = false;
            }

            if (group.fieldsInGroup == null)
                group.fieldsInGroup = new List<string>();

            if (!group.fieldsInGroup.Contains(fieldName))
                group.fieldsInGroup.Add(fieldName);

            if (!groupExists)
            {
                if (propertyGroups == null) propertyGroups = new List<PropertyGroup>();
                propertyGroups.Add(group);
            }
        }

        /// <summary>
        /// Used to Find a PropertyGroup by name. 
        /// </summary>
        /// <param name="groupName">The name of the group to search for.</param>
        /// <returns>PropertyGroup object if group exists, otherwise null.</returns>
        private PropertyGroup FindGroup(string groupName)
        {
            if (propertyGroups == null | propertyGroups.Count == 0) return null;
            foreach (PropertyGroup group in propertyGroups)
            {
                //Debug.Log($"Query: {groupName} -> Current: {group.groupLabel} -> Found: [{groupName == group.groupLabel}]");
                if (group == null) continue;
                if (group.groupLabel == groupName)
                    return group;
            }
            return null;
        }

        /// <summary>
        /// Loads PropertyGroup profile for the current session. A new profile is created if one doesn't already exist.
        /// </summary>
        private void LoadData()
        {
            CurrentData = AssetDatabase.LoadAssetAtPath<PropertyGroupData>(kDataFilePath);
            if (!CurrentData)
            {
                //Debug.Log("PropertyGroup Data could not be loaded. A new data file will be created.");
                CurrentData = CreateInstance<PropertyGroupData>();

                AssetDatabase.CreateAsset(CurrentData, kDataFilePath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }
    }

    /// <summary>
    /// Used to store information about the group.
    /// </summary>
    [System.Serializable]
    public class PropertyGroup
    {
        public string groupLabel;
        public List<string> fieldsInGroup;
    }
}