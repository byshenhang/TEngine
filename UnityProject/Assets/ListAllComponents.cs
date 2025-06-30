using UnityEngine;

public class ListAllComponents : MonoBehaviour
{
    void Update()
    {
        // 获取当前GameObject上的所有组件
        Component[] components = GetComponents<Component>();

        // 输出GameObject名称
        Debug.Log("GameObject: " + gameObject.name + " has the following components:");

        // 遍历并输出所有组件
        foreach (Component component in components)
        {
            if (component != null)
            {
                Debug.Log("Debug Component - " + component.GetType().Name);
            }
            else
            {
                Debug.Log("Debug Component - Miss");
            }
        }

        // 输出组件总数
        Debug.Log("Total components: " + components.Length);
    }

    // 可选：在编辑器模式下也可以查看
#if UNITY_EDITOR
    [ContextMenu("List Components")]
    private void ListComponentsInEditor()
    {
        Component[] components = GetComponents<Component>();
        Debug.Log("Editor - GameObject: " + gameObject.name + " has the following components:");
        foreach (Component component in components)
        {
            Debug.Log("- " + component.GetType().Name);
        }
        Debug.Log("Total components: " + components.Length);
    }
#endif
}