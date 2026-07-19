using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputManager : MonoBehaviour
{
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Transform[] spawnPoints;

    private bool wasdJoined = false;
    private bool arrowsJoined = false;
    private bool[] gamepadJoined = new bool[4]; // Assuming a maximum of 4 gamepads

    private Color GetRandomColor()
    {
        return new Color(Random.value, Random.value, Random.value);
    }

    void Update()
    {
        if (Keyboard.current == null) return;

        if (!wasdJoined && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            var player = PlayerInput.Instantiate(playerPrefab, controlScheme: "WASD", pairWithDevice: Keyboard.current);

            Color playerColor = GetRandomColor();
            PlayerController playerController = player.GetComponent<PlayerController>();

            playerController.SetAssignedColor(playerColor);
            playerController.SetLabel("WASD Keyboard");

            if (spawnPoints.Length > 0)
            {
                player.transform.position = spawnPoints[0].position;
            }

            wasdJoined = true;
        }

        if (!arrowsJoined && Keyboard.current.rightCtrlKey.wasPressedThisFrame)
        {
            var player = PlayerInput.Instantiate(playerPrefab, controlScheme: "Arrows", pairWithDevice: Keyboard.current);
            if (spawnPoints.Length > 1)
            {
                player.transform.position = spawnPoints[0].position;
            }

            arrowsJoined = true;
        }

        for (int i = 0; i < Gamepad.all.Count; i++)
        {
            var gamePad = Gamepad.all[i];
            if (i < gamepadJoined.Length && gamePad.buttonSouth.wasPressedThisFrame && !gamepadJoined[i])
            {
                PlayerInput.Instantiate(playerPrefab, controlScheme: "Gamepad", pairWithDevice: gamePad);
                gamepadJoined[i] = true;
            }
        }
    }
}