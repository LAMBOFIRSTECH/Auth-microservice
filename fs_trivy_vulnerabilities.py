import json
import os


current_directory = os.getcwd()
project = os.path.basename(current_directory)

def find_file(filename, search_path='.',root=None):
    for root, dirs, files in os.walk(search_path):
        if filename in files:
            return os.path.join(root, filename)
    return None

result = find_file("trivy_scan_report.json")
if not result:
    print("Fichier de rapport non trouvé.")
    exit(1)
    
print(f"Fichier trouvé : {result}")

try:
    with open(result, 'r') as file:
        data = json.load(file)
except json.JSONDecodeError as e:
    print(f"Erreur de décodage JSON: {e}")
except FileNotFoundError:
    print(f"Le fichier {result} n'a pas été trouvé.")
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
html_base = f"""
<!DOCTYPE html>
<html lang="fr">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Vulnerabilities & Secrets Report</title>
    <style>
        /* Style général */
        body {{
            font-family: 'Arial', sans-serif;
            background-color: #f9f9f9;
            color: #333;
            margin: 0;
            padding: 0;
            line-height: 1.6;
        }}

        h1 {{
            text-align: center;
            color: #ffffff;
            background-color: #4CAF50;
            padding: 20px;
            margin: 0;
            font-size: 30px;
        }}

        h2 {{
            text-align: center;
            color: #333;
            font-size: 24px;
            margin-top: 30px;
            font-weight: bold;
        }}

        table {{
            width: 95%;
            margin: 20px auto;
            border-collapse: collapse;
            background-color: #ffffff;
            border-radius: 10px;
            box-shadow: 0 4px 8px rgba(0, 0, 0, 0.1);
        }}

        th, td {{
            padding: 15px; /* Augmenter le padding pour plus d'espacement */
            text-align: left;
            border: 1px solid #ddd;
            word-wrap: break-word;
            text-align: center; /* Centrer le texte dans les cellules */
        }}

        th {{
            background-color: #4CAF50;
            color: white;
            text-transform: uppercase;
            font-size: 14px;
        }}

        tr:nth-child(even) {{
            background-color: #f9f9f9;
        }}

        tr:hover {{
            background-color: #f1f1f1;
        }}

        td {{
            background-color: #f8f8f8;
            font-size: 14px;
        }}

        a {{
            color: #4CAF50;
            text-decoration: none;
        }}

        a:hover {{
            text-decoration: underline;
        }}

        .container {{
            width: 100%;
            max-width: 1200px;
            margin: 0 auto;
            padding: 20px;
        }}

        .table-section {{
            margin-bottom: 40px;
        }}

        .table-header {{
            font-weight: bold;
            text-transform: uppercase;
            font-size: 13px;
        }}

        /* Largeur homogène des colonnes */
        th, td {{
            width: 11%; /* Définir une largeur uniforme pour chaque colonne */
        }}

        /* Responsiveness */
        @media (max-width: 768px) {{
            table {{
                width: 100%;
                font-size: 12px;
            }}

            th, td {{
                padding: 10px;
            }}
        }}
    </style>
</head>
<body>
    <h1>Vulnerabilities & Secrets Report for {project} project</h1>
    <div class="container">
"""

# Générer le tableau HTML pour les vulnérabilités
html_table_vulnerabilities = f"""
    <div class="table-section">
        <h2>Vulnerabilities Report</h2>
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

# Remplir le tableau des vulnérabilités
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

html_table_vulnerabilities += "</table></div>"

# Générer le tableau HTML pour les secrets
html_table_secrets = f"""
    <div class="table-section">
        <h2>Secrets Report</h2>
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

# Remplir le tableau des secrets
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

html_table_secrets += "</table></div>"

# Combiner l'ossature HTML avec les tableaux
html_content = html_base + html_table_vulnerabilities + html_table_secrets + "</div></body></html>"

# Enregistrer le fichier HTML final
with open("report.html", "w") as report_file:
    report_file.write(html_content)

print("Tableau généré : report.html")