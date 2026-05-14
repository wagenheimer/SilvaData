import sys
import os

file_path = r'k:\Programas\CWSoftware\ISI Sys\ISISys\SilvaData\Resources\Localization\Localization.Designer.cs'

def remove_block(lines, start_line_num):
    # Search backwards for the start of the summary
    idx = start_line_num - 1
    while idx > 0 and '/// <summary>' not in lines[idx]:
        idx -= 1
    
    # Search forwards for the closing brace
    end_idx = start_line_num - 1
    # Property looks like:
    # public static string Prop {
    #     get { return ...; }
    # }
    # So we need to find TWO closing braces or just find the one that corresponds to the property
    braces = 0
    found_first_brace = False
    while end_idx < len(lines):
        if '{' in lines[end_idx]:
            braces += lines[end_idx].count('{')
            found_first_brace = True
        if '}' in lines[end_idx]:
            braces -= lines[end_idx].count('}')
        
        if found_first_brace and braces == 0:
            break
        end_idx += 1
    
    return idx, end_idx + 1

if os.path.exists(file_path):
    with open(file_path, 'r', encoding='utf-8-sig') as f:
        lines = f.readlines()

    removals = [858]
    removals.sort(reverse=True)

    for r in removals:
        start, end = remove_block(lines, r)
        print(f"Removing lines {start+1} to {end}")
        del lines[start:end]

    with open(file_path, 'w', encoding='utf-8-sig') as f:
        f.writelines(lines)
    print("Successfully cleaned designer file.")
else:
    print(f"File not found: {file_path}")
