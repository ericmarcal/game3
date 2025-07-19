using UnityEngine;
using System;

// Garante que este componente só possa ser adicionado uma vez e que seja executado no editor.
[ExecuteInEditMode]
[DisallowMultipleComponent]
public class UniqueID : MonoBehaviour
{
    // O campo que guarda o nosso ID único.
    [SerializeField]
    private string _id;
    public string ID => _id;

    private void Awake()
    {
        // Gera um novo ID apenas se estiver vazio.
        if (string.IsNullOrEmpty(_id))
        {
            _id = Guid.NewGuid().ToString();
        }
    }
}