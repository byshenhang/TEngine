using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameLogic
{
    
    public class EffectItem
    {
        public string ShowName;
        public string EffectValue;
    }

    public class EffectConfig 
    {
       public static List<EffectItem> SceneItems = new List<EffectItem>()
        {
            new EffectItem()
            {
                ShowName = "默认效果",
                EffectValue = "default_fade",
            },
            new EffectItem()
            {
                ShowName = "文字晃动",
                EffectValue = "shake_character",
            },
            new EffectItem()
            {
                ShowName = "随机颜色",
                EffectValue = "random_color_fade",
            },
        };

    }
}
