import os
import re
import json
import logging

__location__ = os.path.realpath(os.path.join(os.getcwd(), os.path.dirname(__file__)))
c_folder = os.path.join(__location__, 'files_to_convert')
json_folder = os.path.join(__location__, 'json')

logging.basicConfig(level=logging.INFO)

if not os.path.exists(c_folder):
    logging.error(f"{c_folder} does not exist.")
    exit()

c_files = [filename for filename in os.listdir(c_folder) if filename.endswith(".c")]
if not c_files:
    logging.warning(f"No files found in {c_folder} possible to convert.")
    exit()

for filename in c_files:
    c_file = os.path.join(c_folder, filename)
    logging.info(f"Loading {c_file}...")
    with open(c_file) as f:
        lines = f.readlines()

    objects = []
    for line in lines:
        line = line.lstrip()
        if line.startswith("SpawnObject") or line.startswith("CustomObject"):
            name, pos, ypr = re.findall(r'"(.*?)"', line)
            pos = [float(x) for x in pos.split()]
            ypr = [float(x) for x in ypr.split()]
            objects.append({
                "name": name,
                "pos": pos,
                "ypr": ypr,
                "scale": 1
            })

    if objects:
        json_file = os.path.join(json_folder, f"{os.path.splitext(filename)[0]}.json")
        with open(json_file, 'w') as f:
            json.dump({"Objects": objects}, f)
        logging.info(f"Saved {json_file}.")
    else:
        logging.warning(f"No object data found in {c_file}.")