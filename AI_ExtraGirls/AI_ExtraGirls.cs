﻿using HarmonyLib;

using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;

using System;
using System.Linq;
using System.Collections.Generic;

using Manager;
using AIProject;
using AIProject.UI;
using AIProject.SaveData;

using UnityEngine;
using UnityEngine.UI;

using ConfigScene;
using Illusion.Extensions;

namespace AI_ExtraGirls {
    [BepInPlugin(nameof(AI_ExtraGirls), nameof(AI_ExtraGirls), VERSION)][BepInProcess("AI-Syoujyo")]
    public class AI_ExtraGirls : BaseUnityPlugin
    {
        public const string VERSION = "1.0.4";
        public new static ManualLogSource Logger;
        
        /*
         * in-case the game gets an update:
         * Will need manual change: ChangeCharaCount()
         * May need manual change: AddIcons(), StatusUI_OnBeforeStart_AddElementsAndBackgrounds()
        */
        public static int defaultGirlCount;
        private static readonly string[] toAddList =
        {
            "CharaChangeUI(Clone)",
            "CharaLookEditUI(Clone)",
            "CharaMigrateUI(Clone)"
        };
        
        public static int girlCount;
        private static ConfigEntry<int> GirlCount { get; set; }
        
        private void Awake()
        {
            Logger = base.Logger;

            defaultGirlCount = GraphicSystem.MAX_CHARA_NUM;
            
            GirlCount = Config.Bind("Requires restart! Modifies save!", "Free Roam Girl Count", defaultGirlCount, new ConfigDescription("Requires a restart to apply.", new AcceptableValueRange<int>(defaultGirlCount, 99)));
            girlCount = GirlCount.Value;
            
            var harmony = new Harmony(nameof(AI_ExtraGirls));
            harmony.PatchAll(typeof(Transpilers));
            harmony.PatchAll(typeof(AI_ExtraGirls));
        }

        private static void CharaUI_AddScroll()
        { 
            foreach (string uiName in toAddList)
            {
                GameObject Information = GameObject.Find("MapScene/MapUI(Clone)/CommandCanvas/" + uiName + "/Panel/SelectPanel/Infomation");
                if (Information == null)
                    continue;

                Transform ElementLayout = Information.transform.Find("ElementLayout");
                if (ElementLayout == null)
                    continue;
                
                GameObject ScrollView = new GameObject("ScrollView", typeof(RectTransform));
                ScrollView.transform.SetParent(Information.transform, false);
                var svScrollRect = ScrollView.AddComponent<ScrollRect>();

                GameObject ViewPort = new GameObject("ViewPort", typeof(RectTransform));
                ViewPort.transform.SetParent(ScrollView.transform, false);
                ViewPort.AddComponent<RectMask2D>();
                ViewPort.AddComponent<Image>().color = new Color(0, 0, 0, 0);
                var vpRectTransform = ViewPort.GetComponent<RectTransform>();
                
                ElementLayout.SetParent(ViewPort.transform, false);

                svScrollRect.content = ElementLayout.gameObject.GetComponent<RectTransform>();
                svScrollRect.viewport = vpRectTransform;
                svScrollRect.horizontal = false;
                svScrollRect.scrollSensitivity = 40;

                vpRectTransform.offsetMin = new Vector2(-50, -255);
                vpRectTransform.offsetMax = new Vector2(400, 35);
                vpRectTransform.sizeDelta = new Vector2(450, 290);
                
                ScrollView.transform.localPosition = new Vector3(uiName == "CharaMigrateUI(Clone)" ? -175 : -400, -130, 0);
                
                Information.GetComponent<VerticalLayoutGroup>().enabled = false;

                Transform IconText = Information.transform.Find("IconText");
                IconText.localPosition = new Vector3(uiName == "CharaChangeUI(Clone)" ? -140 : -195, -45, 0);
                
                Transform separate = Information.transform.Find("separate");
                separate.GetComponent<RectTransform>().sizeDelta = new Vector2(uiName == "CharaMigrateUI(Clone)" ? -90 : 380, 10);
                separate.localPosition = new Vector3(-225, -80, 0);

                if (uiName != "CharaLookEditUI(Clone)") 
                    continue;

                Transform button = ElementLayout.Find("btnCharaCreation");
                if (button == null) 
                    continue;

                button.SetParent(Information.transform, false);
                button.localPosition = new Vector3(-30, -390, 0);
            }
        }
        
        private static void StatusUI_AddScroll()
        { 
            GameObject Tab = GameObject.Find("MapScene/MapUI(Clone)/CommandCanvas/MenuUI(Clone)/CellularUI/Interface Panel/StatusUI(Clone)/Tab");
            if (Tab == null)
                return;
            
            Transform Content = Tab.transform.Find("Content");
            if (Content == null)
                return;

            GameObject ScrollView = new GameObject("ScrollView", typeof(RectTransform));
            ScrollView.transform.SetParent(Tab.transform, false);
            var svScrollRect = ScrollView.AddComponent<ScrollRect>();

            GameObject ViewPort = new GameObject("ViewPort", typeof(RectTransform));
            ViewPort.transform.SetParent(ScrollView.transform, false);
            ViewPort.AddComponent<RectMask2D>();
            ViewPort.AddComponent<Image>().color = new Color(0, 0, 0, 0);
            var vpRectTransform = ViewPort.GetComponent<RectTransform>();

            Content.SetParent(ViewPort.transform, false);
            Content.gameObject.AddComponent<ContentSizeFitter>();

            var cContentSizeFitter = Content.gameObject.GetComponent<ContentSizeFitter>();
            cContentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            cContentSizeFitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

            var cHorizontalLayoutGroup = Content.gameObject.GetComponent<HorizontalLayoutGroup>();
            cHorizontalLayoutGroup.childForceExpandWidth = true;

            svScrollRect.content = Content.gameObject.GetComponent<RectTransform>();
            svScrollRect.viewport = vpRectTransform;
            svScrollRect.horizontal = true;
            svScrollRect.vertical = false;
            svScrollRect.scrollSensitivity = 40;

            vpRectTransform.offsetMin = new Vector2(-270, -50);
            vpRectTransform.offsetMax = new Vector2(270, 50);
            vpRectTransform.sizeDelta = new Vector2(540, 100);

            Transform Selection = Tab.transform.Find("Selection");
            Transform Focus = Tab.transform.Find("Focus");

            Selection.SetParent(Content);
            Focus.SetParent(Content);

            Selection.localScale = new Vector3(0.95f, 0.55f, 1);
        }
        
        private static void GraphicSystem_SetMaxCharas(GraphicSystem __instance)
        {
            bool[] charasEntry = new bool[girlCount];
            for (int i = 0; i < girlCount; i++)
                charasEntry[i] = true;

            __instance.MaxCharaNum = girlCount;
            __instance.CharasEntry = charasEntry;
        }

        private static void AddIcons(bool useIconCategory, Manager.Resources.ItemIconTables.IconCategory iconCategory)
        {
            if (useIconCategory && iconCategory != Manager.Resources.ItemIconTables.IconCategory.Item)
                return;

            Dictionary<int, Sprite> ActorIconTable = Singleton<Manager.Resources>.Instance.itemIconTables.ActorIconTable;

            if (ActorIconTable.Count >= girlCount + 2 || !ActorIconTable.ContainsKey(defaultGirlCount - 1)) 
                return;

            for (int i = 0; i < girlCount + 2; i++)
            {
                if (i < defaultGirlCount || ActorIconTable.Count >= girlCount + 2)
                    continue;

                ActorIconTable.Add(ActorIconTable.Count - 2, Instantiate(ActorIconTable[defaultGirlCount - 1]));
            }
        }
        
        private static void AddElements(CharaChangeUI change, CharaLookEditUI lookedit, CharaMigrateUI migrate)
        {
            Traverse trav;
            
            if(change != null)
                trav = Traverse.Create(change);
            else if(lookedit != null)
                trav = Traverse.Create(lookedit);
            else if(migrate != null)
                trav = Traverse.Create(migrate);
            else
                return;

            trav.Field("_infos").SetValue(new GameLoadCharaFileSystem.GameCharaFileInfo[girlCount]);
            
            var oldelements = trav.Field("_elements").GetValue<RectTransform[]>();
            List<RectTransform> newelements = new List<RectTransform>();
            newelements.AddRange(oldelements);

            var oldcharaButtons = trav.Field("_charaButtons").GetValue<Button[]>();
            List<Button> newCharaButtons = new List<Button>();
            newCharaButtons.AddRange(oldcharaButtons);
            
            var oldcharaTexts = trav.Field("_charaTexts").GetValue<Text[]>();
            List<Text> newCharaTexts = new List<Text>();
            newCharaTexts.AddRange(oldcharaTexts);

            List<Button> newCharaArrowButtons = new List<Button>();
            if (migrate != null)
            {
                var oldcharaArrowButtons = trav.Field("_charaArrowButtons").GetValue<Button[]>();
                newCharaArrowButtons.AddRange(oldcharaArrowButtons);
            }
            
            int oldElementsLength = oldelements.Length;
            for (int i = 0; i < girlCount - oldElementsLength; i++)
            {
                var copy = Instantiate(oldelements[oldElementsLength - 1], oldelements[oldElementsLength - 1].transform.parent);
                copy.name = $"Element_{i+oldElementsLength:00}";

                newelements.Add(copy);
                newCharaButtons.Add(copy.transform.Find("Button").GetComponent<Button>());
                newCharaTexts.Add(copy.transform.Find("Button/Text").GetComponent<Text>());
                
                if(migrate != null)
                    newCharaArrowButtons.Add(copy.transform.Find("arrow").GetComponent<Button>());
            }
            
            trav.Field("_elements").SetValue(newelements.ToArray());
            trav.Field("_charaButtons").SetValue(newCharaButtons.ToArray());
            trav.Field("_charaTexts").SetValue(newCharaTexts.ToArray());
            
            if(migrate != null)
                trav.Field("_charaArrowButtons").SetValue(newCharaArrowButtons.ToArray());
        }
        
        [HarmonyPostfix, HarmonyPatch(typeof(WorldData), "Copy")]
        private static void WorldData_Copy_AddRemoveAgents(WorldData __instance)
        {
            Dictionary<int, AgentData> agents = __instance.AgentTable;

            if (agents.Count < girlCount)
            {
                for (int i = 0; i < girlCount; i++)
                {
                    if (i < defaultGirlCount || agents.Count >= girlCount)
                        continue;

                    AgentData agentData = new AgentData();
                    agents.Add(agents.Count, agentData);
                }
            }

            var keys = new List<int>(agents.Keys);
            foreach (var key in keys.Where(key => key >= defaultGirlCount))
            {
                if (key > girlCount - 1){
                    agents.Remove(key);
                    
                    continue;
                }

                agents[key].OpenState = true;
                agents[key].PlayEnterScene = true;
            }

            if (Manager.Config.GraphicData != null)
                GraphicSystem_SetMaxCharas(Manager.Config.GraphicData);
        }

        [HarmonyPrefix, HarmonyPatch(typeof(Map), "LoadAgents")]
        public static void Map_LoadAgents_LoadAgents(Map __instance, ref WorldData profile)
        {
            if (profile?.AgentTable == null)
                return;
            
            WorldData_Copy_AddRemoveAgents(profile);

            if (__instance.PointAgent.DevicePointDic.Count >= girlCount) 
                return;
            
            var keys = new List<int>(profile.AgentTable.Keys);
            foreach (var key in keys.Where(key => !__instance.PointAgent.DevicePointDic.ContainsKey(key)))
                __instance.PointAgent.DevicePointDic.Add(key, __instance.PointAgent.DevicePointDic[0]);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(DevicePoint), "Start")]
        public static void DevicePoint_Start_AddRemoveAgents(DevicePoint __instance)
        {
            if (__instance.RecoverPoints.Count >= girlCount) 
                return;
            
            for (int i = 3; i < girlCount; i++)
                __instance.RecoverPoints.Add(__instance.RecoverPoints[0]);
        }
        
        
        [HarmonyPrefix, HarmonyPatch(typeof(StatusUI), "OnBeforeStart")]
        public static void StatusUI_OnBeforeStart_AddElementsAndBackgrounds(StatusUI __instance)
        {
            var trav = Traverse.Create(__instance);
            var oldcharaButtons = trav.Field("_charaButtons").GetValue<Button[]>();
            
            List<Button> newCharaButtons = new List<Button>();
            newCharaButtons.AddRange(oldcharaButtons);

            int oldCharaButtonsLength = oldcharaButtons.Length;
            for (int i = 0; i < 1 + girlCount - oldCharaButtonsLength; i++)
            {
                var copy = Instantiate(oldcharaButtons[oldCharaButtonsLength - 1], oldcharaButtons[oldCharaButtonsLength - 1].transform.parent);
                copy.name = $"Chara ({oldCharaButtonsLength + i - 1:0})";
                
                newCharaButtons.Add(copy.GetComponent<Button>());
            }

            List<Dictionary<int, CanvasGroup>> backgrounds = new List<Dictionary<int, CanvasGroup>>
            {
                trav.Field("_equipmentBackgrounds").GetValue<Dictionary<int, CanvasGroup>>(),
                trav.Field("_equipmentFlavorBackgrounds").GetValue<Dictionary<int, CanvasGroup>>(),
                trav.Field("_skillBackgrounds").GetValue<Dictionary<int, CanvasGroup>>(),
                trav.Field("_skillFlavorBackgrounds").GetValue<Dictionary<int, CanvasGroup>>()
            };
            
            foreach (var category in backgrounds.Where(category => category.Count < girlCount + 1))
            {
                for (int i = 0; i < 1 + girlCount; i++)
                {
                    if (i < defaultGirlCount + 1 || category.Count >= girlCount + 1)
                        continue;
                    
                    CanvasGroup bgData = Instantiate(category[category.Count - 1], category[category.Count - 1].transform.parent);
                    bgData.name = $"Chara ({i:0})";    
                    
                    category.Add(category.Count, bgData);
                }
            }

            trav.Field("_equipmentBackgrounds").SetValue(backgrounds[0]);
            trav.Field("_equipmentFlavorBackgrounds").SetValue(backgrounds[1]);  
            trav.Field("_skillBackgrounds").SetValue(backgrounds[2]);  
            trav.Field("_skillFlavorBackgrounds").SetValue(backgrounds[3]);
            trav.Field("_charaButtons").SetValue(newCharaButtons.ToArray());
            
            trav = Traverse.Create(__instance.Observer);
            Dictionary<int, CanvasGroup> mainBackgrounds = trav.Field("_backgrounds").GetValue<Dictionary<int, CanvasGroup>>();

            for (int i = 0; i < 2 + girlCount; i++)
            {
                if (i < defaultGirlCount + 2 || mainBackgrounds.Count >= girlCount + 2)
                    continue;

                CanvasGroup bgData = Instantiate(
                    mainBackgrounds[defaultGirlCount], 
                    mainBackgrounds[defaultGirlCount].transform.parent
                );
                bgData.name = $"Chara ({i - 2:0})";    
                
                mainBackgrounds.Add(mainBackgrounds.Count - 1, bgData);
            }
            
            trav.Field("_backgrounds").SetValue(mainBackgrounds);
            
            StatusUI_AddScroll();
        }
        
        [HarmonyPostfix, HarmonyPatch(typeof(StatusUI), "OpenCoroutine")]
        public static void StatusUI_OpenCoroutine_SetCharaButtonsVisibility(StatusUI __instance)
        {
            var trav = Traverse.Create(__instance);
            var charaButtons = trav.Field("_charaButtons").GetValue<Button[]>();
            
            var actorTable = Singleton<Map>.Instance.ActorTable;
            for(int i = 1; i < charaButtons.Length; i++)
                charaButtons[i].gameObject.SetActiveIfDifferent(actorTable.ContainsKey(i - 1));
        }
        
        
        [HarmonyPostfix, HarmonyPatch(typeof(MapUIContainer), "Start")]
        public static void MapUIContainer_Start_AddScroll() => CharaUI_AddScroll();
        

        [HarmonyPostfix, HarmonyPatch(typeof(Manager.Resources), "Awake")]
        public static void Resources_Awake_SetMaxAgents(Manager.Resources __instance) => Traverse.Create(__instance.DefinePack.MapDefines).Field("_agentMax").SetValue(girlCount);

        [HarmonyPostfix, HarmonyPatch(typeof(Manager.Resources.ItemIconTables), "LoadIcon", new Type[] {typeof(Manager.Resources.ItemIconTables.IconCategory)})]
        public static void ItemIconTables_LoadIcon_AddIcons(Manager.Resources.ItemIconTables.IconCategory iconCategory) => AddIcons(true, iconCategory);
        
        [HarmonyPostfix, HarmonyPatch(typeof(Manager.Resources.ItemIconTables), "LoadIcon", new Type[] {typeof(List<string>), typeof(Dictionary<int, Sprite>)})]
        public static void ItemIconTables_LoadIcon_AddIcons() => AddIcons(false, 0);

        
        [HarmonyPostfix, HarmonyPatch(typeof(GraphicSystem), "Init")]
        public static void GraphicSystem_Init_SetMaxCharas(GraphicSystem __instance) => GraphicSystem_SetMaxCharas(__instance);

        [HarmonyPrefix, HarmonyPatch(typeof(GraphicSystem), "Write")]
        public static void GraphicSystem_Write_SetMaxCharas_Pre(GraphicSystem __instance) => __instance.MaxCharaNum = defaultGirlCount;

        [HarmonyPostfix, HarmonyPatch(typeof(GraphicSystem), "Write")]
        public static void GraphicSystem_Write_SetMaxCharas_Post(GraphicSystem __instance) => __instance.MaxCharaNum = girlCount;

        [HarmonyPostfix, HarmonyPatch(typeof(GraphicSystem), "Read")]
        public static void GraphicSystem_Read_SetMaxCharas(GraphicSystem __instance) => GraphicSystem_SetMaxCharas(__instance);
        
        
        [HarmonyPrefix, HarmonyPatch(typeof(CharaChangeUI), "OnBeforeStart")]
        public static void CharaChangeUI_OnBeforeStart_AddElements(CharaChangeUI __instance) => AddElements(__instance, null, null);

        [HarmonyPrefix, HarmonyPatch(typeof(CharaLookEditUI), "OnBeforeStart")]
        public static void CharaLookEditUI_OnBeforeStart_AddElements(CharaLookEditUI __instance) => AddElements(null, __instance, null);
        
        [HarmonyPrefix, HarmonyPatch(typeof(CharaMigrateUI), "OnBeforeStart")]
        public static void CharaMigrateUI_OnBeforeStart_AddElements(CharaMigrateUI __instance) => AddElements(null, null, __instance);
        
    }
}