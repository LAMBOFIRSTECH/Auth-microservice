import os
import re

def find_cs_files(search_path='.'):
    cs_files = []
    for root, dirs, files in os.walk(search_path):
        # Exclure les répertoires contenant "test", "Test", ".Test", ou ".test"
        dirs[:] = [d for d in dirs if not re.search(r'(test|Test|\.(test|Test))', d, re.IGNORECASE)]
        
        for file in files:
            # Vérifie si le fichier se termine par .cs
            if file.endswith(".cs"):
                cs_files.append(os.path.join(root, file))
    return cs_files

# Trouver tous les fichiers .cs dans le répertoire courant et sous-répertoires
cs_files = find_cs_files(search_path=".")
if not cs_files:
    print("Aucun fichier .cs trouvé.")
    exit(1)

def calculate_cyclomatic_complexity(file_path):
    keywords = ['if', 'for', 'while', 'case', 'catch', '||', '&&', 'foreach']
    complexity = 1  # Commence toujours à 1
    with open(file_path, 'r') as file:
        for line in file:
            for keyword in keywords:
                if keyword in line:
                    complexity += 1
    return complexity

def calculate_cognitive_complexity(file_path):
    cognitive_complexity = 0
    with open(file_path, 'r') as file:
        for line in file:
            # Compter les structures conditionnelles complexes
            cognitive_complexity += len(re.findall(r'(if|for|while|case|else if|foreach|&&|\|\|)', line))
            if re.search(r'\{.*\}', line):
                cognitive_complexity += 1
    return cognitive_complexity

total_cyclomatic_complexity = 0
total_cognitive_complexity = 0

# Calculer et afficher la complexité cyclomatique pour chaque fichier trouvé
for file_path in cs_files:
    file_cyclomatic_complexity = calculate_cyclomatic_complexity(file_path)
    file_cognitive_complexity = calculate_cognitive_complexity(file_path)
    total_cyclomatic_complexity += file_cyclomatic_complexity
    total_cognitive_complexity += file_cognitive_complexity
    if file_cyclomatic_complexity >= 25 and file_cognitive_complexity >= 20:
        print(f"Fichier trouvé : {file_path}")
        print(f"Complexité cyclomatique : {file_cyclomatic_complexity}\n")
        print(f"Complexité cognitive : {file_cognitive_complexity}\n")

# Afficher le total de la complexité cyclomatique
print(f"Total de la complexité cyclomatique de tous les fichiers : {total_cyclomatic_complexity}")
print(f"Total de la complexité cognitive de tous les fichiers : {total_cognitive_complexity}")
