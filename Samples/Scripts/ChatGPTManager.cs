using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Networking;
using OpenAI4Unity;
using TMPro;
using Newtonsoft.Json;
using UnityEngine.Events;

public class ChatGPTManager : MonoBehaviour
{
    [HideInInspector] public UnityEvent<string> OnResponseReceived;

    private void Awake()
    {
        OpenAI openAI = new OpenAI();
    }

    public ButtonManager[] lights;

    public Agent chatbot;
    void Start()
    {
        chatbot = new Agent();

        string prompt = "The user has three lights: a green one, a yellow one and a red one.";
        chatbot.Prompt = prompt;
        /*
        string tool1 = "evaluate_relevance";
        chatbot.AddTool(tool1, "Evaluates the relevance of the question between 0 and 1.");
        Property property1 = OpenAI.CreateSimpleParameter("relevance_score", "string", "The relevance of the question evaluated between 0 and 1.");
        chatbot.Tools[tool1].AddProperty(property1, true);
        */
        string tool2 = "set_light";
        chatbot.AddTool(tool2, "Only if the user asks to swicth on or off a light, set the value of that light to the appropriate value.");
        Property property2 = OpenAI.CreateEnumParameter("light", "string", new[] { "green", "red", "yellow" });
        chatbot.Tools[tool2].AddProperty(property2, true);
        Property property3 = OpenAI.CreateEnumParameter("value", "string", new[] { "on", "off" });
        chatbot.Tools[tool2].AddProperty(property3, true);
    }

    public void HandleResponse(UnityWebRequest request)
    {
        var response = ResponseParser.ParseResponse(request);
        if(response.choices[0].finish_reason == "tool_calls")
        {
            for (int i = 0; i < response.choices[0].message.tool_calls.Length; i++)
            {
                Debug.Log(response.choices[0].message.tool_calls[i].function.name +
                " " +
                response.choices[0].message.tool_calls[i].function.arguments);
            }
            SetLight(response);
        }
        OnResponseReceived.Invoke(response.choices[0].message.content);
        Debug.Log(response.choices[0].message.content);
    }

    private void WriteResponse(ResponseParser.ResponseData data)
    {
        string response = data.choices[0].message.content;
        //responseField.text = response;
    }

    private void TriggerFunction(ResponseParser.ResponseData data)
    {
        for (int i = 0; i < data.choices[0].message.tool_calls.Length; i++)
        {
            string function = data.choices[0].message.tool_calls[i].function.name;
            Dictionary<string, object> args = JsonConvert.DeserializeObject<Dictionary<string, object>>(data.choices[0].message.tool_calls[i].function.arguments);

            switch (function)
            {
                case "fire_cannon":
                    Debug.Log($"Parameter {args["cannon"]}");
                    break;
                default:
                    break;
            }
        }
    }
    /*
    private void EvaluateRelevance(ResponseParser.ResponseData data, float threshold)
    {
        Dictionary<string, object> args = JsonConvert.DeserializeObject<Dictionary<string, object>>(data.choices[0].message.tool_calls[0].function.arguments);
        if (data.choices[0].message.tool_calls[0].function.name == "check_validity")
            if (float.Parse((string)args["relevance_score"]) > threshold)
                relevantChatbot.Request(inputField.text, HandleResponse, this);
            else
                nonRelevantChatbot.Request(inputField.text, HandleResponse, this);
    }
    */
    private void SetLight(ResponseParser.ResponseData data)
    {
        for(int i = 0; i<data.choices[0].message.tool_calls.Length; i++)
        {
            Dictionary<string, object> args = JsonConvert.DeserializeObject<Dictionary<string, object>>(data.choices[0].message.tool_calls[i].function.arguments);
            if (data.choices[0].message.tool_calls[i].function.name == "set_light")
            {
                switch (args["light"])
                {
                    case "green":
                        lights[0].SetState((string)args["value"] == "on" ? true : false);
                        break;
                    case "yellow":
                        lights[1].SetState((string)args["value"] == "on" ? true : false);
                        break;
                    case "red":
                        lights[2].SetState((string)args["value"] == "on" ? true : false);
                        break;
                    default:
                        break;
                }
            }
        }
    }
}
