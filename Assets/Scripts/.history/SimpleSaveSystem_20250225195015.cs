using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

/// <summary>
/// A simplified save system that uses BinaryFormatter for Android
/// Based on a working implementation from another game
/// </summary>
public static class SimpleSaveSystem
{
    private static string GetSaveFilePath()
    {
        return Path.Combine(Application.persistentDataPath, "gamesave.dat");
    }
    
    private static string GetBackupFilePath()
    {
        return Path.Combine(Application.persistentDataPath, "gamesave.dat.bak");
    }
    
    public static void Save(SaveData data)
    {
        try
        {
            Debug.Log($"SimpleSaveSystem: Saving game to {GetSaveFilePath()}");
            
            // Create backup of existing save if it exists
            if (File.Exists(GetSaveFilePath()))
            {
                if (File.Exists(GetBackupFilePath()))
                {
                    File.Delete(GetBackupFilePath());
                }
                File.Copy(GetSaveFilePath(), GetBackupFilePath());
                Debug.Log($"SimpleSaveSystem: Created backup at {GetBackupFilePath()}");
            }
            
            // Save to a temporary file first
            string tempPath = GetSaveFilePath() + ".tmp";
            
            using (FileStream stream = new FileStream(tempPath, FileMode.Create))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(stream, data);
            }
            
            // If temp file was created successfully, replace the actual save file
            if (File.Exists(tempPath))
            {
                if (File.Exists(GetSaveFilePath()))
                {
                    File.Delete(GetSaveFilePath());
                }
                File.Move(tempPath, GetSaveFilePath());
                Debug.Log($"SimpleSaveSystem: Game saved successfully. File size: {new FileInfo(GetSaveFilePath()).Length} bytes");
            }
            else
            {
                Debug.LogError("SimpleSaveSystem: Failed to create temporary save file");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"SimpleSaveSystem: Error saving game: {e.Message}\nStack trace: {e.StackTrace}");
        }
    }
    
    public static SaveData Load()
    {
        try
        {
            string filePath = GetSaveFilePath();
            Debug.Log($"SimpleSaveSystem: Loading game from {filePath}");
            
            if (File.Exists(filePath))
            {
                Debug.Log($"SimpleSaveSystem: Save file exists. Size: {new FileInfo(filePath).Length} bytes");
                
                try
                {
                    using (FileStream stream = new FileStream(filePath, FileMode.Open))
                    {
                        BinaryFormatter formatter = new BinaryFormatter();
                        SaveData data = formatter.Deserialize(stream) as SaveData;
                        
                        if (data != null)
                        {
                            Debug.Log("SimpleSaveSystem: Game loaded successfully");
                            return data;
                        }
                        else
                        {
                            throw new System.Exception("Deserialized data is null");
                        }
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"SimpleSaveSystem: Error loading save file: {e.Message}");
                    
                    // Try to recover from backup
                    string backupPath = GetBackupFilePath();
                    if (File.Exists(backupPath))
                    {
                        Debug.Log("SimpleSaveSystem: Attempting to recover from backup file");
                        
                        try
                        {
                            using (FileStream stream = new FileStream(backupPath, FileMode.Open))
                            {
                                BinaryFormatter formatter = new BinaryFormatter();
                                SaveData data = formatter.Deserialize(stream) as SaveData;
                                
                                if (data != null)
                                {
                                    Debug.Log("SimpleSaveSystem: Successfully recovered from backup");
                                    
                                    // Save the recovered data back to the main file
                                    Save(data);
                                    return data;
                                }
                            }
                        }
                        catch (System.Exception backupEx)
                        {
                            Debug.LogError($"SimpleSaveSystem: Failed to recover from backup: {backupEx.Message}");
                        }
                    }
                    
                    // If we get here, both main file and backup failed
                    throw;
                }
            }
            
            Debug.Log("SimpleSaveSystem: No save file found, returning new SaveData");
            return new SaveData();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"SimpleSaveSystem: Failed to load game: {e.Message}\nStack trace: {e.StackTrace}");
            return new SaveData();
        }
    }
    
    public static bool SaveFileExists()
    {
        return File.Exists(GetSaveFilePath());
    }
    
    public static void DeleteSave()
    {
        try
        {
            string filePath = GetSaveFilePath();
            string backupPath = GetBackupFilePath();
            
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                Debug.Log($"SimpleSaveSystem: Deleted save file at {filePath}");
            }
            
            if (File.Exists(backupPath))
            {
                File.Delete(backupPath);
                Debug.Log($"SimpleSaveSystem: Deleted backup file at {backupPath}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"SimpleSaveSystem: Error deleting save files: {e.Message}");
        }
    }
    
    public static void VerifyFilePermissions()
    {
        try
        {
            string testFilePath = Path.Combine(Application.persistentDataPath, "test_permissions.dat");
            File.WriteAllText(testFilePath, "Testing file permissions");
            
            if (File.Exists(testFilePath))
            {
                Debug.Log("SimpleSaveSystem: Successfully wrote test file, permissions OK");
                File.Delete(testFilePath);
            }
            else
            {
                Debug.LogError("SimpleSaveSystem: Failed to write test file, permissions may be an issue");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"SimpleSaveSystem: Error testing file permissions: {e.Message}");
        }
    }
} 