import os
import shutil
import sys

# Paths
source_dir = r"c:\Users\adna\Documents\PROJETOS\03-APLICATIVOS-CSHARP\LETREIRO-DIGITAL-WPF"
bin_debug_dir = os.path.join(source_dir, "bin", "Debug", "net8.0-windows")
versao_teste_dir = os.path.join(source_dir, "_VERSAO_TESTE")
dev_projeto_dir = os.path.join(source_dir, "_DEV_PROJETO")

print(f"Source: {source_dir}")
print(f"Bin Debug: {bin_debug_dir}")

# Create destination folders if needed
if not os.path.exists(versao_teste_dir):
    os.makedirs(versao_teste_dir)
    print(f"Created {versao_teste_dir}")
if not os.path.exists(dev_projeto_dir):
    os.makedirs(dev_projeto_dir)
    print(f"Created {dev_projeto_dir}")

# Copy Build Artifacts
print("--- Copying Build Artifacts ---")
if os.path.exists(bin_debug_dir):
    for item in os.listdir(bin_debug_dir):
        s = os.path.join(bin_debug_dir, item)
        d = os.path.join(versao_teste_dir, item)
        print(f"Copying {item}...")
        try:
            if os.path.isdir(s):
                if os.path.exists(d):
                    shutil.rmtree(d)
                shutil.copytree(s, d)
            else:
                shutil.copy2(s, d)
        except Exception as e:
            print(f"Error copying {item}: {e}")
else:
    print(f"ERROR: {bin_debug_dir} not found!")

# Move Source Files
print("--- Moving Source Files ---")
items = os.listdir(source_dir)
exclude = ["_VERSAO_TESTE", "_DEV_PROJETO", "organize.py", "organize_log.txt", "organize.bat"]

for item in items:
    if item in exclude:
        continue
    
    s = os.path.join(source_dir, item)
    d = os.path.join(dev_projeto_dir, item)
    
    print(f"Moving {item}...")
    try:
        shutil.move(s, d)
    except Exception as e:
        print(f"Error moving {item}: {e}")

print("Done.")
