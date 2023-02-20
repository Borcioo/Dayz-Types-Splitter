# Dayz Types Splitter script

if you dont want to run exe file you can run it using commend line and python3

```
python DayZ-Types-Splitter.py
```

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
