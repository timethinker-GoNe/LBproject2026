using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Main manager script for dialogues and quests
/// </summary>

namespace DialogueQuests
{
    public class NarrativeManager : MonoBehaviour
    {
        public UnityAction<NarrativeEvent> onEventStart;
        public UnityAction<NarrativeEvent> onEventEnd;

        public UnityAction<NarrativeEventLine, DialogueMessage> onDialogueMessageStart;
        public UnityAction<NarrativeEventLine, DialogueMessage> onDialogueMessageEnd;

        public UnityAction onPauseGameplay;
        public UnityAction onUnpauseGameplay;

        public UnityAction<string, AudioClip, float> onPlaySFX;
        public UnityAction<string, AudioClip, float> onPlayMusic;
        public UnityAction<string> onStopMusic;

        public UnityAction<QuestData> onQuestStart;
        public UnityAction<QuestData> onQuestStep;
        public UnityAction<QuestData> onQuestComplete;
        public UnityAction<QuestData> onQuestFail;
        public UnityAction<QuestData> onQuestCancel;

        public Func<float> getTimestamp;

        [HideInInspector]
        public bool use_custom_audio = false; //Set this to true to use your own audio system (and connect to the audio 3 events)

        private NarrativeEvent current_event = null;
        private NarrativeEventLine current_event_line = null;
        private DialogueMessage current_message = null;
        private Actor current_player = null;
        private Actor current_triggerer = null;
        private ActorData current_actor = null;

        private List<TriggerListItem> trigger_list = new List<TriggerListItem>();
        private Queue<NarrativeEventLine> event_line_queue = new Queue<NarrativeEventLine>();
        private Dictionary<string, int> random_values = new Dictionary<string, int>();

        private float event_timer = 0f;
        private float pause_duration = 0f;
        private bool is_paused = false;
        private bool should_unpause = false;

        private static NarrativeManager _instance;
        private void Awake()
        {
            _instance = this;

            NarrativeData.LoadLast();
        }

        void Start()
        {

        }

        void Update()
        {
            //Events
            event_timer += Time.deltaTime;

            if (current_event != null)
            {
                //Stop dialogue
                if (current_message != null)
                {
                    bool auto_stop = current_event.dialogue_type == DialogueMessageType.InGame;
                    if (auto_stop)
                    {
                        if (event_timer > current_message.duration)
                        {
                            StopDialogue();
                        }
                    }
                }
                else if (current_event_line != null)
                {
                    if (event_timer > pause_duration)
                    {
                        StopEventLine();
                    }
                }
            }

            if (event_timer > pause_duration)
            {
                if (current_event_line == null && current_event != null && event_line_queue.Count > 0)
                {
                    NarrativeEventLine next = event_line_queue.Dequeue();
                    next.TriggerLineIfMet();
                }
                else if (current_event_line == null && current_event != null && event_line_queue.Count == 0)
                {
                    StopEvent();
                }
                else if (current_event == null && trigger_list.Count > 0)
                {
                    TriggerListItem next = GetPriorityTriggerList();
                    if (next != null)
                    {
                        RemoveFromTriggerList(next.evt);
                        ClearGroup(next.evt.GetGroup());
                        StartEvent(next.evt, next.player, next.triggerer);
                    }
                }
            }

            if (should_unpause && event_timer > 0.1f)
            {
                UnpauseGameplay();
            }

            //Time
            if (!is_paused)
            {
                NarrativeData.Get().time += Time.deltaTime;
            }

            //Automated quests
            if (current_event == null)
            {
                foreach (QuestData quest in QuestData.GetAll())
                {
                    if (quest is QuestAutoData)
                    {
                        Actor actor = Actor.GetPlayerActor();
                        QuestAutoData quest_auto = (QuestAutoData)quest;
                        UpdateAutoQuest(actor, quest_auto);
                    }
                }
            }
        }

        private void UpdateAutoQuest(Actor player, QuestAutoData quest_auto)
        {
            if (!quest_auto.IsStarted())
            {
                if (quest_auto.AreStartConditionsMet(player))
                    StartQuest(quest_auto);
            }

            else if (quest_auto.IsActive())
            {
                quest_auto.Update();
                if (quest_auto.AreConditionsMet(player))
                    CompleteQuest(quest_auto);
            }
        }

        public void AddToTriggerList(NarrativeEvent narrative_event, Actor player, Actor triggerer)
        {
            if (current_event == narrative_event)
                return; //This event is already running

            if (IsInTriggerList(narrative_event))
                return;

            TriggerListItem item = new TriggerListItem();
            item.evt = narrative_event;
            item.player = player;
            item.triggerer = triggerer;
            trigger_list.Add(item);
        }

        public void RemoveFromTriggerList(NarrativeEvent narrative_event)
        {
            for (int i = trigger_list.Count - 1; i >= 0; i--)
            {
                if (trigger_list[i].evt == narrative_event)
                    trigger_list.RemoveAt(i);
            }
        }

        public bool IsInTriggerList(NarrativeEvent narrative_event)
        {
            foreach (TriggerListItem item in trigger_list)
            {
                if (item.evt == narrative_event)
                    return true;
            }
            return false;
        }

        public void ClearGroup(NarrativeGroup group)
        {
            if (group == null)
                return;

            for (int i = trigger_list.Count - 1; i >= 0; i--)
            {
                if (trigger_list[i].evt.GetGroup() == group)
                    trigger_list.RemoveAt(i);
            }
        }

        private TriggerListItem GetPriorityTriggerList()
        {
            if (trigger_list.Count > 0)
            {
                TriggerListItem priority = trigger_list[0];
                foreach (TriggerListItem item in trigger_list)
                {
                    if (item.evt.GetGroupPriority() < priority.evt.GetGroupPriority())
                        priority = item;
                    else if (item.evt.GetGroupPriority() == priority.evt.GetGroupPriority() && item.evt.GetPriority() < priority.evt.GetPriority())
                        priority = item;
                }
                return priority;
            }
            return null;
        }

        public void StartEvent(NarrativeEvent narrative_event, Actor player, Actor triggerer)
        {
            if (current_event != narrative_event)
            {
                StopEvent();

                current_event = narrative_event;
                current_player = player;
                current_triggerer = triggerer;
                current_message = null;
                event_timer = 0f;
                should_unpause = is_paused && !current_event.pause_gameplay;

                current_event.IncreaseCounter();
                pause_duration = current_event.TriggerEffects(player, triggerer);

                foreach (NarrativeEventLine line in current_event.GetLines())
                    event_line_queue.Enqueue(line);

                if (onEventStart != null)
                    onEventStart.Invoke(narrative_event);
                if (narrative_event.onStart != null)
                    narrative_event.onStart.Invoke(player, triggerer);

                if (narrative_event.pause_gameplay && !is_paused && onPauseGameplay != null)
                {
                    PauseGameplay();
                }
            }
        }

        public void StopEvent()
        {
            StopDialogue();

            if (current_event != null)
            {
                NarrativeEvent trigger = current_event;
                Actor player = current_player;
                Actor triggerer = current_triggerer;

                current_event = null;
                current_event_line = null;
                current_message = null;
                current_actor = null;
                current_player = null;
                current_triggerer = null;
                event_timer = 0f;
                pause_duration = 0f;
                should_unpause = is_paused;

                event_line_queue.Clear();

                if (onEventEnd != null)
                    onEventEnd.Invoke(trigger);
                if (trigger.onEnd != null)
                    trigger.onEnd.Invoke(player, triggerer);
            }
        }

        public void StartEventLine(NarrativeEventLine line)
        {
            if (current_event_line != line)
            {
                current_event_line = line;
                current_message = null;
                event_timer = 0f;
                pause_duration = 0f;

                pause_duration = current_event_line.TriggerEffects(current_player, current_triggerer);

                if (line.dialogue != null)
                {
                    StartDialogue(line.dialogue);
                }
            }
        }

        public void StopEventLine()
        {
            if (current_event_line != null)
            {
                current_event_line = null;
                current_message = null;
                current_actor = null;
                event_timer = 0f;
                pause_duration = 0f;
            }
        }

        public void StartDialogue(DialogueMessage dialogue)
        {
            StopDialogue();

            current_message = dialogue;
            current_actor = dialogue.actor;
            pause_duration = current_message.pause;

            NarrativeData.Get().AddToHistory(new DialogueMessageData(current_actor.actor_id, current_actor.GetTitle(), current_message.GetText()));

            event_timer = 0f;

            if (onDialogueMessageStart != null)
                onDialogueMessageStart.Invoke(current_event_line, current_message);

            if (current_message.onStart != null)
                current_message.onStart.Invoke();
        }

        public void SelectChoice(int choice_index)
        {
            if (current_event_line != null && current_event_line.GetChoice(choice_index) != null) {

                StopDialogue();

                DialogueChoice choice = current_event_line.GetChoice(choice_index);

                if (choice.onSelect != null)
                    choice.onSelect.Invoke(choice.choice_index);

                if (choice.go_to != null)
                    choice.go_to.TriggerImmediately(GetCurrentPlayer(), GetCurrentTriggerer());
            }
        }

        public void StopDialogue()
        {
            if (current_message != null) {

                if (current_message.onEnd != null)
                    current_message.onEnd.Invoke();
                if (onDialogueMessageEnd != null)
                    onDialogueMessageEnd.Invoke(current_event_line, current_message);

                pause_duration = current_message.pause;
                current_message = null;
                current_actor = null;
                event_timer = 0f;
            }
        }

        public void StartQuest(QuestData quest)
        {
            if (quest != null && !quest.IsActive())
            {
                quest.Begin();

                if (onQuestStart != null)
                    onQuestStart.Invoke(quest);
                if (quest is QuestAutoData)
                    ((QuestAutoData)quest).OnStart();
            }
        }

        public void CompleteQuest(QuestData quest)
        {
            if (quest != null && quest.IsActive())
            {
                quest.Complete();

                if (onQuestComplete != null)
                    onQuestComplete.Invoke(quest);
                if (quest is QuestAutoData)
                    ((QuestAutoData)quest).OnComplete();
            }
        }

        public void FailQuest(QuestData quest)
        {
            if (quest != null && quest.IsActive())
            {
                quest.Fail();

                if (onQuestFail != null)
                    onQuestFail.Invoke(quest);
                if (quest is QuestAutoData)
                    ((QuestAutoData)quest).OnFail();
            }
        }

        public void CancelQuest(QuestData quest)
        {
            if (quest != null && quest.IsStarted())
            {
                quest.Cancel();

                if (onQuestCancel != null)
                    onQuestCancel.Invoke(quest);
                if (quest is QuestAutoData)
                    ((QuestAutoData)quest).OnCancel();
            }
        }

        public int GetQuestStatus(QuestData quest)
        {
            if (quest != null)
                return quest.GetQuestStatus();
            return 0;
        }

        public void SetQuestValue(QuestData quest, string progress, int value)
        {
            if (quest != null)
                quest.SetQuestValue(progress, value);
        }

        public void AddQuestValue(QuestData quest, string progress, int value)
        {
            if (quest != null)
                quest.AddQuestValue(progress, value);
        }

        public int GetQuestValue(QuestData quest, string progress)
        {
            if (quest != null)
                return quest.GetQuestValue(progress);
            return 0;
        }

        public void SetQuestStep(QuestData quest, int value)
        {
            if (quest != null)
                quest.SetQuestStep(value);
            if (onQuestStep != null)
                onQuestStep.Invoke(quest);
        }

        public void AddQuestStep(QuestData quest, int value = 1)
        {
            if (quest != null)
                quest.AddQuestStep(value);
            if (onQuestStep != null)
                onQuestStep.Invoke(quest);
        }

        public int GetQuestStep(QuestData quest)
        {
            if (quest != null)
                return quest.GetQuestStep();
            return 0;
        }
		
        public float GetTimestamp()
        {
            if (getTimestamp != null)
                return getTimestamp.Invoke();
            return NarrativeData.Get().time;
        }

        public void PlaySFX(string channel, AudioClip clip, float vol = 0.8f)
        {
            if (onPlaySFX != null)
                onPlaySFX.Invoke(channel, clip, vol);

            if (!use_custom_audio && TheAudio.Get())
                TheAudio.Get().PlaySFX(channel, clip, vol);
        }

        public void PlayMusic(string channel, AudioClip clip, float vol = 0.4f)
        {
            if (onPlayMusic != null)
                onPlayMusic.Invoke(channel, clip, vol);
            
            if(!use_custom_audio && TheAudio.Get())
                TheAudio.Get().PlayMusic(channel, clip, vol);
        }

        public void StopMusic(string channel)
        {
            if (onStopMusic != null)
                onStopMusic.Invoke(channel);
            
            if(!use_custom_audio && TheAudio.Get())
                TheAudio.Get().StopMusic(channel);
        }

        public void RollRandomValue(string id, int min, int max)
        {
            int value = UnityEngine.Random.Range(min, max + 1);
            random_values[id] = value;
        }

        public int GetRandomValue(string id)
        {
            if (random_values.ContainsKey(id))
                return random_values[id];
            return 0;
        }

        public void PauseGameplay()
        {
            if (!is_paused)
            {
                is_paused = true;
                onPauseGameplay?.Invoke();
            }
        }

        public void UnpauseGameplay()
        {
            if (is_paused)
            {
                is_paused = false;
                should_unpause = false;
                onUnpauseGameplay?.Invoke();
            }
        }

        public void UnpauseSoon()
        {
            should_unpause = is_paused;
        }

        //Currently has an event/dialogue running
        public bool IsRunning()
        {
            return (current_event != null);
        }

        public NarrativeEvent GetCurrent()
        {
            return current_event;
        }

        public NarrativeEventLine GetCurrentLine()
        {
            return current_event_line;
        }

        public DialogueMessage GetCurrentMessage()
        {
            return current_message;
        }

        public ActorData GetCurrentActor()
        {
            return current_actor;
        }

        public Actor GetCurrentPlayer()
        {
            return current_player;
        }

        public Actor GetCurrentTriggerer()
        {
            return current_triggerer;
        }

        public float GetEventTimer()
        {
            return event_timer;
        }

        public bool IsPaused()
        {
            return is_paused;
        }

        //Is enabled
        public static bool IsActive()
        {
            return _instance != null && _instance.enabled;
        }

        //Is ready to receive events (not already one running)
        public static bool IsReady()
        {
            return IsActive() && !_instance.IsRunning();
        }

        public static NarrativeManager Get()
        {
            return _instance;
        }
    }

    public class TriggerListItem
    {
        public NarrativeEvent evt;
        public Actor player;
        public Actor triggerer;
    }
}