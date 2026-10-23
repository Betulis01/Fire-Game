using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Skills tab: one sub-tab per weapon (Axe, Sword, Pickaxe, Wand, Bow). The chosen
// weapon's icon, weapon-XP bar and level, unspent skill points, and its skill tree.
// Weapons without a SkillTree on the player's WeaponExperience are greyed out.
public class SkillsTabUI : MonoBehaviour
{
    [SerializeField] HudRoot root;
    [SerializeField] TabButtonUI[] weaponTabs;
    [SerializeField] ToolKind[] weaponKinds;      // same order as weaponTabs
    [SerializeField] Image weaponIcon;
    [SerializeField] Image xpFill;                // Filled, horizontal
    [SerializeField] TMP_Text levelText;
    [SerializeField] TMP_Text pointsText;
    [SerializeField] SkillTreeUI tree;

    WeaponExperience progress;
    int current = -1;

    void Awake()
    {
        for (int i = 0; i < weaponTabs.Length; i++)
        {
            int index = i;
            weaponTabs[i].Clicked += () => Select(index);
        }
    }

    void OnEnable()
    {
        progress = root != null ? root.WeaponExperience : null;
        if (progress != null) progress.Changed += Refresh;

        for (int i = 0; i < weaponTabs.Length; i++)
            weaponTabs[i].SetInteractable(progress != null && progress.TreeFor(weaponKinds[i]) != null);

        Select(current >= 0 ? current : FirstAvailable());
    }

    void OnDisable()
    {
        if (progress != null) progress.Changed -= Refresh;
    }

    int FirstAvailable()
    {
        for (int i = 0; i < weaponKinds.Length; i++)
            if (progress != null && progress.TreeFor(weaponKinds[i]) != null) return i;
        return 0;
    }

    void Select(int index)
    {
        current = index;
        for (int i = 0; i < weaponTabs.Length; i++) weaponTabs[i].SetSelected(i == index);

        SkillTree skillTree = progress != null ? progress.TreeFor(weaponKinds[index]) : null;
        weaponIcon.enabled = skillTree != null && skillTree.icon != null;
        if (weaponIcon.enabled)
        {
            weaponIcon.sprite = skillTree.icon;
            weaponIcon.SetNativeSize();
        }
        tree.Show(skillTree, progress);
        Refresh();
    }

    void Refresh()
    {
        if (progress == null || current < 0) return;
        ToolKind kind = weaponKinds[current];
        xpFill.fillAmount = progress.Normalized(kind);
        levelText.text = progress.Level(kind).ToString();
        pointsText.text = "Points: " + progress.Points(kind);
        tree.Refresh();
    }
}
