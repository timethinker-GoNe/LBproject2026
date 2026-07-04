using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;

namespace DialogueQuests
{
    /// <summary>
    /// Script to write a class to the disk, or to read a file containing class from the disk.
    /// Uses JSON (Newtonsoft.Json) for new saves. Falls back to legacy Binary format for old save files.
    /// </summary>

    [System.Serializable]
    public class SaveTool
    {
        // Load any file to a class. Tries JSON first, falls back to legacy binary for old save files.
        public static T LoadFile<T>(string filename) where T : class
        {
            T data = null;
            string fullpath = Application.persistentDataPath + "/" + filename;
            if (IsValidFilename(filename) && File.Exists(fullpath))
            {
                // Try JSON first (new format)
                try
                {
                    string json = File.ReadAllText(fullpath);
                    data = JsonConvert.DeserializeObject<T>(json);
                }
                catch (System.Exception)
                {
                    // Fall back to legacy binary format for old save files
                    data = LoadFileBinary<T>(fullpath);
                    if (data != null)
                        Debug.Log("[SaveTool-DQ] Loaded legacy binary save: " + filename + ". Will convert to JSON on next save.");
                    else
                        Debug.LogWarning("[SaveTool-DQ] Failed to load save file: " + filename);
                }
            }
            return data;
        }

        // Legacy binary loader — only used as migration fallback
        private static T LoadFileBinary<T>(string fullpath) where T : class
        {
            T data = null;
            FileStream file = null;
            try
            {
#pragma warning disable SYSLIB0011
                var bf = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                file = File.Open(fullpath, FileMode.Open);
                data = (T)bf.Deserialize(file);
                file.Close();
#pragma warning restore SYSLIB0011
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[SaveTool-DQ] Binary fallback also failed: " + e.Message);
                if (file != null) file.Close();
            }
            return data;
        }

        // Save any class to a file as JSON
        public static void SaveFile<T>(string filename, T data) where T : class
        {
            if (IsValidFilename(filename))
            {
                try
                {
                    string fullpath = Application.persistentDataPath + "/" + filename;
                    string json = JsonConvert.SerializeObject(data, Formatting.Indented);
                    File.WriteAllText(fullpath, json);
                }
                catch (System.Exception e) { Debug.LogWarning("[SaveTool-DQ] Error Saving Data: " + e.Message); }
            }
        }

        public static void DeleteFile(string filename)
        {
            string fullpath = Application.persistentDataPath + "/" + filename;
            if (File.Exists(fullpath))
                File.Delete(fullpath);
        }

        // Return all save files with the given extension
        public static List<string> GetAllSave(string extension = "")
        {
            List<string> saves = new List<string>();
            string[] files = Directory.GetFiles(Application.persistentDataPath);
            foreach (string file in files)
            {
                if (file.EndsWith(extension))
                {
                    string filename = Path.GetFileName(file);
                    if (!saves.Contains(filename))
                        saves.Add(filename);
                }
            }
            return saves;
        }

        public static bool DoesFileExist(string filename)
        {
            string fullpath = Application.persistentDataPath + "/" + filename;
            return IsValidFilename(filename) && File.Exists(fullpath);
        }

        public static bool IsValidFilename(string filename)
        {
            if (string.IsNullOrWhiteSpace(filename))
                return false;

            foreach (char c in Path.GetInvalidFileNameChars())
            {
                if (filename.Contains(c.ToString()))
                    return false;
            }
            return true;
        }
    }
}
