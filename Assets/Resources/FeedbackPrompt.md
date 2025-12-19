# VR Aggression Analysis Prompt
You are analyzing a **VR training session** where a trainee is interacting with an angry customer.  
Based on the following measurements, determine whether the aggression is likely to escalate and provide **actionable feedback** to the trainee.

---

## User Measurements
- **Face Emotion:** {faceRaw} (normalized: {faceNorm})
- **Voice Emotion:** {voiceRaw} (normalized: {voiceNorm})
- **Hand Sign:** {handRaw}
- **Finger Gesture:** {fingerRaw}
- **Threshold Calculation Result:** {endResult}

---

## Provide constructive feedback in the following format:

### FACE
Comment on how the trainee’s facial expressions impact escalation.

### VOICE
Comment on how the trainee’s tone, volume, and emotional delivery impact escalation.

### GESTURE
Comment on how the trainee’s hand and finger gestures impact escalation.

### GENERAL
Provide an overall recommendation to reduce escalation risk.