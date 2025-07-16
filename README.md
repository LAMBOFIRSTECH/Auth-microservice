# 🔐 Auth‑Microservice

[![Build Status](https://img.shields.io/badge/build-passing-brightgreen)](#) 
[![Coverage](https://img.shields.io/badge/coverage-90%25-blue)](#)  
[![Trivy](https://img.shields.io/badge/trivy-passed-success)](#)
[![Sonar](https://img.shields.io/badge/sonarqube-clean-orange)](#)

> **Note**: A diagram summarizing the project is available below.

---

## 🧰 Project Context

As part of our portfolio **Human Resources Management** project, this microservice acts as a **fallback authentication provider**, serving as a backup to **Keycloak**, our primary identity provider.

---

## ⚙️ Key Features

This service handles:

📅 **User authentication** using an **OpenLDAP** directory
🔑 **JWT token generation** (access & refresh)
🪀 **Redis** for high-performance token caching
📬 **RabbitMQ** for asynchronous messaging between OpenLDAP and the app (e.g., account updates)
🔐 **HashiCorp Vault** for secure secrets management (tokens, keys, credentials)
📦 **Modular and containerized deployment** with Docker

💡 This component integrates seamlessly into a **microservices architecture** while ensuring:

* 🔒 Enhanced security with Vault
* 📊 Auditability with Trivy & SonarQube
* ♻️ High reliability when Keycloak is unavailable
* ⚙️ Compatibility with standard identity services

---

## 🧽 Table of Contents

* [📦 Installation](#-installation)
* [🚀 Getting Started](#-getting-started)
* [🧹 Architecture](#-architecture)
* [🧪 Tests](#-tests)
* [⚙️ CI/CD & Quality](#-cicd--quality)
* [🤝 Contributing](#-contributing)
* [📄 License](#-license)

---

## 📦 Installation

### Prerequisites

* [.NET SDK](https://dotnet.microsoft.com/)
* Docker / Docker Compose
* Git

```bash
git clone https://github.com/LAMBOFIRSTECH/Auth-microservice.git
cd Auth-microservice
```

---

## 🚀 Getting Started

Several services must be running before you can test the application locally:

1. Run **Redis** (via Docker) to handle token and refresh token caching
2. Run **OpenLDAP** (via Docker) as the user account storage
3. Run **RabbitMQ** (via Docker) to enable message-based communication between OpenLDAP and the app
4. Navigate to the authentication service directory:

   ```bash
   cd Authentifications
   dotnet run Authentifications
   dotnet test Authentifications.Tests
   ```

---

## 🧹 Architecture

![Diagram](./Microservice-authentication.png)

---

## ⚙️ CI/CD & Quality Assurance

The project leverages a **GitLab CI/CD pipeline** composed of multiple stages:

| Stage                         | Description                                                                                                                                                   |
| ----------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| ⚙️ `pre-treatment`            | Cleans the CI environment: removes temporary folders (`bin/`, `obj/`, `TestResults/`), clears local NuGet packages, resets the repo.                          |
| 🛠️ `build`                   | Restores dependencies with `dotnet restore` and compiles the project in `Release` mode.                                                                       |
| ✅ `test`                      | Executes **unit tests** using `dotnet test`.                                                                                                                  |
| 🔍 `scan-vulnerabilities`     | Builds the Docker image and performs a **vulnerability scan** with [Trivy](https://github.com/aquasecurity/trivy) (image, filesystem, and .NET dependencies). |
| 📊 `sonar-build-and-analysis` | Analyzes code quality with **SonarQube**: detects bugs, code smells, duplication, etc.                                                                        |
| 🚀 `deploy` (Dev & Staging)   | Deploys the Docker image to the **development** (`develop`) or **staging** (`main`) environment via `docker-compose` with secure config.                      |
| 🩺 `health_check`             | Verifies that the application is running and accessible via the **health endpoint** (`$HEALTH_ENDPOINT`). Triggers rollback if needed.                        |
| ⟳ `rollback_staging`          | Allows **manual or automatic rollback** to the last working image if deployment fails.                                                                        |

> 💡 This pipeline ensures **end-to-end validation** before production: from compilation to quality scanning, with built-in **security**, **automatic rollback**, and **controlled deployment**.

---

## 🤝 Contributing

Fork → Branch → Pull Request…
We welcome contributions! Feel free to improve documentation, suggest features, or fix issues.

---
