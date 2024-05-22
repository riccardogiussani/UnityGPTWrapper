using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CanvasManager : MonoBehaviour
{
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private TextMeshProUGUI responseField;
    [SerializeField] private Button sendButton;

    [SerializeField] private ChatGPTManager manager;

    private void Start()
    {
        manager.OnResponseReceived.AddListener(HandleResponse);
        sendButton.onClick.AddListener(() =>
        {
            // Call the Request method from the chatbot instance in manager
            manager.chatbot.Prompt = $"The user has three lights: a green one, a yellow one and a red one. Currently the green button is {(manager.lights[0].State == true ? "on" : "off")}, the yellow light is {(manager.lights[1].State == true ? "on" : "off")} and the red light is {(manager.lights[2].State == true ? "on" : "off")}";
            manager.chatbot.Request(inputField.text, manager.HandleResponse, manager);
        });
    }

    private void HandleResponse(string response)
    {
        responseField.text = response;
    }
}
