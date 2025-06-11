//using UnityEngine;
//using UnityEditor;
//using UnityEngine.XR.Interaction.Toolkit;
//using System.Collections.Generic;
//using System.Linq;

//namespace GameLogic.Editor
//{
//    /// <summary>
//    /// XRISceneManager 的自定义编辑器工具 - 提供可视化编辑功能
//    /// </summary>
//    [CustomEditor(typeof(XRISceneManager))]
//    public class XRISceneManagerEditor : UnityEditor.Editor
//    {
//        // 分组折叠状态
//        private bool _showInteractables = true;
//        private bool _showGroups = true;
//        private bool _showPresets = true;
        
//        // 预设应用设置
//        private string _selectedPresetName;
//        private GameObject _selectedInteractable;
        
//        // 组管理设置
//        private string _newGroupName = "NewGroup";
//        private GameObject _interactableToAddToGroup;
//        private string _selectedGroupForAdd;
        
//        // 场景描画结果缓存
//        private List<XRBaseInteractable> _sceneInteractables = new List<XRBaseInteractable>();
        
//        public override void OnInspectorGUI()
//        {
//            var sceneManager = (XRISceneManager)target;
            
//            // 绘制默认属性
//            DrawDefaultInspector();
            
//            EditorGUILayout.Space(10);
//            EditorGUILayout.LabelField("XRI 场景管理工具", EditorStyles.boldLabel);
            
//            if (GUILayout.Button("扫描场景交互物体"))
//            {
//                sceneManager.ScanSceneInteractables();
//                RefreshSceneInteractables();
//            }
            
//            EditorGUILayout.Space(5);
            
//            // 场景交互物体列表
//            _showInteractables = EditorGUILayout.Foldout(_showInteractables, "场景交互物体列表", true);
//            if (_showInteractables)
//            {
//                EditorGUI.indentLevel++;
//                DrawInteractablesList(sceneManager);
//                EditorGUI.indentLevel--;
//            }
            
//            EditorGUILayout.Space(5);
            
//            // 交互组管理
//            _showGroups = EditorGUILayout.Foldout(_showGroups, "交互组管理", true);
//            if (_showGroups)
//            {
//                EditorGUI.indentLevel++;
//                DrawGroupsManagement(sceneManager);
//                EditorGUI.indentLevel--;
//            }
            
//            EditorGUILayout.Space(5);
            
//            // 预设应用
//            _showPresets = EditorGUILayout.Foldout(_showPresets, "预设应用", true);
//            if (_showPresets)
//            {
//                EditorGUI.indentLevel++;
//                DrawPresetsApplication(sceneManager);
//                EditorGUI.indentLevel--;
//            }
            
//            if (GUI.changed)
//            {
//                EditorUtility.SetDirty(target);
//            }
//        }
        
//        private void RefreshSceneInteractables()
//        {
//            _sceneInteractables = FindObjectsOfType<XRBaseInteractable>().ToList();
//        }
        
//        private void DrawInteractablesList(XRISceneManager sceneManager)
//        {
//            if (_sceneInteractables == null || _sceneInteractables.Count == 0)
//                RefreshSceneInteractables();
            
//            EditorGUILayout.LabelField($"共找到 {_sceneInteractables.Count} 个交互物体");
//            EditorGUILayout.Space(2);
            
//            foreach (var interactable in _sceneInteractables)
//            {
//                if (interactable == null) continue;
//                EditorGUILayout.BeginHorizontal();
//                EditorGUILayout.ObjectField(interactable, typeof(XRBaseInteractable), true);
//                EditorGUILayout.LabelField(GetInteractableTypeLabel(interactable), GUILayout.Width(100));
//                EditorGUILayout.EndHorizontal();
//            }
//        }
        
//        private string GetInteractableTypeLabel(XRBaseInteractable interactable)
//        {
//            if (interactable is XRGrabInteractable)
//                return "可抓取";
//            else if (interactable is XRSimpleInteractable)
//                return "简单交互";
//            else
//                return "其他";
//        }
        
//        private void DrawGroupsManagement(XRISceneManager sceneManager)
//        {
//            EditorGUILayout.LabelField("创建新交互组", EditorStyles.boldLabel);
//            _newGroupName = EditorGUILayout.TextField("组名称", _newGroupName);
//            if (GUILayout.Button("创建交互组"))
//            {
//                if (!string.IsNullOrEmpty(_newGroupName))
//                {
//                    sceneManager.CreateInteractionGroup(_newGroupName);
//                    EditorUtility.SetDirty(target);
//                    _newGroupName = "NewGroup";
//                }
//                else
//                {
//                    EditorUtility.DisplayDialog("错误", "组名称不能为空", "确定");
//                }
//            }
            
//            EditorGUILayout.Space(5);
//            EditorGUILayout.LabelField("添加物体到交互组", EditorStyles.boldLabel);
//            _interactableToAddToGroup = EditorGUILayout.ObjectField("选择交互物体", _interactableToAddToGroup, typeof(GameObject), true) as GameObject;
//            var groups = GetGroups(sceneManager);
//            if (groups.Count > 0)
//            {
//                int idx = Mathf.Max(0, groups.IndexOf(_selectedGroupForAdd));
//                idx = EditorGUILayout.Popup("选择组", idx, groups.ToArray());
//                _selectedGroupForAdd = groups[idx];
//                GUI.enabled = _interactableToAddToGroup != null;
//                if (GUILayout.Button("添加到组"))
//                {
//                    var inter = _interactableToAddToGroup.GetComponent<XRBaseInteractable>();
//                    if (inter != null)
//                        sceneManager.AddToGroup(_selectedGroupForAdd, inter);
//                    else
//                        EditorUtility.DisplayDialog("错误", "所选物体缺少 XRBaseInteractable 组件", "确定");
//                    EditorUtility.SetDirty(target);
//                }
//                GUI.enabled = true;
//            }
//            else
//            {
//                EditorGUILayout.HelpBox("没有可用的交互组，请先创建。", MessageType.Info);
//            }
//        }
        
//        private List<string> GetGroups(XRISceneManager sceneManager)
//        {
//            // TODO: 从 sceneManager 获取实际组列表
//            return new List<string> { "Group1", "Group2", "DangerousItems" };
//        }
        
//        private void DrawPresetsApplication(XRISceneManager sceneManager)
//        {
//            _selectedInteractable = EditorGUILayout.ObjectField("选择交互物体", _selectedInteractable, typeof(GameObject), true) as GameObject;
//            var presets = GetAvailablePresets();
//            if (presets.Length > 0)
//            {
//                int idx = Mathf.Max(0, System.Array.IndexOf(presets, _selectedPresetName));
//                idx = EditorGUILayout.Popup("选择预设", idx, presets);
//                _selectedPresetName = presets[idx];
//                EditorGUILayout.HelpBox(GetPresetDescription(_selectedPresetName), MessageType.Info);
//                GUI.enabled = _selectedInteractable != null;
//                if (GUILayout.Button("应用预设"))
//                {
//                    ApplyPresetToInteractable(_selectedInteractable, _selectedPresetName, sceneManager);
//                }
//                GUI.enabled = true;
//            }
//            else
//            {
//                EditorGUILayout.HelpBox("没有可用的交互预设。", MessageType.Info);
//            }
//        }
        
//        private string[] GetAvailablePresets()
//        {
//            return new string[] { "标准按钮", "标准拉杆", "旋转按钮" };
//        }
        
//        private string GetPresetDescription(string presetName)
//        {
//            switch (presetName)
//            {
//                case "标准按钮":
//                    return "提供标准按钮交互，包括按下、释放事件和可选的视觉/音频反馈";
//                case "标准拉杆":
//                    return "提供标准拉杆交互，包括拉动、释放事件和位置检测";
//                case "旋转按钮":
//                    return "提供标准旋钮交互，包括旋转和数值检测";
//                default:
//                    return "无描述";
//            }
//        }
        
//        private void ApplyPresetToInteractable(GameObject go, string presetName, XRISceneManager sceneManager)
//        {
//            if (go == null) return;
//            var inter = go.GetComponent<XRBaseInteractable>();
//            if (inter == null)
//            {
//                EditorUtility.DisplayDialog("错误", "所选物体缺少 XRBaseInteractable 组件", "确定");
//                return;
//            }
//            EditorUtility.DisplayDialog("预设应用", $"已将预设 '{presetName}' 应用到 '{go.name}'", "确定");
//            EditorUtility.SetDirty(go);
//            EditorUtility.SetDirty(target);
//        }
//    }
//}