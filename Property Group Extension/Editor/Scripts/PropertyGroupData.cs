using System.Collections.Generic;
using UnityEngine;

namespace Tom.PropertyGroups.Editor
{
    /// <summary>
    /// ScriptableObject used to store PropertyGroup Data
    /// 
    /// Created by Tomasz Galka | E-Mail: tommy.galk@gmail.com | Github: GomysTalka
    /// </summary>
    [System.Serializable]
    public class PropertyGroupData : ScriptableObject
    {
        public List<Data> data;
        /// <summary>
        /// Returns a list of all groups within the Data file. Used for cleaning.
        /// </summary>
        public string[] SavedGroups {
            get {
                if (data == null) return null;
                string[] groups = new string[data.Count];
                for (int i = 0; i < data.Count; i++)
                    groups[i] = data[i].groupLabel;
                return groups;
            }
        }

        /// <summary>
        /// Used to 
        /// </summary>
        /// <param name="groupName">The group to search for.</param>
        /// <returns><b>Data</b> Object if group exists, otherwise returns null.</returns>
        public Data FindGroup(string groupName) {
            if (data == null) return null;
            foreach (Data d in data) {
                if (d.groupLabel == groupName)
                    return d;
            }
            return null;
        }

        /// <summary>
        /// Add specified Data object to group database.
        /// </summary>
        /// <param name="data">The <b>Data</b> object to add.</param>
        public void AddGroup(Data data) {
            if (this.data.Contains(data)) return;
            this.data.Add(data);
        }

        /// <summary>
        /// Removes the group which matches the name specified. If the group doesn't exist, this function doesn't do anything.
        /// </summary>
        /// <param name="groupName">The group to search for.</param>
        public void RemoveGroupByName(string groupName) =>
            data.RemoveAll((a) => a.groupLabel == groupName);
    }

    /// <summary>
    /// Class used to store group information.
    /// </summary>
    [System.Serializable]
    public class Data {
        public string groupLabel;
        public bool isShown;
    }
}
