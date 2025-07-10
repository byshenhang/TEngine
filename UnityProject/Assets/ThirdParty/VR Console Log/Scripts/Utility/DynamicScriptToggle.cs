using UnityEngine;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace MikeNspired.VRConsoleLog
{
    public class DynamicComponentToggle : MonoBehaviour
    {
        [Tooltip("Drag any Component scripts from child objects here.")]
        public List<Component> scriptTypes;

        private Dictionary<Type, List<Component>> cachedComponentsByType = new Dictionary<Type, List<Component>>();

        void Start()
        {
            CacheComponents();
        }

        public void CacheComponents()
        {
            cachedComponentsByType.Clear();
            foreach (Component scriptType in scriptTypes)
            {
                if (scriptType != null)
                {
                    Type type = scriptType.GetType();
                    if (!cachedComponentsByType.ContainsKey(type))
                    {
                        cachedComponentsByType[type] = new List<Component>(GetComponentsInChildren(type, true));
                    }
                }
            }
        }

        public void ToggleComponents()
        {
            foreach (var pair in cachedComponentsByType)
            {
                foreach (Component component in pair.Value)
                {
                    ToggleComponent(component);
                }
            }
        }

        public void EnableComponents()
        {
            SetComponentsEnabled(true);
        }

        public void DisableComponents()
        {
            SetComponentsEnabled(false);
        }

        private void ToggleComponent(Component component)
        {
            PropertyInfo enabledProperty = component.GetType().GetProperty("enabled");
            if (enabledProperty != null && enabledProperty.PropertyType == typeof(bool))
            {
                bool currentValue = (bool)enabledProperty.GetValue(component);
                enabledProperty.SetValue(component, !currentValue);
            }
        }

        private void SetComponentsEnabled(bool enabled)
        {
            foreach (var pair in cachedComponentsByType)
            {
                foreach (Component component in pair.Value)
                {
                    PropertyInfo enabledProperty = component.GetType().GetProperty("enabled");
                    if (enabledProperty != null && enabledProperty.PropertyType == typeof(bool))
                    {
                        enabledProperty.SetValue(component, enabled);
                    }
                }
            }
        }

        // Update the cache to reflect any changes in the child components or the component types
        public void UpdateCache()
        {
            CacheComponents();
        }
    }
}