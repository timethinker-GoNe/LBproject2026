using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

#if SURVIVAL_ENGINE || SURVIVAL_ENGINE_ONLINE
using SurvivalEngine;
#endif

#if FARMING_ENGINE
using FarmingEngine;
#endif

namespace DialogueQuests.EditorTool
{

    [CustomEditor(typeof(NarrativeEffect)), CanEditMultipleObjects]
    public class NarrativeEffectEditor : Editor
    {
        SerializedProperty sprop;

        internal void OnEnable()
        {
            sprop = serializedObject.FindProperty("callfunc_evt");
        }

        public override void OnInspectorGUI()
        {
            NarrativeEffect myScript = target as NarrativeEffect;

            EffectData effect = AddScriptableObjectField<EffectData>("Effect", myScript.effect);
            EditEffect(effect);

            if (effect != null)
            {
                if (effect.ShowValueData())
                {
                    ScriptableObject value = AddScriptableObjectField(effect.GetLabelValueData(), myScript.value_data, effect.GetDataType());
                    EditValueData(value);
                }

                if (effect.ShowTargetID())
                {
                    string id = AddTextField(effect.GetLabelTargetID(), myScript.target_id);
                    EditTargetID(id);
                }

                if (effect.ShowValueObject())
                {
                    GameObject obj = AddGameObjectField(effect.GetLabelValueObject(), myScript.value_object);
                    EditGameObject(obj);
                }

                if (effect.ShowValueAudio())
                {
                    AudioClip value = AddAudioField(effect.GetLabelValueAudio(), myScript.value_audio);
                    EditAudio(value);
                }

                if (effect.ShowOperator())
                {
                    NarrativeEffectOperator value = (NarrativeEffectOperator)AddEnumField(effect.GetLabelOperator(), myScript.oper);
                    EditOperator(value);
                }

                if (effect.ShowValueInt())
                {
                    int value = AddIntField(effect.GetLabelValueInt(), myScript.value_int);
                    EditInt(value);
                }

                if (effect.ShowValueFloat())
                {
                    float value = AddFloatField(effect.GetLabelValueFloat(), myScript.value_float);
                    EditFloat(value);
                }

                if (effect.ShowValueString())
                {
                    string value = AddTextField(effect.GetLabelValueString(), myScript.value_string);
                    EditString(value);
                }

                if (effect.ShowFunction())
                {
                    //EditorGUIUtility.LookLikeControls();
                    GUILayout.BeginHorizontal();
                    EditorGUI.BeginChangeCheck();
                    {
                        EditorGUILayout.PropertyField(sprop, new GUIContent(effect.GetLabelFunction(), ""));
                    }

                    if (GUI.changed)
                    {
                        serializedObject.ApplyModifiedProperties();
                    }
                    GUILayout.EndHorizontal();
                }
            }

            if (GUI.changed && !Application.isPlaying)
            {
                EditorUtility.SetDirty(myScript);
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                AssetDatabase.SaveAssets();
            }
        }

        private void EditEffect(EffectData value)
        {
            foreach (Object obj in targets)
            {
                NarrativeEffect cond = obj as NarrativeEffect;
                cond.effect = value;
            }
        }

        private void EditTargetID(string value)
        {
            foreach (Object obj in targets)
            {
                NarrativeEffect eff = obj as NarrativeEffect;
                eff.target_id = value;
            }
        }

        private void EditGameObject(GameObject value)
        {
            foreach (Object obj in targets)
            {
                NarrativeEffect eff = obj as NarrativeEffect;
                eff.value_object = value;
            }
        }

        private void EditOperator(NarrativeEffectOperator value)
        {
            foreach (Object obj in targets)
            {
                NarrativeEffect eff = obj as NarrativeEffect;
                eff.oper = value;
            }
        }

        private void EditInt(int value)
        {
            foreach (Object obj in targets)
            {
                NarrativeEffect eff = obj as NarrativeEffect;
                eff.value_int = value;
            }
        }

        private void EditFloat(float value)
        {
            foreach (Object obj in targets)
            {
                NarrativeEffect eff = obj as NarrativeEffect;
                eff.value_float = value;
            }
        }

        private void EditString(string value)
        {
            foreach (Object obj in targets)
            {
                NarrativeEffect eff = obj as NarrativeEffect;
                eff.value_string = value;
            }
        }

        private void EditAudio(AudioClip value)
        {
            foreach (Object obj in targets)
            {
                NarrativeEffect eff = obj as NarrativeEffect;
                eff.value_audio = value;
            }
        }

        private void EditValueData(ScriptableObject value)
        {
            foreach (Object obj in targets)
            {
                NarrativeEffect eff = obj as NarrativeEffect;
                eff.value_data = value;
            }
        }

        private string AddTextField(string label, string value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GetLabelWidth());
            GUILayout.FlexibleSpace();
            string outval = EditorGUILayout.TextField(value, GetWidth());
            GUILayout.EndHorizontal();
            return outval;
        }

        private string AddTextAreaField(string label, string value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GetShortLabelWidth());
            GUILayout.FlexibleSpace();
            EditorStyles.textField.wordWrap = true;
            string outval = EditorGUILayout.TextArea(value, GetLongWidth(), GUILayout.Height(50));
            GUILayout.EndHorizontal();
            return outval;
        }

        private int AddIntField(string label, int value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GetLabelWidth());
            GUILayout.FlexibleSpace();
            int outval = EditorGUILayout.IntField(value, GetWidth());
            GUILayout.EndHorizontal();
            return outval;
        }

        private float AddFloatField(string label, float value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GetLabelWidth());
            GUILayout.FlexibleSpace();
            float outval = EditorGUILayout.FloatField(value, GetWidth());
            GUILayout.EndHorizontal();
            return outval;
        }

        private System.Enum AddEnumField(string label, System.Enum value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GetLabelWidth());
            GUILayout.FlexibleSpace();
            System.Enum outval = EditorGUILayout.EnumPopup(value, GetWidth());
            GUILayout.EndHorizontal();
            return outval;
        }

        private GameObject AddGameObjectField(string label, GameObject value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GetLabelWidth());
            GUILayout.FlexibleSpace();
            GameObject outval = (GameObject)EditorGUILayout.ObjectField(value, typeof(GameObject), true, GetWidth());
            GUILayout.EndHorizontal();
            return outval;
        }

        private ScriptableObject AddScriptableObjectField(string label, ScriptableObject value, System.Type type)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GetLabelWidth());
            GUILayout.FlexibleSpace();
            ScriptableObject outval = EditorGUILayout.ObjectField(value, type, true, GetWidth()) as ScriptableObject;
            GUILayout.EndHorizontal();
            return outval;
        }

        private T AddScriptableObjectField<T>(string label, ScriptableObject value) where T : ScriptableObject
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GetLabelWidth());
            GUILayout.FlexibleSpace();
            T outval = (T)EditorGUILayout.ObjectField(value, typeof(T), true, GetWidth());
            GUILayout.EndHorizontal();
            return outval;
        }

        private Sprite AddSpriteField(string label, Sprite value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GetLabelWidth());
            GUILayout.FlexibleSpace();
            Sprite outval = (Sprite)EditorGUILayout.ObjectField(value, typeof(Sprite), true, GetWidth());
            GUILayout.EndHorizontal();
            return outval;
        }

        private AudioClip AddAudioField(string label, AudioClip value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GetLabelWidth());
            GUILayout.FlexibleSpace();
            AudioClip outval = (AudioClip)EditorGUILayout.ObjectField(value, typeof(AudioClip), true, GetWidth());
            GUILayout.EndHorizontal();
            return outval;
        }
		
		private bool AddToggleField(string label, bool value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GetLabelWidth());
            GUILayout.FlexibleSpace();
            bool outval = EditorGUILayout.Toggle(value, GetWidth());
            GUILayout.EndHorizontal();
            return outval;
        }

        private GUILayoutOption GetLabelWidth()
        {
            return GUILayout.Width(EditorGUIUtility.currentViewWidth * 0.4f);
        }

        private GUILayoutOption GetWidth()
        {
            return GUILayout.Width(EditorGUIUtility.currentViewWidth * 0.45f);
        }

        private GUILayoutOption GetShortLabelWidth()
        {
            return GUILayout.Width(EditorGUIUtility.currentViewWidth * 0.25f);
        }

        private GUILayoutOption GetLongWidth()
        {
            return GUILayout.Width(EditorGUIUtility.currentViewWidth * 0.65f);
        }
    }

}