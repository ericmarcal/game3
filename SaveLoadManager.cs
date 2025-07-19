using UnityEngine;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;

public class SaveLoadManager : MonoBehaviour
{
    public static SaveLoadManager instance;

    private readonly List<ISavable> savableObjects = new List<ISavable>();

    private string saveFilePath;
    private GameData gameData;

    private void Awake()
    {
        if (instance == null) { instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); return; }

        saveFilePath = Path.Combine(Application.persistentDataPath, "gameData.json");
    }

    public void RegisterSavable(ISavable savable)
    {
        if (!savableObjects.Contains(savable))
        {
            savableObjects.Add(savable);
        }
    }

    public void UnregisterSavable(ISavable savable)
    {
        savableObjects.Remove(savable);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F5)) SaveGame();
        if (Input.GetKeyDown(KeyCode.F9)) LoadGame();
    }

    public void NewGame()
    {
        this.gameData = new GameData();
        Debug.Log("Novo Jogo iniciado. Os valores padrão serão usados.");
    }

    public void SaveGame()
    {
        Debug.Log($"<color=green>SAVE:</color> A iniciar processo de save para {savableObjects.Count} objetos registados.");

        if (this.gameData == null) { this.gameData = new GameData(); }
        gameData.savedStates.Clear();

        // << MUDANÇA PRINCIPAL: Iterar sobre uma cópia da lista >>
        foreach (ISavable savable in new List<ISavable>(savableObjects))
        {
            var savableMonoBehaviour = savable as MonoBehaviour;
            if (savableMonoBehaviour != null && savableMonoBehaviour.gameObject.activeInHierarchy)
            {
                gameData.savedStates[savable.ID] = savable.CaptureState();
            }
        }

        string json = JsonConvert.SerializeObject(gameData, Formatting.Indented);
        File.WriteAllText(saveFilePath, json);
        Debug.Log("<color=green>SAVE:</color> Jogo guardado com sucesso!");
    }

    public void LoadGame()
    {
        if (!File.Exists(saveFilePath))
        {
            Debug.LogWarning("Nenhum ficheiro de save para carregar.");
            return;
        }

        string json = File.ReadAllText(saveFilePath);
        this.gameData = JsonConvert.DeserializeObject<GameData>(json);

        if (gameData.savedStates == null)
        {
            Debug.LogError("LOAD ERROR: O ficheiro de save parece estar corrompido.");
            return;
        }

        Debug.Log($"<color=cyan>LOAD:</color> A carregar estado para {savableObjects.Count} objetos registados.");

        // << MUDANÇA PRINCIPAL: Iterar sobre uma cópia da lista >>
        foreach (ISavable savable in new List<ISavable>(savableObjects))
        {
            if (gameData.savedStates.TryGetValue(savable.ID, out object savedState))
            {
                savable.RestoreState(savedState);
            }
        }
        Debug.Log("<color=cyan>LOAD:</color> Jogo carregado com sucesso!");
    }
}