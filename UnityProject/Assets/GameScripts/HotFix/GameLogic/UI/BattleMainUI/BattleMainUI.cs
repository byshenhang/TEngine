using UnityEngine;
using UnityEngine.UI;
using TEngine;

namespace GameLogic
{
    [Window(UILayer.UI)]
    class BattleMainUI : UI3DWindow
    {
        #region 脚本工具生成的代码
        private RectTransform _rectContainer;
        private GameObject _itemTouch;
        private GameObject _goTopInfo;
        private GameObject _itemRoleInfo;
        private GameObject _itemMonsterInfo;
        private Button _btn_debug;
        protected override void ScriptGenerator()
        {
            _rectContainer = FindChildComponent<RectTransform>("m_rectContainer");
            _itemTouch = FindChild("m_rectContainer/m_itemTouch").gameObject;
            _goTopInfo = FindChild("m_goTopInfo").gameObject;
            _itemRoleInfo = FindChild("m_goTopInfo/m_itemRoleInfo").gameObject;
            _itemMonsterInfo = FindChild("m_goTopInfo/m_itemMonsterInfo").gameObject;
            _btn_debug = FindChildComponent<Button>("m_btn_debug");
            _btn_debug.onClick.AddListener(OnClick_debugBtn);
        }
        #endregion

        #region 事件
        private void OnClick_debugBtn()
        {
            Debug.Log("---------------------------------- XR Event Action ----------------------------------");
        }
        #endregion

    }
}
