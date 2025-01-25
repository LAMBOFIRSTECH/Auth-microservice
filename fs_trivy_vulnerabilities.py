import json
import os


current_directory = os.getcwd()
project = os.path.basename(current_directory)

def find_file(filename, search_path='.'):
    for root, dirs, files in os.walk(search_path):
        if filename in files:
            return os.path.join(root, filename)
    return None

result = find_file("trivy_scan_report.json")
if not result:
    print("Fichier de rapport non trouvé.")
    exit(1)
    
print(f"Fichier trouvé : {result}")

file_path = 'trivy_scan_report.json' 

try:
    with open(file_path, 'r') as file:
        data = json.load(file)
except json.JSONDecodeError as e:
    print(f"Erreur de décodage JSON: {e}")
except FileNotFoundError:
    print(f"Le fichier {file_path} n'a pas été trouvé.")
except Exception as e:
    print(f"Une erreur s'est produite: {e}")

# Initialiser les listes pour les vulnérabilités et les secrets
vulnerabilities = []
secrets = []

# Extraction des vulnérabilités
for result in data.get("Results", []):
    target = result.get("Target", "Unknown Target")
    for vuln in result.get("Vulnerabilities", []):
        vulnerabilities.append({
            "Target": target,
            "VulnerabilityID": vuln.get("VulnerabilityID"),
            "PkgName": vuln.get("PkgName"),
            "Title": vuln.get("Title"),
            "InstalledVersion": vuln.get("InstalledVersion"),
            "FixedVersion": vuln.get("FixedVersion"),
            "Severity": vuln.get("Severity"),
            "PrimaryURL": vuln.get("PrimaryURL"),
            "PublishedDate": vuln.get("PublishedDate")
        })

    # Extraction des secrets
    for secret in result.get("Secrets", []):
        secrets.append({
            "Target": result.get("Target"),
            "Class": secret.get("Class"),
            "RuleID": secret.get("RuleID"),
            "Category": secret.get("Category"),
            "Severity": secret.get("Severity"),
            "Title": secret.get("Title"),
        })

# Générer le tableau HTML pour les vulnérabilités
html_table_vulnerabilities = f"""
<style>
    table {{
        width: 100%;
        border-collapse: collapse;
        table-layout: 12.5%; /* Fixe la largeur des colonnes */
    }}
    th, td {{
        padding: 12px;
        text-align: left;
        border: 1px solid #ddd;
        width: 12.5%; /* Fixe la largeur à 12.5% pour chaque colonne */
    }}
    th {{
        background-color: #4CAF50;
        color: white;
    }}
    tr:nth-child(even) {{
        background-color: #f2f2f2;
    }}
    tr:hover {{
        background-color: #ddd;
    }}
    a {{
        color: #4CAF50;
        text-decoration: none;
    }}
    a:hover {{
        text-decoration: underline;
    }}
    h2 {{
        text-align: center;
        color: #333;
        font-family: Arial, sans-serif;
    }}
</style>

<h2>Vulnerabilities Report for {project} project </h2>
<table>
    <tr>
        <th>Target</th>
        <th>Vulnerability ID</th>
        <th>Package Name</th>
        <th>Title</th>
        <th>Installed Version</th>
        <th>Fixed Version</th>
        <th>Severity</th>
        <th>Primary URL</th>
        <th>Published Date</th>
    </tr>
"""

for vuln in vulnerabilities:
    html_table_vulnerabilities += f"""
    <tr>
        <td>{vuln['Target']}</td>
        <td>{vuln['VulnerabilityID']}</td>
        <td>{vuln['PkgName']}</td>
        <td>{vuln['Title']}</td>
        <td>{vuln['InstalledVersion']}</td>
        <td>{vuln['FixedVersion']}</td>
        <td>{vuln['Severity']}</td>
        <td><a href="{vuln['PrimaryURL']}">Link</a></td>
        <td>{vuln['PublishedDate']}</td>
    </tr>
    """

html_table_vulnerabilities += "</table>"

# Générer le tableau HTML pour les secrets
html_table_secrets = f"""
<h2>Secrets Report for {project} project</h2>
<table>
    <tr>
        <th>Target</th>
        <th>Class</th>
        <th>Rule ID</th>
        <th>Category</th>
        <th>Severity</th>
        <th>Title</th>
    </tr>
"""

for secret in secrets:
    html_table_secrets += f"""
    <tr>
        <td>{secret['Target']}</td>
        <td>{secret['Class']}</td>
        <td>{secret['RuleID']}</td>
        <td>{secret['Category']}</td>
        <td>{secret['Severity']}</td>
        <td>{secret['Title']}</td>
    </tr>
    """

html_table_secrets += "</table>"

# Si vous souhaitez les enregistrer dans un fichier HTML:
with open("report.html", "w") as report_file:
    report_file.write(html_table_vulnerabilities)
    report_file.write("<br><br>")
    report_file.write(html_table_secrets)


print("Tableau généré : report.html")