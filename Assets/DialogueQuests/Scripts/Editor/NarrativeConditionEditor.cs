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

    [CustomEditor(typeof(NarrativeCondition)), CanEditMultipleObjects]
    public class NarrativeConditionEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            NarrativeCondition myScript = target as NarrativeCondition;

            ConditionData condition = AddScriptableObjectField<ConditionData>("Condition", myScript.condition);
            EditCondition(condition);

            if (condition != null)
            {
                if (condition.ShowValueData())
                {
                    ScriptableObject value = AddScriptableObjectField(condition.GetLabelValueData(), myScript.value_data, condition.GetDataType());
                    EditValueData(value);
                }

                if (condition.ShowTargetID())
                {
                    string id = AddTextField(condition.GetLabelTargetID(), myScript.target_id);
                    EditTargetID(id);
                }

                if (condition.ShowValueObject())
                {
                    GameObject obj = AddGameObjectField(condition.GetLabelValueObject(), myScript.value_object);
                    EditGameObject(obj);
                }

                if (condition.ShowOperatorInt())
                {
                    NarrativeConditionOperatorInt value = (NarrativeConditionOperatorInt)AddEnumField(condition.GetLabelOperatorInt(), myScript.oper);
                    EditOperator(value);
                }

                if (condition.ShowOperatorBool())
                {
                    NarrativeConditionOperatorBool value = (NarrativeConditionOperatorBool)AddEnumField(condition.GetLabelOperatorBool(), myScript.oper2);
                    EditOperator(value);
                }

                if (condition.ShowValueInt())
                {
                    int value = AddIntField(condition.GetLabelValueInt(), myScript.value_int);
                    EditInt(value);
                }

                if (condition.ShowValueFloat())
                {
                    float value = AddFloatField(condition.GetLabelValueFloat(), myScript.value_float);
                    EditFloat(value);
                }

                if (condition.ShowValueString())
                {
                    string value = AddTextField(condition.GetLabelValueString(), myScript.value_string);
                    EditString(value);
                }

                if (condition.ShowOtherTargetID())
                {
                    string id = AddTextField(condition.GetLabelOtherTargetID(), myScript.target_id);
                    EditOtherTargetID(id);
                }
            }

            if (GUI.changed && !Application.isPlaying)
            {
                EditorUtility.SetDirty(myScript);
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                AssetDatabase.SaveAssets();
            }
        }

        private void EditCondition(ConditionData value)
        {
            foreach (Object obj in targets)
            {
                NarrativeCondition cond = obj as NarrativeCondition;
                cond.condition = value;
            }
        }

        private void EditTargetID(string value)
        {
            foreach (Object obj in targets)
            {
                NarrativeCondition cond = obj as NarrativeCondition;
                cond.target_id = value;
            }
        }

        private void EditOtherTargetID(string value)
        {
            foreach (Object obj in targets)
            {
                NarrativeCondition cond = obj as NarrativeCondition;
                cond.other_target_id = value;
            }
        }

        private void EditGameObject(GameObject value)
        {
            foreach (Object obj in targets)
            {
                NarrativeCondition cond = obj as NarrativeCondition;
                cond.value_object = value;
            }
        }

        private void EditOperator(NarrativeConditionOperatorInt value)
        {
            foreach (Object obj in targets)
            {
                NarrativeCondition cond = obj as NarrativeCondition;
                cond.oper = value;
            }
        }

        private void EditOperator(NarrativeConditionOperatorBool value)
        {
            foreach (Object obj in targets)
            {
                NarrativeCondition cond = obj as NarrativeCondition;
                cond.oper2 = value;
            }
        }

        private void EditInt(int value)
        {
            foreach (Object obj in targets)
            {
                NarrativeCondition cond = obj as NarrativeCondition;
                cond.value_int = value;
            }
        }

        private void EditFloat(float value)
        {
            foreach (Object obj in targets)
            {
                NarrativeCondition cond = obj as NarrativeCondition;
                cond.value_float = value;
            }
        }

        private void EditString(string value)
        {
            foreach (Object obj in targets)
            {
                NarrativeCondition cond = obj as NarrativeCondition;
                cond.value_string = value;
            }
        }

        private void EditValueData(ScriptableObject value)
        {
            foreach (Object obj in targets)
            {
                NarrativeCondition cond = obj as NarrativeCondition;
                cond.value_data = value;
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
            T outval = EditorGUILayout.ObjectField(value, typeof(T), true, GetWidth()) as T;
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