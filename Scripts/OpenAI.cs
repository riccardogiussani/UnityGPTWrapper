using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace OpenAI4Unity
{
    public class OpenAI
    {
        public static string apiUrl = "https://api.openai.com/v1/chat/completions";
        public static string apiKey;

        public OpenAI()
        {
            LoadCredentials();
        }

        public static void LoadCredentials()
        {
            string homeDirectory = System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile);
            string credentialsPath = Path.Combine(homeDirectory, ".openai", "auth.json");

            if (File.Exists(credentialsPath))
            {
                string json = File.ReadAllText(credentialsPath);
                Credentials credentials = JsonConvert.DeserializeObject<Credentials>(json);
                apiKey = credentials.api_key;
            }
            else
            {
                Debug.LogError("Credentials file not found at: " + credentialsPath);
            }
        }

        public static Property CreateSimpleParameter(string name, string type, string description)
        {
            var parameter = new Dictionary<string, object>
                    {
                        { "type", type },
                        { "description", description }
                    };

            return new Property(name, parameter);
        }
        public static Property CreateEnumParameter(string name, string type, string[] enumValues)
        {
            var parameter = new Dictionary<string, object>
                    {
                        { "type", type },
                        { "enum", enumValues }
                    };

            return new Property(name, parameter);
        }

        public static Tool CreateTool(string name, string description, List<Property> properties)
        {
            var function = new Function(name, description, properties);
            var tool = new Tool("function", function);

            return tool;
        }
    }

    public class Agent
    {
        public Dictionary<string, Tool> Tools;
        public List<ChatMessage> Messages;
        private string prompt;
        public string Prompt
        {
            get { return prompt; }
            set
            {
                if (value != prompt)
                {
                    prompt = value;
                    Messages.Add(new ChatMessage("system", value));
                }
            }
        }

        public Agent()
        {
            Tools = new Dictionary<string, Tool>();
            Messages = new List<ChatMessage>();
            //Messages.Add(new ChatMessage("system", "Don't make assumptions about what values to plug into functions. Ask for clarification if a user request is ambiguous."));
        }

        public void Request(string query, Action<UnityWebRequest> handleReceived, MonoBehaviour caller)
        {
            caller.StartCoroutine(RequestCo(query, handleReceived));
        }

        public IEnumerator RequestCo(string query, Action<UnityWebRequest> handleReceived)
        {
            Messages.Add(new ChatMessage("user", query));

            // Create request object
            var requestObject = new ChatRequest("gpt-3.5-turbo", Messages, Tools);

            // Serialize request object to JSON
            string jsonData = JsonConvert.SerializeObject(requestObject, Formatting.Indented);

            Debug.Log(jsonData);

            // Create UnityWebRequest object
            UnityWebRequest request = new UnityWebRequest(OpenAI.apiUrl, "POST");

            // Set request headers
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + OpenAI.apiKey);

            // Convert json data to byte array
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);

            // Set request body
            request.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();

            // Send the request
            yield return request.SendWebRequest();

            // Check for errors
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error: " + request.error);
                Debug.LogError(request.downloadHandler.text);
            }
            else
            {
                handleReceived.Invoke(request);
                //Debug.Log("Response: " + request.downloadHandler.text);
            }

        }

        public void ClearChat()
        {
            Messages = new List<ChatMessage>();
            Messages.Add(new ChatMessage("system", "Don't make assumptions about what values to plug into functions. Ask for clarification if a user request is ambiguous."));
        }

        public void AddTool(string name, string description, List<Property> properties = null, List<string> _required = null)
        {
            if (Tools.Any(tool => tool.Value.Type == name))
            {
                Debug.LogError($"Error: Tool with name '{name}' already exists.");
                return; // Exit the method
            }

            if(properties == null)
            {
                var function = new Function(name, description);
                var tool = new Tool("function", function);

                Tools.Add(name, tool);
            }
            else
            {
                Function function;
                if (_required != null)
                    function = new Function(name, description, properties, _required);
                else
                    function = new Function(name, description, properties);

                var tool = new Tool("function", function);

                Tools.Add(name, tool);
            }
        }
        public void RemoveTool(string name)
        {
            if (Tools.ContainsKey(name))
            {
                // Remove the tool from the dictionary using its key
                Tools.Remove(name);
            }
            else
            {
                Debug.LogWarning($"Tool with name '{name}' not found.");
            }
        }
    }

    public class ChatRequest
    {
        [JsonProperty("model")]
        public string Model { get; set; }

        [JsonProperty("messages")]
        public List<ChatMessage> Messages { get; set; }

        [JsonProperty("tools")]
        public List<Tool> Tools { get; set; }

        [JsonProperty("tool_choice")]
        public string ToolChoice { get; set; }

        public ChatRequest(string model, List<ChatMessage> messages, Dictionary<string, Tool> tools, string toolChoice = "auto")
        {
            Model = model;
            Messages = messages;
            Tools = (tools.Count != 0) ? tools.Values.ToList() : null;
            ToolChoice = (tools.Count != 0) ? toolChoice : null;
        }
    }

    public class ChatMessage
    {
        [JsonProperty("role")]
        public string Role { get; set; }

        [JsonProperty("content")]
        public string Content { get; set; }

        public ChatMessage(string role, string content)
        {
            Role = role;
            Content = content;
        }
    }
    public class Tool
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("function")]
        public Function Function { get; set; }

        public Tool(string type, Function function)
        {
            Type = type;
            Function = function;
        }

        public void AddProperty(Property par, bool isRequired = false)
        {
            // Check if the key already exists in the Parameters dictionary
            if (Function.Parameters.Properties.ContainsKey(par.Key))
            {
                // Update the value of the existing key
                Function.Parameters.Properties[par.Key] = par.Value;
                if (isRequired)
                    Function.Parameters.Required.Add(par.Key);
            }
            else
            {
                // Add a new key-value pair to the Parameters dictionary
                Function.Parameters.Properties.Add(par.Key, par.Value);
                if (isRequired)
                    Function.Parameters.Required.Add(par.Key);
            }
        }

    }

    public class Function
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("parameters")]
        public Parameters Parameters { get; set; }

        Parameters CreateParameters(List<Property> properties, List<string> _required = null)
        {
            var functionParameters = new Dictionary<string, object>();
            foreach (var parameter in properties)
            {
                functionParameters.Add(parameter.Key, parameter.Value);
            }
            List<string> required = (_required == null) ? new List<string>() : _required;
            // Assuming Parameters constructor exists
            return new Parameters("object", functionParameters, required);
        }

        public Function(string name, string description, List<Property> properties)
        {
            Name = name;
            Description = description;
            Parameters = CreateParameters(properties);
        }

        public Function(string name, string description, List<Property> properties, List<string> required)
        {
            Name = name;
            Description = description;
            Parameters = CreateParameters(properties, required);
        }

        public Function(string name, string description)
        {
            Name = name;
            Description = description;
            Parameters = new Parameters("object", new Dictionary<string, object>());
        }

        
    }
    public class Parameters
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("properties")]
        public Dictionary<string, object> Properties { get; set; }

        [JsonProperty("required")]
        public List<string> Required { get; set; }

        public Parameters(string type, Dictionary<string, object> properties, List<string> required = null)
        {
            Type = type;
            Properties = properties;
            Required = required == null ? new List<string>() : required;
        }
    }
    public class Property
    {
        public string Key { get; }
        public object Value { get; }

        public Property(string key, object value)
        {
            Key = key;
            Value = value;
        }
    }


    [System.Serializable]
    public class Credentials
    {
        public string api_key;
        public string organization;
    }
}