using UnityEngine;
using UnityEngine.UI;

// One action slot on the hand bar (key 1 = left hand, 2 = right hand): the active
// skill of that hand's weapon, with a cooldown overlay that drains from the top.
// Empty until weapons carry an IActiveSkill.
public class ActionSlotUI : MonoBehaviour
{
    [SerializeField] Image icon;
    [SerializeField] Image cooldown;   // Filled, vertical, from the top
    [SerializeField] Vector2 iconBox = new(56f, 56f);

    IActiveSkill skill;

    public void SetSkill(IActiveSkill newSkill)
    {
        skill = newSkill;
        UiPixel.FitIntegerScale(icon, skill?.Icon, iconBox);
        cooldown.enabled = skill != null;
        cooldown.fillAmount = 0f;
    }

    void Update()
    {
        // a weapon destroyed while slotted leaves a dead Unity object behind the
        // interface (interface == null can't see that), so test it as an Object
        if (skill is Object o && o == null) { SetSkill(null); return; }
        if (skill != null) cooldown.fillAmount = skill.Cooldown01;
    }
}
