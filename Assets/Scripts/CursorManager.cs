using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

public class CursorManager : MonoBehaviour
{
    [SerializeField]
    RectTransform rectTransform;
    [SerializeField]
    VirtualMouseInput[] cursors;
    [SerializeField]
    string cursorActionMapName = "Move";

    readonly List<VirtualMouseInput> activeCursors = new List<VirtualMouseInput>();

    public void OnPlayerJoined(PlayerInput playerInput)
    {
        Debug.Log($"Player joined: {playerInput.playerIndex}");

        var index = playerInput.playerIndex;

        if(index < 0 || index >= cursors.Length)
        {
            Debug.LogWarning($"No cursor assigned for player index {index}");
            return;
        }

        var cursor = Instantiate(cursors[index], rectTransform);
        cursor.name = $"Cursor_{index}";
        activeCursors.Add( cursor );
        var actions = playerInput.actions;
        var cursorAction = actions.FindAction(cursorActionMapName);

        if (cursorAction != null)
        {
            cursor.stickAction = new InputActionProperty(cursorAction);
        }
    }

    public void OnPlayerLeft(PlayerInput playerInput)
    {
        Debug.Log($"Player left: {playerInput.playerIndex}");
        var index = playerInput.playerIndex;
        var cursor = activeCursors.Find(c => c.name == $"Cursor_{index}");
        if(cursor != null)
        {
            activeCursors.Remove(cursor);
            Destroy(cursor.gameObject);
        }
    }
}
