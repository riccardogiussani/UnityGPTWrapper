using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace OpenAI4Unity
{
    public class ResponseParser
    {
        public static ResponseData ParseResponse(UnityWebRequest request)
        {
            return JsonUtility.FromJson<ResponseData>(request.downloadHandler.text);
        }

        public static ResponseData ParseResponse(string jsonData)
        {
            return JsonUtility.FromJson<ResponseData>(jsonData);
        }


        [System.Serializable]
        public class ToolCall
        {
            public string id;
            public string type;
            public ToolFunction function;
        }

        [System.Serializable]
        public class ToolFunction
        {
            public string name;
            public string arguments;
        }

        [System.Serializable]
        public class Choice
        {
            public int index;
            public Message message;
            public object logprobs; // Change the type if logprobs have specific structure
            public string finish_reason;
        }

        [System.Serializable]
        public class Message
        {
            public string role;
            public string content; // Change the type if content is expected to be something specific
            public ToolCall[] tool_calls;
        }

        [System.Serializable]
        public class Usage
        {
            public int prompt_tokens;
            public int completion_tokens;
            public int total_tokens;
        }

        [System.Serializable]
        public class ResponseData
        {
            public string id;
            public string response_object;
            public long created;
            public string model;
            public Choice[] choices;
            public Usage usage;
            public string system_fingerprint;
        }
    }

    public class Utils
    {
        public static GameObject GetObjectById(int id)
        {
            foreach (GameObject go in Resources.FindObjectsOfTypeAll(typeof(GameObject)))
            {
                if (go.GetInstanceID() == id)
                    return go;
            }
            return null;
        }
    }
}

