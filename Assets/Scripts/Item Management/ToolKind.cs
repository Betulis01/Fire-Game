using System;

// The kind of a tool/weapon, used to gate what it can damage (axe -> trees,
// pickaxe -> ore, sword -> enemies). [Flags] so a target can accept several kinds
// while a tool carries a single one. Shield is intentionally absent: it's defensive,
// not a damage kind. Add new kinds as powers of two (Shovel = 8, ...).
[Flags]
public enum ToolKind
{
    None = 0,
    Axe = 1,
    Pickaxe = 2,
    Sword = 4,
    Fist = 8,     // empty-handed punch; hurts enemies but not trees/ore
}
