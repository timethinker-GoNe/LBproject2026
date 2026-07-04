using FarmingEngine;
using UnityEngine;

namespace FarmingEngine
{
    [CreateAssetMenu(fileName = "Action", menuName = "FarmingEngine/Actions/Dialogue", order = 50)]
    public class ActionNPCDialogue : AAction
    {
        public override void DoAction(PlayerCharacter character, Selectable select)
        {
            NPCDialogue npc = select.GetComponent<NPCDialogue>();
            if (npc != null)
                npc.OpenDialogue(character);
        }

        public override bool CanDoAction(PlayerCharacter character, Selectable select)
        {
            NPCDialogue npc = select.GetComponent<NPCDialogue>();
            return npc != null;
        }
    }
}