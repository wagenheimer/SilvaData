import xml.etree.ElementTree as ET
from collections import Counter

try:
    tree = ET.parse('k:/Programas/CWSoftware/ISI Sys/ISISys/SilvaData/Resources/Localization/Localization.resx')
    root = tree.getroot()
    
    names = []
    for data in root.findall('data'):
        names.append(data.get('name'))
        
    counts = Counter(names)
    duplicates = [name for name, count in counts.items() if count > 1]
    
    if duplicates:
        print("Duplicates found:")
        for d in duplicates:
            print(f"- {d} ({counts[d]} times)")
    else:
        print("No duplicates found by name.")
        
except Exception as e:
    print(f"Error: {e}")
