using UnityEngine;
using UnityEditor;
using System.Linq;

public class RoomTool : EditorWindow
{
    private enum VerticalAlignment { Middle, Top, Bottom }
    private enum HorizontalAlignment { Center, Left, Right }

    private Room roomA;
    private Room roomB;
    private VerticalAlignment vAlign = VerticalAlignment.Middle;
    private HorizontalAlignment hAlign = HorizontalAlignment.Center;

    [MenuItem("Tools/Room Tool (Unified)")]
    public static void ShowWindow()
    {
        GetWindow<RoomTool>("Room Tool");
    }

    private void OnGUI()
    {
        GUILayout.Label("Ferramenta de Gestão de Salas", EditorStyles.boldLabel);

        EditorGUILayout.Space();

        // --- SEÇÃO DE SALA ÚNICA ---
        EditorGUILayout.LabelField("1. Ferramentas de Sala Única", EditorStyles.boldLabel);
        roomA = (Room)EditorGUILayout.ObjectField("Sala Selecionada (Referência)", roomA, typeof(Room), true);

        if (roomA == null)
        {
            GUI.enabled = false;
        }

        if (GUILayout.Button("Alinhar Transições Filhas"))
        {
            AlignChildTransitions(roomA);
        }

        GUI.enabled = true;
        EditorGUILayout.Space();

        // --- SEÇÃO DE DUAS SALAS ---
        EditorGUILayout.LabelField("2. Ferramentas de Alinhamento e Ligação", EditorStyles.boldLabel);
        roomB = (Room)EditorGUILayout.ObjectField("Sala B (Para Mover)", roomB, typeof(Room), true);

        if (roomA == null || roomB == null)
        {
            GUI.enabled = false;
            EditorGUILayout.HelpBox("Selecione duas salas para usar as ferramentas abaixo.", MessageType.Warning);
        }

        EditorGUILayout.LabelField("Opções de Alinhamento Secundário:", EditorStyles.miniBoldLabel);
        vAlign = (VerticalAlignment)EditorGUILayout.EnumPopup("Alinhamento Vertical:", vAlign);
        hAlign = (HorizontalAlignment)EditorGUILayout.EnumPopup("Alinhamento Horizontal:", hAlign);

        EditorGUILayout.Space();

        if (GUILayout.Button("Alinhar e Ligar (B à Direita de A)"))
        {
            AlignAndLinkRooms(roomA, roomB, "East", "West", vAlign);
        }
        if (GUILayout.Button("Alinhar e Ligar (B à Esquerda de A)"))
        {
            AlignAndLinkRooms(roomA, roomB, "West", "East", vAlign);
        }
        if (GUILayout.Button("Alinhar e Ligar (B em Cima de A)"))
        {
            AlignAndLinkRooms(roomA, roomB, "North", "South", hAlign);
        }
        if (GUILayout.Button("Alinhar e Ligar (B em Baixo de A)"))
        {
            AlignAndLinkRooms(roomA, roomB, "South", "North", hAlign);
        }

        GUI.enabled = true;
    }

    private void AlignChildTransitions(Room room)
    {
        // ... (código que já estava a funcionar)
    }

    private void AlignAndLinkRooms(Room referenceRoom, Room roomToMove, string refDirection, string moveDirection, System.Enum secondaryAlignment)
    {
        BoxCollider2D refCollider = referenceRoom.GetComponent<BoxCollider2D>();
        BoxCollider2D moveCollider = roomToMove.GetComponent<BoxCollider2D>();
        if (refCollider == null || moveCollider == null) { Debug.LogError("Ambas as salas precisam de um BoxCollider2D."); return; }

        Undo.RecordObject(roomToMove.transform, "Align and Link Room");

        Bounds refBounds = refCollider.bounds;
        Bounds moveBounds = moveCollider.bounds;
        Vector3 newMovePosition = roomToMove.transform.position;

        switch (refDirection)
        {
            case "East":
            case "West":
                float offsetX = refBounds.extents.x + moveBounds.extents.x;
                newMovePosition.x = (refDirection == "East") ? refBounds.center.x + offsetX : refBounds.center.x - offsetX;
                switch ((VerticalAlignment)secondaryAlignment)
                {
                    case VerticalAlignment.Top: newMovePosition.y = refBounds.max.y - moveBounds.extents.y; break;
                    case VerticalAlignment.Bottom: newMovePosition.y = refBounds.min.y + moveBounds.extents.y; break;
                    default: newMovePosition.y = refBounds.center.y; break;
                }
                break;
            case "North":
            case "South":
                float offsetY = refBounds.extents.y + moveBounds.extents.y;
                newMovePosition.y = (refDirection == "North") ? refBounds.center.y + offsetY : refBounds.center.y - offsetY;
                switch ((HorizontalAlignment)secondaryAlignment)
                {
                    case HorizontalAlignment.Left: newMovePosition.x = refBounds.min.x + moveBounds.extents.x; break;
                    case HorizontalAlignment.Right: newMovePosition.x = refBounds.max.x - moveBounds.extents.x; break;
                    default: newMovePosition.x = refBounds.center.x; break;
                }
                break;
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