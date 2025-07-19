using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GameData
{
    // Vamos guardar o estado de cada objeto 'ISavable' num dicionário.
    // A 'string' será um identificador único para o objeto (ex: "player_transform", "farming_manager_data").
    // O 'object' será os dados guardados por esse objeto.
    public Dictionary<string, object> savedStates;

    // Construtor para criar um novo estado de jogo
    public GameData()
    {
        savedStates = new Dictionary<string, object>();
    }
}