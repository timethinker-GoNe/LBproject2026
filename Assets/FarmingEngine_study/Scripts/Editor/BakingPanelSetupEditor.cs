using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.IO;

namespace FarmingEngine
{
    /// <summary>
    /// FarmingEngine > Bakery > Setup Baking Panel 메뉴로 실행.
    /// UICanvas 아래에 BakingPanel 계층 구조를 자동 생성하고,
    /// 샘플 BreadRecipeData 에셋도 생성한다.
    /// </summary>
    public class BakingPanelSetupEditor : EditorWindow
    {
        [MenuItem("Farming Engine/Bakery/Setup Baking Panel")]
        public static void SetupBakingPanel()
        {
            // UICanvas 찾기
            Canvas uiCanvas = FindUICanvas();
            if (uiCanvas == null)
            {
                Debug.LogError("UICanvas를 씬에서 찾을 수 없습니다. UICanvas가 있는 씬에서 실행하세요.");
                return;
            }

            // 기존 BakingPanel 제거
            Transform existing = uiCanvas.transform.Find("BakingPanel");
            if (existing != null)
            {
                Undo.DestroyObjectImmediate(existing.gameObject);
            }

            // BakingPanel 루트 생성
            GameObject panelRoot = CreatePanel(uiCanvas.transform, "BakingPanel",
                new Vector2(0.05f, 0.02f), new Vector2(0.95f, 0.98f),
                new Color(0.12f, 0.1f, 0.08f, 0.97f));

            // UIPanel 컴포넌트 추가 (BakingPanel이 상속)
            BakingPanel bakingPanel = panelRoot.AddComponent<BakingPanel>();
            panelRoot.AddComponent<CanvasGroup>(); // UIPanel이 요구

            // ── 타이틀 바 ──────────────────────────────
            GameObject titleBar = CreateRect(panelRoot.transform, "TitleBar",
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -120), new Vector2(0, 0));
            var titleBg = titleBar.AddComponent<Image>();
            titleBg.color = new Color(0.08f, 0.06f, 0.04f, 1f);

            Text titleText = CreateText(titleBar.transform, "TitleText", "제빵소",
                60, Color.white, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 0), new Vector2(0, 0));
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.MiddleCenter;

            Button closeBtn = CreateButton(titleBar.transform, "CloseButton", "✕",
                new Vector2(1, 0), new Vector2(1, 1), new Vector2(-132, 12), new Vector2(0, -12));
            closeBtn.onClick.AddListener(() => {
                // 런타임에 패널 숨기기 (BakingPanel.Awake에서 처리)
            });
            closeBtn.GetComponentInChildren<Text>().fontSize = 54;
            closeBtn.GetComponentInChildren<Text>().color = new Color(0.8f, 0.8f, 0.8f);

            // ── 왼쪽 레시피 패널 ────────────────────────
            GameObject leftPanel = CreateRect(panelRoot.transform, "LeftPanel",
                new Vector2(0, 0), new Vector2(0.38f, 1), new Vector2(24, 24), new Vector2(-24, -168));
            var leftBg = leftPanel.AddComponent<Image>();
            leftBg.color = new Color(0.09f, 0.08f, 0.06f, 0.95f);

            Text leftTitle = CreateText(leftPanel.transform, "LeftTitle", "레시피",
                42, new Color(0.85f, 0.75f, 0.5f),
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(24, -96), new Vector2(-24, 0));
            leftTitle.alignment = TextAnchor.MiddleLeft;
            leftTitle.fontStyle = FontStyle.Bold;

            // ScrollRect
            GameObject scrollObj = CreateRect(leftPanel.transform, "RecipeScrollRect",
                new Vector2(0, 0), new Vector2(1, 1), new Vector2(12, 12), new Vector2(-12, -108));
            ScrollRect scrollRect = scrollObj.AddComponent<ScrollRect>();
            var scrollImg = scrollObj.AddComponent<Image>();
            scrollImg.color = Color.clear;

            // Viewport
            GameObject viewport = CreateRect(scrollObj.transform, "Viewport",
                new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            viewport.AddComponent<RectMask2D>();
            scrollRect.viewport = viewport.GetComponent<RectTransform>();

            // Content
            GameObject content = CreateRect(viewport.transform, "Content",
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 0), new Vector2(0, 0));
            var contentRT = content.GetComponent<RectTransform>();
            contentRT.pivot = new Vector2(0.5f, 1f);
            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 12; vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
            vlg.padding = new RectOffset(4, 4, 4, 4);
            var csf = content.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.content = contentRT;
            scrollRect.horizontal = false; scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            // BakingPanel 참조 설정
            bakingPanel.recipe_list_content = content.transform;

            // ── 오른쪽 상세 패널 ─────────────────────────
            GameObject rightPanel = CreateRect(panelRoot.transform, "RightPanel",
                new Vector2(0.38f, 0), new Vector2(1, 1), new Vector2(12, 24), new Vector2(-24, -168));
            var rightBg = rightPanel.AddComponent<Image>();
            rightBg.color = new Color(0.09f, 0.08f, 0.06f, 0.95f);

            // 레시피 제목
            Text recipeTitle = CreateText(rightPanel.transform, "RecipeTitleText", "레시피를 선택하세요",
                51, new Color(0.95f, 0.88f, 0.65f),
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(36, -132), new Vector2(-36, 0));
            recipeTitle.fontStyle = FontStyle.Bold;
            recipeTitle.alignment = TextAnchor.UpperLeft;
            bakingPanel.recipe_title_text = recipeTitle;

            // 레시피 설명
            Text recipeDesc = CreateText(rightPanel.transform, "RecipeDescText", "",
                36, new Color(0.75f, 0.72f, 0.65f),
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(36, -222), new Vector2(-36, -132));
            recipeDesc.alignment = TextAnchor.UpperLeft;
            bakingPanel.recipe_desc_text = recipeDesc;

            // 재료 레이블
            CreateText(rightPanel.transform, "IngredientsLabel", "필요 재료",
                39, new Color(0.85f, 0.75f, 0.5f),
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(36, -276), new Vector2(-36, -222));

            // 재료 컨테이너
            GameObject ingredientsContainer = CreateRect(rightPanel.transform, "IngredientsContainer",
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(36, -468), new Vector2(-36, -276));
            var hlg = ingredientsContainer.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 18; hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = false;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            bakingPanel.ingredients_container = ingredientsContainer.transform;

            // 구분선
            GameObject divider = CreateRect(rightPanel.transform, "Divider",
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(36, -480), new Vector2(-36, -474));
            var dividerImg = divider.AddComponent<Image>();
            dividerImg.color = new Color(0.3f, 0.28f, 0.22f, 1f);

            // Bake 버튼
            Button bakeBtn = CreateButton(rightPanel.transform, "BakeButton", "굽기 시작",
                new Vector2(0, 1), new Vector2(0.5f, 1), new Vector2(36, -564), new Vector2(-18, -480));
            SetButtonColor(bakeBtn, new Color(0.55f, 0.38f, 0.12f), new Color(0.7f, 0.52f, 0.2f));
            bakeBtn.GetComponentInChildren<Text>().fontSize = 42;
            bakingPanel.bake_button = bakeBtn;

            // 상태 텍스트
            Text statusText = CreateText(rightPanel.transform, "StatusText", "레시피를 선택하세요",
                33, new Color(0.75f, 0.72f, 0.65f),
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(36, -612), new Vector2(-36, -570));
            statusText.alignment = TextAnchor.MiddleLeft;
            bakingPanel.status_text = statusText;

            // 진행 슬라이더
            GameObject sliderObj = CreateRect(rightPanel.transform, "ProgressSlider",
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(36, -678), new Vector2(-36, -618));
            Slider slider = sliderObj.AddComponent<Slider>();
            slider.minValue = 0; slider.maxValue = 1; slider.value = 0;

            // Slider Background
            GameObject sliderBg = CreateRect(sliderObj.transform, "Background",
                new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            var sliderBgImg = sliderBg.AddComponent<Image>();
            sliderBgImg.color = new Color(0.2f, 0.18f, 0.14f);

            // Slider Fill Area
            GameObject fillArea = CreateRect(sliderObj.transform, "Fill Area",
                new Vector2(0, 0.25f), new Vector2(1, 0.75f), new Vector2(5, 0), new Vector2(-5, 0));
            GameObject fill = CreateRect(fillArea.transform, "Fill",
                new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0), new Vector2(0, 0));
            var fillImg = fill.AddComponent<Image>();
            fillImg.color = new Color(0.65f, 0.45f, 0.15f);

            slider.fillRect = fill.GetComponent<RectTransform>();
            slider.targetGraphic = sliderBgImg;
            slider.interactable = false;
            bakingPanel.progress_slider = slider;

            // ── 결과물 영역 ──────────────────────────────
            CreateText(rightPanel.transform, "OutputLabel", "완성품",
                36, new Color(0.85f, 0.75f, 0.5f),
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(36, -726), new Vector2(-36, -684));

            // 출력 버튼 (결과물 아이콘 포함)
            GameObject outputBtnObj = CreateRect(rightPanel.transform, "OutputButton",
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(36, -912), new Vector2(228, -726));
            Button outputBtn = outputBtnObj.AddComponent<Button>();
            var outputBtnImg = outputBtnObj.AddComponent<Image>();
            outputBtnImg.color = new Color(0.18f, 0.15f, 0.1f, 0.9f);
            SetButtonColor(outputBtn, new Color(0.18f, 0.15f, 0.1f), new Color(0.28f, 0.24f, 0.16f));
            outputBtn.interactable = false;

            // 출력 아이콘
            GameObject outputIconObj = CreateRect(outputBtnObj.transform, "OutputIcon",
                new Vector2(0.1f, 0.15f), new Vector2(0.9f, 0.85f), Vector2.zero, Vector2.zero);
            Image outputIcon = outputIconObj.AddComponent<Image>();
            outputIcon.color = Color.white;
            outputIcon.preserveAspect = true;
            outputIconObj.SetActive(false);

            // 수량 텍스트
            Text outputQty = CreateText(outputBtnObj.transform, "OutputQuantity", "",
                12, Color.white,
                new Vector2(0, 0), new Vector2(1, 0.3f), Vector2.zero, Vector2.zero);
            outputQty.alignment = TextAnchor.MiddleCenter;
            outputQty.gameObject.SetActive(false);

            bakingPanel.output_icon = outputIcon;
            bakingPanel.output_button = outputBtn;
            bakingPanel.output_quantity_text = outputQty;

            Undo.RegisterCreatedObjectUndo(panelRoot, "Create Baking Panel");
            EditorUtility.SetDirty(panelRoot);

            Debug.Log("BakingPanel 생성 완료! UICanvas 아래 BakingPanel을 확인하세요.");

            // 샘플 레시피 에셋 생성
            CreateSampleRecipeAssets();
        }

        [MenuItem("Farming Engine/Bakery/Create Display Shelf Panel")]
        public static void SetupDisplayShelfPanel()
        {
            Canvas uiCanvas = FindUICanvas();
            if (uiCanvas == null) { Debug.LogError("UICanvas 없음"); return; }

            Transform existing = uiCanvas.transform.Find("DisplayShelfPanel");
            if (existing != null) Undo.DestroyObjectImmediate(existing.gameObject);

            // StoragePanel과 동일한 크기/위치
            GameObject panelRoot = CreatePanel(uiCanvas.transform, "DisplayShelfPanel",
                new Vector2(0.6f, 0.35f), new Vector2(0.98f, 0.9f),
                new Color(0.12f, 0.1f, 0.08f, 0.97f));

            DisplayShelfPanel panel = panelRoot.AddComponent<DisplayShelfPanel>();
            panelRoot.AddComponent<CanvasGroup>();

            // 타이틀
            CreateText(panelRoot.transform, "TitleText", "진열대",
                18, Color.white,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(8, -44), new Vector2(-8, 0));

            // 슬롯 그리드 (6개) - Inspector에서 직접 ItemSlot 연결 필요
            // TODO: 슬롯 프리팹이 없으면 빈 그리드 표시
            GameObject gridObj = CreateRect(panelRoot.transform, "SlotGrid",
                new Vector2(0, 0), new Vector2(1, 1), new Vector2(8, 8), new Vector2(-8, -52));
            var glg = gridObj.AddComponent<GridLayoutGroup>();
            glg.cellSize = new Vector2(72, 72);
            glg.spacing = new Vector2(6, 6);
            glg.padding = new RectOffset(8, 8, 8, 8);

            Undo.RegisterCreatedObjectUndo(panelRoot, "Create Display Shelf Panel");
            Debug.Log("DisplayShelfPanel 생성 완료! Inspector에서 UISlotPanel.slots에 ItemSlot을 연결하세요.");
        }

        [MenuItem("Farming Engine/Bakery/Create Sample Recipe Assets")]
        public static void CreateSampleRecipeAssets()
        {
            string dir = "Assets/FarmingEngine_study/Resources/Bakery/Recipes";
            Directory.CreateDirectory(dir);

            CreateRecipeAsset(dir, "RecipeBreadBasic", "basic_bread", "기본 식빵",
                "밀가루와 계란으로 만드는 기본 식빵", 25f);

            CreateRecipeAsset(dir, "RecipeCroissant", "croissant", "크루아상",
                "버터가 듬뿍 들어간 바삭한 크루아상", 40f);

            CreateRecipeAsset(dir, "RecipeTomatoBread", "tomato_bread", "토마토 빵",
                "신선한 토마토가 들어간 건강 빵", 35f);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"샘플 레시피 에셋 생성 완료: {dir}");
            Debug.Log("생성된 에셋에서 required_items, result_item을 직접 연결해주세요.");
        }

        // ─── 헬퍼 메서드 ──────────────────────────────────────

        private static void CreateRecipeAsset(string dir, string fileName, string id, string title, string desc, float duration)
        {
            string path = $"{dir}/{fileName}.asset";
            if (AssetDatabase.LoadAssetAtPath<BreadRecipeData>(path) != null) return;

            var recipe = ScriptableObject.CreateInstance<BreadRecipeData>();
            recipe.id = id;
            recipe.title = title;
            recipe.desc = desc;
            recipe.bake_duration = duration;
            recipe.result_quantity = 1;
            recipe.unlocked_by_default = true;

            AssetDatabase.CreateAsset(recipe, path);
        }

        private static Canvas FindUICanvas()
        {
            Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            foreach (var c in canvases)
            {
                if (c.gameObject.name == "UICanvas" || c.gameObject.name.Contains("UICanvas"))
                    return c;
            }
            // fallback: 첫 번째 루트 캔버스
            foreach (var c in canvases)
            {
                if (c.isRootCanvas) return c;
            }
            return null;
        }

        private static GameObject CreatePanel(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, Color bgColor)
        {
            var obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            var rt = obj.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var bg = obj.AddComponent<Image>();
            bg.color = bgColor;
            // SetActive(false) 하지 않음 — UIPanel이 런타임에 자체 초기화로 숨김 처리
            return obj;
        }

        private static GameObject CreateRect(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            var rt = obj.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin; rt.offsetMax = offsetMax;
            return obj;
        }

        private static Text CreateText(Transform parent, string name, string content,
            int fontSize, Color color,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var obj = CreateRect(parent, name, anchorMin, anchorMax, offsetMin, offsetMax);
            var txt = obj.AddComponent<Text>();
            txt.text = content;
            txt.fontSize = fontSize;
            txt.color = color;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return txt;
        }

        private static Button CreateButton(Transform parent, string name, string label,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var obj = CreateRect(parent, name, anchorMin, anchorMax, offsetMin, offsetMax);
            var img = obj.AddComponent<Image>();
            img.color = new Color(0.3f, 0.22f, 0.1f);
            var btn = obj.AddComponent<Button>();

            var txtObj = new GameObject("Text", typeof(RectTransform));
            txtObj.transform.SetParent(obj.transform, false);
            var txtRT = txtObj.GetComponent<RectTransform>();
            txtRT.anchorMin = Vector2.zero; txtRT.anchorMax = Vector2.one;
            txtRT.offsetMin = txtRT.offsetMax = Vector2.zero;
            var txt = txtObj.AddComponent<Text>();
            txt.text = label; txt.fontSize = 15; txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            return btn;
        }

        private static void SetButtonColor(Button btn, Color normal, Color highlighted)
        {
            var colors = btn.colors;
            colors.normalColor = normal;
            colors.highlightedColor = highlighted;
            colors.pressedColor = highlighted * 0.8f;
            btn.colors = colors;
        }
    }
}
