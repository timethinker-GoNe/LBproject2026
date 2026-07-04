using UnityEngine;

namespace FarmingEngine
{
    [CreateAssetMenu(fileName = "Action", menuName = "FarmingEngine/Actions/Bake", order = 50)]
    public class ActionBake : AAction
    {
        public override void DoAction(PlayerCharacter character, Selectable select)
        {
            BakeryOven oven = select.GetComponent<BakeryOven>();
            BakingPanel panel = BakingPanel.Get();
            if (panel != null && oven != null)
                panel.ShowBaking(character, oven);
        }

        public override bool CanDoAction(PlayerCharacter character, Selectable select)
        {
            return select.GetComponent<BakeryOven>() != null && BakingPanel.Get() != null;
        }
    }
}
