# Dayz Types Splitter script

Splits a DayZ `types.xml` into one file per category (`types_weapons.xml`, `types_food.xml`, ...).
Types without a `<category>` element go to `types_other.xml`. Tested against DayZ 1.28 vanilla `types.xml`.

GUI mode (pick file + output folder, press Run):

```
python DayZ-Types-Splitter.py
```

CLI mode (no GUI, good for scripting):

```
python DayZ-Types-Splitter.py path\to\types.xml path\to\output_dir
```

Note: to actually use the split files on a server you must register each one in
`cfgeconomycore.xml` (`<ce folder="db"><file name="types_weapons.xml" type="types"/></ce>` etc.).

# Dayz-C to Json SpawnObject convert script (only script no exe)

https://www.youtube.com/watch?v=PsFPHqpMoMM

python 3 necessary

if you want to convert c to json clone repo and unpack it

open DayZ-C_to_Json_Spawn_Points folder

in folder: 
```
files_to_convert(folder)
```

go files with extension .c (see example file in folder, you can conver multiply files at once)

next run

```
python C_to_Json_Spawn_Points.py
```

and that's it, in the json folder will be all the files you have converted ^^

you can now copy json to server files and add them to cfggameplay

```
    "objectSpawnersArr": [
      "xxx.json",
    ],
```
