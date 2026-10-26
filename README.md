# The Fenrir: Local GenAI-Powered NPC Interaction Framework

> **Undergraduate Thesis Project (Skripsi)**  
> **Topic:** Multimedia & Game — Computer Science, BINUS University (2026)  
> **Student:** Kevin Khemananado Kan (NIM: 2702344665)  
> **Advisor:** Dr. Hady Pranoto, S.Kom., M.T.I. (D1832)

---

## 📌 Overview

**The Fenrir** is a 2D top-down Action RPG developed in **Unity (C#)** featuring an offline, local-first Generative AI dialogue pipeline. Traditional RPG Non-Player Characters (NPCs) rely heavily on static dialogue trees that become repetitive and immersion-breaking. While cloud-based LLM solutions exist, they incur high API costs, require persistent internet connections, and introduce non-deterministic hallucinations.

This project implements a lightweight **C# middleware** that communicates asynchronously via local REST API with a **4-bit quantized Small Language Model (Llama 3.2 1B)** hosted on **Ollama**. The system is specifically optimized for **Resource-Constrained Environments (APUs / Integrated Graphics with 16GB Shared RAM)** and utilizes automated **Context Injection** to enforce strict narrative consistency without requiring free-text user input.

---

## 🎮 Core Game Mechanics & Story

* **Story Synopsis:** For 50 years, the village of Aethelgard (Oakhaven) lived in peace until the ancient **Ashen Fenrir** woke from slumber, starving and besieging the borders with feral wolves. The player must gather information from the Village Elder, cull lesser wolves in the forest, and grow strong enough to face the beast.
* **Core Loop:**
  1. **Village Hub (Scene 1):** Talk to the Village Elder via context-injected dynamic dialogue.
  2. **Forest (Scene 2):** Real-time melee combat against respawning lesser wolves to gain EXP.
  3. **Forest Gate (Scene 3):** Gated barrier that verifies player progression (requires **Level 5**).
  4. **Boss Arena (Scene 4):** Climactic battle against the Ashen Fenrir.
* **Controls:**
  * `W`, `A`, `S`, `D`: Top-down 2D character movement.
  * `Left Mouse Button`: Melee attack swing / Progress dialogue.
  * `E`: Interact with NPCs within proximity trigger radius.

---

## 🛠️ System Architecture

```
[ Unity Client (C#) ]
       │
       ▼ (Proximity + 'E' Key)
[ ContextInjector.cs ] ─── Merges Lore, Persona & Player Level into System Prompt
       │
       ▼ (JSON Serialization)
[ OllamaAPIClient.cs ] ─── UnityWebRequest (HTTP POST, Asynchronous Coroutine)
       │
       ▼ (REST API: http://localhost:11434/api/generate)
[ Ollama Server (Local OS Process) ]
       │
       ▼ (4-bit GGUF Inference)
[ Llama 3.2 1B ] (num_ctx: 1024, num_predict: 80)
       │
       ▼ (JSON Response / 5s Timeout Fallback)
[ DialogueSystem.cs ] ─── Typewriter UI Stream on Dialogue Canvas
```

### Key Technical Parameters:
* **Inference Engine:** [Ollama](https://ollama.com/) (Local HTTP server on port `11434`).
* **Model:** `Llama 3.2 1B` (GGUF 4-bit Post-Training Quantization).
* **Context Window (`num_ctx`):** `1024` tokens (conserves APU shared memory bandwidth).
* **Predict Token Limit (`num_predict`):** `80` tokens (1–2 concise in-character sentences).
* **Safety Safeguard (UC-03 Exception E-1):** 5.0-second HTTP timeout fallback to prevent game freeze.

---

## 📂 Repository Structure

```
genai-npc-project/
│
├── unity-game-client/               # Unity 2022.3 LTS Project
│   ├── Assets/
│   │   ├── Scenes/
│   │   │   ├── VillageHub.unity     # Hub area with Village Elder & Houses
│   │   │   ├── ForestGrind.unity    # Mob combat & level grinding zone
│   │   │   └── BossArena.unity      # Boss encounter with Ashen Fenrir
│   │   │
│   │   ├── Scripts/
│   │   │   ├── AI/
│   │   │   │   ├── OllamaAPIClient.cs   # Asynchronous UnityWebRequest handler
│   │   │   │   ├── ContextInjector.cs   # Automated narrative system prompt builder
│   │   │   │   └── JSONPayloads.cs      # Data contracts for REST request/response
│   │   │   ├── Core/
│   │   │   │   ├── GameManager.cs       # Scene management & game state tracking
│   │   │   │   └── InteractionManager.cs
│   │   │   ├── Entities/
│   │   │   │   ├── PlayerController.cs  # Movement, animation & dialogue lock
│   │   │   │   ├── NPCController.cs     # Trigger collider & interaction logic
│   │   │   │   └── EnemyController.cs   # Combat AI & health logic
│   │   │   └── UI/
│   │   │       ├── UIManager.cs
│   │   │       └── DialogueSystem.cs    # Typewriter text display & thinking UI
│   │   │
│   │   ├── Sprites/                 # 16 PPU Pixel Art assets & slice metadata (.meta)
│   │   └── Prefabs/                 # Player, NPC, and Enemy prefabs
│   │
│   ├── Packages/                    # Unity Package Manager manifest (Cinemachine, 2D)
│   └── ProjectSettings/             # Project tags, layers, and graphics settings
│
├── local-llm-server/                # Local LLM configuration & scripts
│   ├── Modelfile                    # Ollama model parameters (ctx: 1024, temp: 0.7)
│   ├── system_prompt.txt            # Base lore and persona definitions
│   └── start_server.bat             # Convenience script to start the local LLM
│
├── docs/                            # Academic thesis assets
│   ├── GDD_The_Fenrir.pdf           # Game Design Document
│   ├── UML_Diagrams/                # Use Case, Sequence, and Class diagrams
│   └── Thesis_Proposal.docx
│
├── .gitignore                       # Unity & OS ignore file
└── README.md
```

---

## 🚀 Setup & Installation

### Prerequisites
1. **Unity Engine:** Version **`2022.3 LTS`** (with 2D feature set).
2. **Ollama:** Installed from [ollama.com](https://ollama.com/).
3. **Hardware Requirement:**
   * **Minimum:** 16 GB Dual-Channel RAM (APU / Integrated Graphics compatible).
   * **Target Architecture:** Windows 10/11 x64.

---

### Step-by-Step Setup

#### 1. Clone the Repository
```bash
git clone https://github.com/YourUsername/genai-npc-project.git
cd genai-npc-project
```

#### 2. Start the Local LLM Server
Ensure Ollama is running and pull the optimized 1B model:
```bash
ollama run llama3.2:1b
```
*(Alternatively, execute `local-llm-server/start_server.bat`)*

Verify the server is active by opening `http://localhost:11434` in your browser. It should display:
```
Ollama is running
```

#### 3. Open the Project in Unity
1. Launch **Unity Hub**.
2. Click **Add > Add project from disk**.
3. Select the `unity-game-client` folder.
4. Open the project using **Unity 2022.3 LTS**. *(Unity will automatically restore all sprite slicing, tilemaps, and prefabs via committed `.meta` files).*

#### 4. Play and Test
1. In the `Project` window, navigate to `Assets/Scenes/VillageHub.unity` and double-click to open it.
2. Ensure Ollama is running in the background.
3. Press the **Play** button in Unity.
4. Walk your character up to the **Village Elder** using `WASD` and press **`E`**.
5. Observe the UI dialogue window displaying the `"Thinking..."` indicator before streaming the dynamically generated lore-consistent dialogue.

---

## 📊 Evaluation & Research Metrics

This repository serves as the testing environment for:
1. **Quantitative Performance:** Benchmarking **Tokens Per Second (TPS)**, **Shared RAM usage (MB)**, **Inference Latency (s)**, and **Client FPS Stability** on integrated graphics hardware.
2. **Qualitative Evaluation:** Measuring narrative adherence and hallucination rates comparing outputs generated **with Context Injection** versus **without Context Injection**.

---

## 📜 References
* Dubey, A., et al. (2024). *The Llama 3 herd of models*. arXiv:2407.21783.
* Frantar, E., et al. (2023). *GPTQ: Accurate Post-Training Quantization for Generative Pre-trained Transformers*. arXiv:2210.17323.
* Fajrianti, E. D., et al. (2023). *INSUS: Indoor Navigation System Using Unity and Smartphone*. Information, 14(7), 359.
* Park, J. S., et al. (2023). *Generative Agents: Interactive Simulacra of Human Behavior*. arXiv:2304.03442.
* Nielsen, J. (1994). *Usability Engineering*. Morgan Kaufmann.