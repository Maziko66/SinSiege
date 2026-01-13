using UnityEngine;
using System.IO;
using System.Security.Cryptography;
using System.Text;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance => PersistentManager.Instance.SaveManager;
    
    [Header("Settings")]
    [Tooltip("Uncheck this to edit save files manually with Notepad for testing.")]
    [SerializeField] private bool useEncryption = false;
    [SerializeField] private string fileName = "savegame.json";
    
    [Header("References")]
    [SerializeField] private GameData gameData; // Drag your ScriptableObject here

    // 32-byte Key for AES (Keep this secret!)
    private readonly string encryptionKey = "12345678901234567890123456789012"; 

    private string SavePath => Path.Combine(Application.persistentDataPath, fileName);

    private void Start()
    {
        // Optional: Auto-load on start
        // LoadGame();
    }

    public void SaveGame()
    {
        if (gameData == null)
        {
            Debug.LogError("No GameData found! Assign it in the Inspector.");
            return;
        }

        // 1. Convert Data to JSON
        string json = JsonUtility.ToJson(gameData, true);

        // 2. Write to file (Encrypted or Plain)
        if (useEncryption)
        {
            WriteEncryptedFile(json);
            Debug.Log($"<color=green>Game Saved (Encrypted)</color> to: {SavePath}");
        }
        else
        {
            File.WriteAllText(SavePath, json);
            Debug.Log($"<color=green>Game Saved (Plain Text)</color> to: {SavePath}");
        }
    }

    public void LoadGame()
    {
        if (!File.Exists(SavePath))
        {
            Debug.LogWarning("No save file found.");
            return;
        }

        try
        {
            string json = "";

            // 1. Read from file
            if (useEncryption)
            {
                json = ReadEncryptedFile();
            }
            else
            {
                json = File.ReadAllText(SavePath);
            }

            // 2. Overwrite the ScriptableObject
            if (!string.IsNullOrEmpty(json))
            {
                JsonUtility.FromJsonOverwrite(json, gameData);
                Debug.Log("<color=cyan>Game Loaded Successfully</color>");
                
                // Notify other scripts that data changed (Optional)
                // EventManager.TriggerEvent("OnGameLoaded");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load save file: {e.Message}");
        }
    }

    public void DeleteSave()
    {
        if (File.Exists(SavePath))
        {
            File.Delete(SavePath);
            gameData.ResetData(); // Ensure you have this method in your GameData script
            Debug.Log("Save file deleted.");
        }
    }

    // --- ENCRYPTION LOGIC ---

    private void WriteEncryptedFile(string json)
    {
        byte[] keyBytes = Encoding.UTF8.GetBytes(encryptionKey);
        byte[] ivBytes = new byte[16];

        using (Aes aes = Aes.Create())
        {
            aes.Key = keyBytes;
            aes.GenerateIV();
            ivBytes = aes.IV;

            using (FileStream fs = new FileStream(SavePath, FileMode.Create))
            {
                // Write the IV first (unencrypted) so we can use it to decrypt later
                fs.Write(ivBytes, 0, ivBytes.Length);

                using (ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
                using (CryptoStream cs = new CryptoStream(fs, encryptor, CryptoStreamMode.Write))
                using (StreamWriter sw = new StreamWriter(cs))
                {
                    sw.Write(json);
                }
            }
        }
    }

    private string ReadEncryptedFile()
    {
        byte[] keyBytes = Encoding.UTF8.GetBytes(encryptionKey);
        byte[] ivBytes = new byte[16];

        using (FileStream fs = new FileStream(SavePath, FileMode.Open))
        {
            // Read the IV from the start of the file
            fs.Read(ivBytes, 0, ivBytes.Length);

            using (Aes aes = Aes.Create())
            {
                aes.Key = keyBytes;
                aes.IV = ivBytes;

                using (ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                using (CryptoStream cs = new CryptoStream(fs, decryptor, CryptoStreamMode.Read))
                using (StreamReader sr = new StreamReader(cs))
                {
                    return sr.ReadToEnd();
                }
            }
        }
    }
}