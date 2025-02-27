using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Component to add to a UI button for testing the SimpleSaveSystem
/// </summary>
public class AndroidSaveTest : MonoBehaviour
{
    public TextMeshProUGUI statusText;
    
    public void TestSimpleSaveSystem()
    {
        try
        {
            if (statusText != null)
            {
                statusText.text = "Testing SimpleSaveSystem...";
            }
            
            Debug.Log("Testing SimpleSaveSystem from UI button...");
            
            // Call the test method in SaveManager
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.TestSimpleSaveSystem();
                
                if (statusText != null)
                {
                    statusText.text = "Test completed. Check logs for results.";
                }
            }
            else
            {
                Debug.LogError("SaveManager.Instance is null");
                
                if (statusText != null)
                {
                    statusText.text = "Error: SaveManager.Instance is null";
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error testing SimpleSaveSystem: {e.Message}");
            
            if (statusText != null)
            {
                statusText.text = $"Error: {e.Message}";
            }
        }
    }
    
    public void DisplaySaveInfo()
    {
        string info = "Save System Info:\n";
        
        // Check if SimpleSaveSystem file exists
        bool simpleSaveExists = SimpleSaveSystem.SaveFileExists();
        info += $"Save file exists: {simpleSaveExists}\n";
        
        // Check if text file exists
        string txtPath = System.IO.Path.Combine(Application.persistentDataPath, "gamesave.txt");
        bool txtFileExists = System.IO.File.Exists(txtPath);
        info += $"Text file exists: {txtFileExists}\n";
        
        // Add persistent data path
        info += $"PersistentDataPath: {Application.persistentDataPath}";
        
        Debug.Log(info);
        
        if (statusText != null)
        {
            statusText.text = info;
        }
    }
    
    public void DeleteSaveFiles()
    {
        try
        {
            if (statusText != null)
            {
                statusText.text = "Deleting save files...";
            }
            
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.DeleteSave();
                
                if (statusText != null)
                {
                    statusText.text = "Save files deleted successfully.";
                }
            }
            else
            {
                Debug.LogError("SaveManager.Instance is null");
                
                if (statusText != null)
                {
                    statusText.text = "Error: SaveManager.Instance is null";
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error deleting save files: {e.Message}");
            
            if (statusText != null)
            {
                statusText.text = $"Error: {e.Message}";
            }
        }
    }
} 