using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Kugon.BetterAnimationEvents
{
    [Serializable]
    public class KMethodHandler
    {
        [Serializable]
        public struct KMethodInfo
        {
            public string ID;
            public MethodInfo MethodInfo;
            public MonoBehaviour Component;

            public KMethodInfo(string id, MethodInfo methodInfo, MonoBehaviour component)
            {
                ID = id;
                MethodInfo = methodInfo;
                Component = component;
            }

            public string Name => MethodInfo.Name;
            public Type DeclaringType => MethodInfo.DeclaringType;
            public ParameterInfo[] GetParameters() => MethodInfo.GetParameters();
            
            public bool IsValid => MethodInfo != null;
        }
        
        public Dictionary<string, KMethodInfo> RuntimeFunctions { get; private set; }
        
        
        
        private static readonly HashSet<string> ExcludedMethodNames = new HashSet<string>
        {
            "Awake",
            "Start",
            "Update",
            "FixedUpdate",
            "LateUpdate",
            "CancelInvoke",
            "StopCoroutine",
            "StopAllCoroutines",
            "SendMessage",
            "SendMessageUpwards",
            "BroadcastMessage",
        };

        public KMethodHandler()
        {
            RuntimeFunctions = new Dictionary<string, KMethodInfo>();
        }
        
        public void GenerateFunctions(Transform animatorRoot)
        {
            Clear();
            if (animatorRoot == null)
            {
                Debug.LogWarning("Transform root was null. Could Not Generate Functions.");
                return;
            }
            
            List<MethodInfo> functions = new List<MethodInfo>();
            
            var components = animatorRoot.GetComponents<MonoBehaviour>();
            foreach (var comp in components)
            {
                functions.Clear();
                var methods = GetValidFunctions(comp);

                functions.AddRange(methods);

                foreach (var method in methods)
                {
                    string methodID = GenerateMethodID(method);
                    if (!RuntimeFunctions.ContainsKey(methodID))
                        RuntimeFunctions.Add(methodID, new KMethodInfo(methodID, method, comp));
                }
            }
        }

        public void Clear()
        {
            RuntimeFunctions.Clear();
        }

        public KMethodInfo[] GetFunctions => RuntimeFunctions.Values.ToArray();
        
        
        public string GetMethodName(string methodID)
        {
            if (RuntimeFunctions.ContainsKey(methodID))
                return RuntimeFunctions[methodID].Name;

            return "";
        }

        public string GetMethodIDByFunctionName(string functionName)
        {
            foreach (var pair in RuntimeFunctions)
                if (pair.Value.Name == functionName)
                    return pair.Key;

            return "";
        }

        public bool GetMethodInfo(string id, out KMethodInfo methodInfo)
        {
            methodInfo = new KMethodInfo("", null, null);  
            if (!RuntimeFunctions.ContainsKey(id))
                return false;
            
            methodInfo = RuntimeFunctions[id];
            return true;
        }
        
        public string GetClassName(string methodID)
        {
            if (GetMethodInfo(methodID, out KMethodInfo info))
            {
                return info.DeclaringType != null
                    ? info.DeclaringType.Name
                    : string.Empty;
            }
        
            return string.Empty; // not found
        }
        
        public bool HasFunctionID(string methodID)
        {
            return RuntimeFunctions.ContainsKey(methodID);
        }
        
        private IEnumerable<MethodInfo> GetValidFunctions(MonoBehaviour component)
        {
            return component.GetType()
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(IsValidMethod);
        }
        
        private bool IsValidMethod(MethodInfo method)
        {
            if (method.ReturnType != typeof(void))
                return false;

            if (method.IsSpecialName)
                return false;

            if (ExcludedMethodNames.Contains(method.Name))
                return false;

            var parameters = method.GetParameters();

            if (parameters.Length > 1)
                return false;

            if (parameters.Length == 1)
                return IsValidParamType(parameters[0].ParameterType);

            return true;
        }

        private bool IsValidParamType(Type t)
        {
            return t == typeof(string) || 
                   t == typeof(int) || 
                   t == typeof(float) || 
                   t == typeof(AnimationEvent) || 
                   t.IsEnum || 
                   typeof(Object).IsAssignableFrom(t);
        }
        
        public string GenerateMethodID(MethodInfo method)
        {
            // return $"{method.Name}(" + string.Join(",", method.GetParameters().Select(p => p.ParameterType.Name)) + ")";
            return $"{method.Name}";
        }
    }
}