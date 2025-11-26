
# Editor.md
# NoHeavyGear.cs
I got tired of watching players exploit Heavy Plate armor + vehicles.
This plugin blocks players from mounting into a vehicle with predefined armor.
The Json lets server owners set what armor is not allow in vehicles.

Heavy Plate is the default armor type in the .JSON

- **Permission:** *NoHeavyGear.bypass*

This permi bypasses any restrictons set in the .JSON file*

JSON:
```json
{
  "Version": "1.0.3",
  "Vehicles Affected By Weight Check (short prefab names)": [
    "minicopter.entity",
    "scraptransporthelicopter",
    "attackhelicopter.entity",
    "rowboat",
    "submarine.solo.entity",
    "submarine.duo.entity"
  ],
  "Blocked Wear Items (shortnames)": [
    "heavy.plate.helmet",
    "heavy.plate.jacket",
    "heavy.plate.pants"
  ],
  "Require All Listed Wear Items To Block (true = must wear all; false = any listed item blocks)": false
}
```

 ![](https://img.shields.io/github/release/pandao/editor.md.svg)
