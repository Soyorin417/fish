using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Serialization
{
    public static class JsonArrayHelper
    {
        [Serializable]
        private class Wrapper<T>
        {
            public List<T> dataList = new List<T>();
        }

        public static List<T> FromJsonArray<T>(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new List<T>();
            }

            string trimmed = json.Trim();

            // 如果本来就是对象格式 {"dataList":[...]}，直接按对象读
            if (trimmed.StartsWith("{"))
            {
                Wrapper<T> wrapped = JsonUtility.FromJson<Wrapper<T>>(trimmed);
                return wrapped?.dataList ?? new List<T>();
            }

            // 如果是数组格式 [...]，手动包一层
            if (trimmed.StartsWith("["))
            {
                string wrappedJson = "{ \"dataList\": " + trimmed + " }";
                Wrapper<T> wrapped = JsonUtility.FromJson<Wrapper<T>>(wrappedJson);
                return wrapped?.dataList ?? new List<T>();
            }

            Debug.LogError("JsonArrayHelper: 无法识别的 JSON 格式。\n" + json);
            return new List<T>();
        }

        public static string ToJsonArray<T>(List<T> list, bool prettyPrint = false)
        {
            Wrapper<T> wrapper = new Wrapper<T>
            {
                dataList = list ?? new List<T>()
            };
            return JsonUtility.ToJson(wrapper, prettyPrint);
        }
    }
}