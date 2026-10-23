// Body slots an Equippable can be worn in. Order is the UI's top-to-bottom order.
// Ring and Amulet are accessories: they show on the HUD's Player tab but have no
// paper-doll layer. Values are serialized as ints (Equippable.slot), so append new
// slots at the end rather than reordering.
public enum EquipSlot { Head, Chest, Legs, Ring, Amulet }
