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
        };

    }
}
