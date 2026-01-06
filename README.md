# AR Aggression Trainer

**AR Aggression Trainer** is an augmented reality training application built in **Unity** for **Microsoft HoloLens 2**. Its purpose is to help learners (such as service staff or students) experience and practice de‑escalation techniques in simulated aggressive interactions, enhanced with AI‑driven feedback based on tone of voice, facial expressions, and body language. 
Futures Lab
#
🎯 Table of Contents
- 🧠 About
- 🚀 Features
- 🛠 Architecture
- 📦 Getting Started
- 📌 Requirements
- ▶️ Running the Project
- 🔄 Streaming & AI Integration
- 🧪 Testing
- 📄 License
- ❤️ Contributing
#
🧠 About

AR Aggression Trainer delivers immersive, real‑time training scenarios on the **HoloLens 2 headset** by:

- Streaming live video to an AI service
- Receiving emotional, vocal, and gesture analyses
- Providing responsive feedback to the user
- Creating a safe space for training challenging interpersonal skills

This project is part of an educational toolkit developed by **Team Education Futures Lab**, in collaboration with stakeholders looking to effectively teach conflict management and customer interaction skills. 
Futures Lab
#
🚀 Features

✅ **Unity‑based AR application optimized** for HoloLens 2

✅ **Live video streaming** from the headset

✅ **AI‑driven behavior analysis** (voice tone, gesture, facial expressions)

✅ **Automated feedback loop** to guide users during scenarios

✅ **Modular structure** for defining new scenario experiences
#
🛠 Architecture

This repository primarily includes:

📁 Unity project assets

📁 Scene definitions for aggression training scenarios

📁 Scripts to manage camera, video streaming, and AI communication

📁 MRTK/interaction components for HoloLens

📁 Shader and visuals for AR rendering


The core logic bridges streaming input from the device with a backend AI system that returns context‑aware guidance.
#
📦 Requirements

To build and run this project, you’ll need:

|**Requirement**                        |**Version**
|---------------------------------------|---------------------------------------
|Unity                                  |	2020.3.49f1
|Microsoft Mixed Reality Toolkit (MRTK) |	v3
|HoloLens 2 SDK / XR Plugin             |	Latest
|Visual Studio                          |	2019+
|.NET / C#                              |	Compatible with Unity
|AI Feedback Backend                    |	**REST/WebSocket capable**

#
**▶️ Getting Started**

**1. Clone the repository**

```bash
git clone https://github.com/Team-Education-Futures-Lab/AR_Agression_Trainer.git
cd AR_Agression_Trainer
```

**2. Create a new folder and follow the instructions in the READ.md from:**
https://github.com/Team-Education-Futures-Lab/Facial_Recognition 

**3. Open in Unity**
- Launch Unity Hub
- Add the project folder
- Open the project

**4. Import Dependencies**

Ensure MRTK and HoloLens packages are installed via Unity Package Manager.

**5. Configure Scenes**
- Set `Scenes/Main.unity` (or equivalent) as the startup scene.
- Confirm XR configuration is set for **HoloLens 2**.
#
**🔄 Streaming & AI Integration**

This project streams the camera feed from the HoloLens 2 to an external AI service that analyzes user interactions and scenario responses. The interaction flow is roughly:

1. HoloLens captures **video** + **audio** + **user movement**
2. Data is sent to an **AI service** (via REST/WebSocket)
3. AI processes the inputs and returns feedback
4. The Unity app applies **real‑time feedback and visuals**

⚠️ This repository assumes you have a compatible AI backend. You’ll need to provide the endpoint and API keys in the config scripts.
#
**🧪 Testing**

Use the **Unity Editor Play Mode** for rapid iteration/testing of non‑HoloLens logic. For spatial/AR tests, deploy directly to your **HoloLens 2** device.
#
**📄 License**

This project has no license defined. If you own rights to this codebase, add a license file (e.g., MIT, Apache‑2.0) to clarify usage.
#
**❤️ Contributing**

Contributions are welcome! Please follow these guidelines:

Submit issues for bugs/features

Use feature branches for pull requests

Include descriptive commit messages

Reference backing tickets/issues
#
**📌 Notes**

This repository currently has no **README** in its main branch.

The context and purpose were inferred from public descriptions of the project vision.
