using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;

namespace FarmingEngine
{
    /// <summary>
    /// StreamingAssets/Dialogue/{lang}.json 에서 대사 텍스트를 로드.
    /// NarrativeTool.Translate() 훅과 연결해서 사용.
    ///
    /// 키 네이밍 컨벤션: {npc_id}.{quest_id}.{line_id}
    /// 예) "grandma.tutorial_01.start"
    /// </summary>
    public static class DialogueLocalizer
    {
        private static Dictionary<string, string> _table;
        private static string _currentLang = "";

        public static string CurrentLang => _currentLang;

        // ── 초기화 ───────────────────────────────────────────────────────────

        /// <summary>언어 파일 로드. 앱 시작 시 또는 언어 변경 시 호출.</summary>
        public static void Load(string lang = "ko")
        {
            if (_currentLang == lang && _table != null) return;

            string path = Path.Combine(Application.streamingAssetsPath, "Dialogue", lang + ".json");

            if (!File.Exists(path))
            {
                Debug.LogWarning($"[DialogueLocalizer] 언어 파일 없음: {path}");
                // 한국어 폴백
                if (lang != "ko") Load("ko");
                return;
            }

            string json = File.ReadAllText(path, System.Text.Encoding.UTF8);
            _table = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
            _currentLang = lang;

            Debug.Log($"[DialogueLocalizer] 로드 완료: {lang}.json ({_table?.Count}개 항목)");
        }

        // ── 텍스트 조회 ──────────────────────────────────────────────────────

        /// <summary>
        /// 키로 현재 언어 텍스트 반환.
        /// 키가 없으면 키 자체를 반환 (개발 중 디버깅 편의).
        /// </summary>
        public static string Get(string key)
        {
            if (_table == null) Load();

            if (_table != null && _table.TryGetValue(key, out string text))
                return text;

            // 키가 없을 때: 키처럼 보이면(점 포함) 경고, 일반 텍스트면 그냥 반환
            if (key.Contains("."))
                Debug.LogWarning($"[DialogueLocalizer] 키 없음: '{key}'");

            return key;
        }

        // ── 언어 변경 ────────────────────────────────────────────────────────

        public static void SetLanguage(string lang)
        {
            if (_currentLang == lang) return;
            _table = null;
            Load(lang);
        }
    }
}
