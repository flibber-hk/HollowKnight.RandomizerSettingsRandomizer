## Randomizer Settings Randomizer

Add-on for Randomizer 4 that allows you to randomize settings. Enable settings randomization in the Connections menu.

By default, the options "Disabled" (do not randomize settings) and "Full" (randomize all randomizable settings) are available. 
It is also possible to create a custom randomization configuration. To do so, create a .txt file in this mod's directory
with a list of fields to exclude from randomization. Options are:

* `ModuleName.FieldName` to exclude the named field from randomization (e.g. `PoolSettings.Skills` to make the Skills setting not
be randomized - so it will default to what was selected in the randomizer menu).

* `ModuleName` to exclude the entire module from randomization (e.g. `TransitionSettings` to make the transition settings default
to what was selected in the randomizer menu).

* `.FieldName` to exclude the named field from randomization (equivalent to `ModuleName.FieldName` where ModuleName is the module
containing the field).

Each option must be on a single line.

It is also possible to mark fields as being randomized (instead of excluded); to do so, simply include the line `INCLUDE` and subsequent
fields will be included in randomization. (The line `EXCLUDE` will cause subsequent fields to be excluded until the next include). If
a field is matched twice, it will behave according to the first match; if a field is not matched at all, its randomization will be the
opposite of the last INCLUDE/EXCLUDE (so ending on an INCLUDE will cause other fields to be excluded from randomization).

For example, the lines:

```
INCLUDE
SkipSettings.DarkRooms
PoolSettings.GeoRocks
EXCLUDE
SkipSettings
```
will cause:
* SkipSettings.DarkRooms to be randomized
* PoolSettings.GeoRocks to be randomized (this line is unnecessary, because the last INCLUDE/EXCLUDE is an EXCLUDE so settings are randomized by default).
* The rest of the skip settings to be excluded (so they will default to the values from the menu)
* The rest of the settings to be randomized.
