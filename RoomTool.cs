using UnityEngine;
using UnityEditor;
using System.Linq;

public class RoomTool : EditorWindow
{
    private Room roomA;
    private Room roomB;

    [MenuItem("Tools/Room Tool (Unified)")]
    public static void ShowWindow()
    {
        GetWindow<RoomTool>("Room Tool");
    }

    private void OnGUI()
    {
        GUILayout.Label("Ferramenta de Gestão de Salas", EditorStyles.boldLabel);

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("1. Ferramentas de Sala Única", EditorStyles.boldLabel);
        roomA = (Room)EditorGUILayout.ObjectField("Sala Selecionada", roomA, typeof(Room), true);

        if (roomA == null)
        {
            GUI.enabled = false;
        }

        if (GUILayout.Button("Alinhar Transições Filhas (IMPORTANTE)"))
        {
            AlignChildTransitions(roomA);
        }

        GUI.enabled = true;
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("2. Ferramentas de Duas Salas", EditorStyles.boldLabel);
        roomB = (Room)EditorGUILayout.ObjectField("Sala Alvo (Para Mover)", roomB, typeof(Room), true);

        if (roomA == null || roomB == null)
        {
            GUI.enabled = false;
        }

        if (GUILayout.Button("Alinhar e Ligar (B à Direita de A)"))
        {
            AlignAndLinkRooms(roomA, roomB, "East", "West");
        }
        if (GUILayout.Button("Alinhar e Ligar (B à Esquerda de A)"))
        {
            AlignAndLinkRooms(roomA, roomB, "West", "East");
        }
        if (GUILayout.Button("Alinhar e Ligar (B em Cima de A)"))
        {
            AlignAndLinkRooms(roomA, roomB, "North", "South");
        }
        if (GUILayout.Button("Alinhar e Ligar (B em Baixo de A)"))
        {
            AlignAndLinkRooms(roomA, roomB, "South", "North");
        }

        GUI.enabled = true;
    }

    // << MÉTODO ATUALIZADO COM POSICIONAMENTO PRECISO >>
    private void AlignChildTransitions(Room room)
    {
        if (room == null) return;
        BoxCollider2D roomBounds = room.GetComponent<BoxCollider2D>();
        if (roomBounds == null) { Debug.LogError("O objeto Room não tem um BoxCollider2D!", room); return; }

        var transitions = room.GetComponentsInChildren<RoomTransition>();
        Undo.RecordObjects(transitions.Select(t => t.transform).ToArray(), "Alinhar Transições Filhas");

        foreach (var t in transitions)
        {
            // Move o próprio GameObject da transição para a borda correta.
            Vector3 newLocalPos = Vector3.zero;
            switch (t.gameObject.name)
            {
                case "Transition_North": newLocalPos.y = roomBounds.size.y / 2f; break;
                case "Transition_South": newLocalPos.y = -roomBounds.size.y / 2f; break;
                case "Transition_East": newLocalPos.x = roomBounds.size.x / 2f; break;
                case "Transition_West": newLocalPos.x = -roomBounds.size.x / 2f; break;
            }
            t.transform.localPosition = newLocalPos;

            // O colisor da transição agora fica centrado no seu próprio objeto.
            var tCollider = t.GetComponent<BoxCollider2D>();
            tCollider.offset = Vector2.zero;
            switch (t.gameObject.name)
            {
                case "Transition_North":
                case "Transition_South":
                    tCollider.size = new Vector2(roomBounds.size.x, 0.1f); // Fino na vertical
                    break;
                case "Transition_East":
                case "Transition_West":
                    tCollider.size = new Vector2(0.1f, roomBounds.size.y); // Fino na horizontal
                    break;
            }
        }
        Debug.Log($"Transições para '{room.name}' alinhadas e posicionadas.", room);
    }

    private void AlignAndLinkRooms(Room referenceRoom, Room roomToMove, string refDirection, string moveDirection)
    {
        // Esta lógica já estava correta.
        BoxCollider2D refCollider = referenceRoom.GetComponent<BoxCollider2D>();
        BoxCollider2D moveCollider = roomToMove.GetComponent<BoxCollider2D>();
        if (refCollider == null || moveCollider == null) { Debug.LogError("Ambas as salas precisam de um BoxCollider2D."); return; }

        Undo.RecordObject(roomToMove.transform, "Align and Link Room");

        Bounds refBounds = refCollider.bounds;
        Vector3 newMovePosition = roomToMove.transform.position;

        if (refDirection == "East" || refDirection == "West")
        {
            float offsetX = refBounds.extents.x + moveCollider.bounds.extents.x;
            newMovePosition.x = (refDirection == "East") ? refBounds.center.x + offsetX : refBounds.center.x - offsetX;
            newMovePosition.y = refBounds.center.y;
        }
        else
        {
            float offsetY = refBounds.extents.y + moveCollider.bounds.extents.y;
            newMovePosition.y = (refDirection == "North") ? refBounds.center.y + offsetY : refBounds.center.y - offsetY;
            newMovePosition.x = refBounds.center.x;
        }

        roomToMove.transform.position = newMovePosition;
        Debug.Log($"Sala '{roomToMove.name}' alinhada com '{referenceRoom.name}'.");

        RoomTransition transitionA = referenceRoom.transform.Find($"Transition_{refDirection}")?.GetComponent<RoomTransition>();
        RoomTransition transitionB = roomToMove.transform.Find($"Transition_{moveDirection}")?.GetComponent<RoomTransition>();

        if (transitionA != null && transitionB != null)
        {
            Undo.RecordObject(transitionA, "Auto-Link Transition A");
            Undo.RecordObject(transitionB, "Auto-Link Transition B");

            transitionA.LinkTo(roomToMove, transitionB.transform.Find("CameraTargetPoint"), transitionB.transform.Find("PlayerSpawnPoint"));
            transitionB.LinkTo(referenceRoom, transitionA.transform.Find("CameraTargetPoint"), transitionA.transform.Find("PlayerSpawnPoint"));

            EditorUtility.SetDirty(transitionA);
            EditorUtility.SetDirty(transitionB);

            Debug.Log($"<color=green>SUCESSO:</color> Transições '{transitionA.name}' e '{transitionB.name}' ligadas!");
        }
    }
}