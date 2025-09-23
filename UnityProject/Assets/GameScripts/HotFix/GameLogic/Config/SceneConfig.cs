using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameLogic
{
    
    public class SceneItem
    {
        public string ShowName;
        public string ScenePath;
        public string ImagePath;
    }

    public class SceneConfig 
    {
       public static List<SceneItem> SceneItems = new List<SceneItem>()
        {
            new SceneItem()
            {
                ShowName = "梦境森林",
                ScenePath = "Demo01",
                ImagePath = "Dream_Show",
            },
            new SceneItem()
            {
                ShowName = "默认",
                ScenePath = "MainCity",
                ImagePath = "MainCity_Show",
            },
            new SceneItem()
            {
                ShowName = "寂静森林-白天",
                ScenePath = "Demo Day",
                ImagePath = "Demo Day_Show",
            },
            new SceneItem()
            {
                ShowName = "寂静森林-晚上",
                ScenePath = "Demo Night",
                ImagePath = "Demo Night_Show",
            },
            new SceneItem()
            {
                ShowName = "寂静森林-混合",
                ScenePath = "Demo Blend",
                ImagePath = "Demo Blend_Show",
            },
        };

    }
}
