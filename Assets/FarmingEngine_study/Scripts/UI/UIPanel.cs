using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace FarmingEngine
{

    /// <summary>
    /// Generic script for any UI panel, can be inherited 
    /// </summary>

    [RequireComponent(typeof(CanvasGroup))]
    public class UIPanel : MonoBehaviour
    {
        public float display_speed = 4f;

        [Header("UI 레이아웃")]
        [SerializeField] protected string layoutKey = "";

        public UnityAction onShow;
        public UnityAction onHide;

        private CanvasGroup canvas_group;
        private bool visible;

        protected virtual void Awake()
        {
            canvas_group = GetComponent<CanvasGroup>();
            canvas_group.alpha = 0f;
            visible = false;
            ApplyLayoutConfig();
        }

        private void ApplyLayoutConfig()
        {
            if (string.IsNullOrEmpty(layoutKey)) return;
            float minX, minY, maxX, maxY;
            if (!UILayoutConfig.TryGetAnchors(layoutKey, out minX, out minY, out maxX, out maxY)) return;
            var rt = GetComponent<RectTransform>();
            if (rt == null) return;
            rt.anchorMin = new Vector2(minX, minY);
            rt.anchorMax = new Vector2(maxX, maxY);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        protected virtual void Start()
        {

        }

        protected virtual void Update()
        {
            float add = visible ? display_speed : -display_speed;
            float alpha = Mathf.Clamp01(canvas_group.alpha + add * Time.deltaTime);
            canvas_group.alpha = alpha;

            // 완전히 보일 때만 raycasts 허용 — fade-out 중에 뒤의 버튼이 막히지 않도록
            canvas_group.blocksRaycasts = visible;
            canvas_group.interactable   = visible;

            if (!visible && alpha < 0.01f)
                AfterHide();
        }

        public virtual void Toggle(bool instant = false)
        {
            if (IsVisible())
                Hide(instant);
            else
                Show(instant);
        }

        public virtual void Show(bool instant = false)
        {
            // SetActive 먼저 — 비활성 오브젝트에서 처음 호출될 때
            // Awake()가 SetActive(true) 내부에서 실행되어 visible=false로 리셋되기 때문에
            // visible=true는 반드시 SetActive 이후에 설정해야 한다.
            gameObject.SetActive(true);
            visible = true;

            if (instant || display_speed < 0.01f)
                canvas_group.alpha = 1f;

            if (onShow != null)
                onShow.Invoke();
        }

        public virtual void Hide(bool instant = false)
        {
            visible = false;
            if (instant || display_speed < 0.01f)
                canvas_group.alpha = 0f;

            if (onHide != null)
                onHide.Invoke();
        }

        public void SetVisible(bool visi)
        {
            if (!visible && visi)
                Show();
            else if (visible && !visi)
                Hide();
        }

        public virtual void AfterHide()
        {
            gameObject.SetActive(false);
        }

        public bool IsVisible()
        {
            return visible;
        }

        public bool IsFullyVisible()
        {
            return visible && canvas_group.alpha > 0.99f;
        }

        public bool IsFullyHidden()
        {
            return !visible && !gameObject.activeSelf;
        }

        public float GetAlpha()
        {
            return canvas_group.alpha;
        }
    }

}