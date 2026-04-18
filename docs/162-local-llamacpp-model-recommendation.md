# Feature 162: Local llama.cpp Model Recommendation for 32 GB RAM / 16 GB VRAM

> **Status:** Complete

## Overview

Budget Experiment already supports llama.cpp as a local AI backend, but the repository does not yet name a practical default model for a strong consumer desktop: **32 GB system RAM, 16 GB VRAM, RTX 5070-class NVIDIA GPU, Ryzen CPU**.

This research recommends a default Hugging Face model family for **general-purpose chat and reasoning** on that hardware. The aim is not to chase benchmark headlines; it is to choose the best model that is both **good enough to matter** and **small enough to stay pleasant locally**.

My conclusion: the sweet spot on this machine is a **strong dense 12B-14B model in GGUF Q5_K_M or Q6_K**. That class gives materially better reasoning than 8B models without the latency and memory penalties of 24B-class models.

---

## Problem Statement

### Current State

- Feature 160 made **llama.cpp** a supported backend in the application.
- `docs/AI.md` explains local AI usage, but it does **not** recommend a default llama.cpp model for realistic local hardware.
- Without a recommendation, future work risks oscillating between:
  - models that are too small and fast but underpowered for reasoning, or
  - models that are impressive on paper but sluggish on 16 GB VRAM.

### Target State

- One primary model recommendation for this hardware profile.
- At least two credible runner-up options with clear tradeoffs.
- Practical guidance for quantization, context sizing, and expected local behavior in llama.cpp.
- A recommendation future implementation work in this repo can adopt without redoing the research.

---

## Research Findings

### Selection heuristic

For this hardware profile, the practical questions are:

1. **Can the quant fit comfortably in 16 GB VRAM?**
2. **Does it leave enough headroom for KV cache and normal context lengths?**
3. **Is the model actually strong for chat + reasoning, not just one or the other?**
4. **Does it have clean GGUF / llama.cpp support today?**

On those criteria, **14B dense models** are the best general-purpose fit.

### Candidate comparison

| Model | Overall take | Practical quants | VRAM / RAM fit | Context practicality | Speed / latency expectation | llama.cpp compatibility |
|---|---|---|---|---|---|---|
| **Qwen/Qwen3-14B-GGUF** | **Best overall balance; recommended** | Q4_K_M **9.00 GB**, Q5_K_M **10.51 GB**, Q6_K **12.12 GB**, Q8_0 **15.70 GB** | Excellent at Q5_K_M; good at Q6_K; Q8_0 is too tight for comfortable headroom on 16 GB VRAM | Treat **8K-16K** as the sensible default; **32K** is viable; **131K YaRN** is possible but not a practical default | Interactive on this class of GPU; slower in thinking mode because the model emits extra reasoning tokens | **High confidence**: official GGUF repo, official llama.cpp guidance, official chat template |
| **Qwen/Qwen2.5-14B-Instruct-GGUF** | Safest stable fallback; still very strong | Q4_K_M **8.99 GB**, Q5_K_M **10.51 GB**, Q6_K **12.12 GB**, Q8_0 **15.70 GB** | Excellent at Q5_K_M; similar fit to Qwen3 14B | Same practical advice: 8K-16K default; use long context deliberately | Similar to Qwen3 14B, often a little simpler operationally because there is no think/non-think switching | **High confidence**: mature GGUF ecosystem and straightforward instruct behavior |
| **bartowski/Meta-Llama-3.1-8B-Instruct-GGUF** | Fastest mainstream option; weaker reasoning than the 14B class | Q4_K_M **4.92 GB**, Q5_K_M **5.73 GB**, Q6_K **6.60 GB**, Q8_0 **8.54 GB** | Extremely comfortable fit; leaves lots of cache headroom | Easy to run at larger contexts than 14B models on the same card | Fastest of the serious options; best when responsiveness matters more than answer quality | **High confidence**: mature llama.cpp support and abundant GGUF choices |
| **MaziyarPanahi/mistral-small-3.1-24b-instruct-2503-hf-GGUF** | Highest upside of the shortlist, but not the best daily-driver fit | Q4_K_M **14.33 GB**, Q5_K_M **16.76 GB**, Q6_K **19.35 GB** | Technically runnable, but Q4_K_M already consumes most of 16 GB VRAM; Q5_K_M spills into RAM / hybrid territory | Use moderate context unless you accept a latency hit | Clearly slower than the 14B class on this machine; viable for deliberate evaluation, not my default pick | **Good**: GGUF variants exist and run in llama.cpp, but this is a less comfortable fit |

### Honorable mention: Gemma 3 12B

`ggml-org/gemma-3-12b-it-GGUF` is real and practical:

- Q4_K_M **7.30 GB**
- Q8_0 **12.51 GB**
- additional multimodal projection file: **0.85 GB**
- GGUF context length metadata: **131072**

It is attractive if future work needs **text + image** input, but for this repo's stated use case of **general-purpose chat and reasoning**, I would still pick Qwen3 14B first. Gemma 3 also introduces a little more licensing/access friction than Apache-2.0 Qwen.

---

## Recommendation

### Primary recommendation

**Use `Qwen/Qwen3-14B-GGUF:Q5_K_M` as the default llama.cpp model for this hardware profile.**

### Why this is the best fit

- **Best balance of quality and practicality.** Qwen3 14B lands in the right size class for 16 GB VRAM while still being strong enough for real reasoning work.
- **Official GGUF support exists.** This is not a speculative conversion; Qwen publishes an official GGUF repo and official llama.cpp instructions.
- **One model covers two modes well.** Qwen3's `/think` and `/no_think` behavior is especially useful for a local assistant:
  - use **`/no_think`** for ordinary chat and UI responsiveness,
  - use **`/think`** only when the task actually needs deeper reasoning.
- **Apache-2.0 licensing is straightforward.**
- **Q5_K_M fits well.** At about **10.51 GB**, it leaves materially more headroom than Q8_0 for KV cache and normal context lengths.

### Recommended quant by priority

1. **Default:** `Q5_K_M`
   - Best overall quality/fit point for 16 GB VRAM.
2. **If speed matters more:** `Q4_K_M`
   - Slightly weaker, but still sensible and more forgiving on context/cache.
3. **If quality matters more and you can tolerate tighter headroom:** `Q6_K`
   - Viable, but less comfortable once context grows.
4. **Do not use as the default on this machine:** `Q8_0`
   - It fits on paper, but leaves too little practical room for cache and smooth operation.

### Practical context guidance

- **Start with 8K context** for everyday use.
- **16K** is still reasonable on this machine.
- **32K** is possible, but not free.
- **64K+ / 131K YaRN** should be treated as an explicit experiment, not the default:
  - higher memory pressure,
  - more latency,
  - more opportunity for quality degradation on shorter tasks when using static YaRN.

### Practical latency guidance

Exact tokens-per-second will vary with:

- llama.cpp build,
- CUDA backend/version,
- full vs partial GPU offload,
- batch size,
- context length,
- whether Qwen3 is emitting hidden reasoning text in thinking mode.

Still, the practical expectation is clear:

- **Qwen3 14B Q5_K_M:** comfortably interactive for single-user chat on this hardware.
- **Qwen2.5 14B Q5_K_M:** similar class, slightly more conservative operationally.
- **Llama 3.1 8B:** noticeably faster, but materially weaker on harder reasoning.
- **Mistral Small 24B Q4_K_M:** slower enough that it stops feeling like the best default for everyday local chat.

---

## Runner-Up Recommendations

### Runner-up 1: `Qwen/Qwen2.5-14B-Instruct-GGUF:Q5_K_M`

Choose this when:

- you want a **more conservative, mature instruct model**,
- you do **not** care about Qwen3's think/non-think switching,
- you want nearly the same memory profile as the primary recommendation.

Why it narrowly loses:

- it is still excellent, but **Qwen3 has the stronger forward-looking chat + reasoning story** for a local assistant.

### Runner-up 2: `bartowski/Meta-Llama-3.1-8B-Instruct-GGUF:Q6_K` or `:Q5_K_M`

Choose this when:

- you care more about **speed and responsiveness** than maximum reasoning quality,
- you want extra VRAM headroom for larger context or multiple experiments,
- you value very mature llama.cpp support.

Why it narrowly loses:

- the **8B class is simply below the 14B class** for general-purpose reasoning on the same hardware budget.

### Runner-up 3: `MaziyarPanahi/mistral-small-3.1-24b-instruct-2503-hf-GGUF:Q4_K_M`

Choose this when:

- you want to probe the **highest-quality model that is still barely practical** on this machine,
- you are willing to trade responsiveness for answer quality.

Why it narrowly loses:

- it is **too close to the edge** for 16 GB VRAM to be my default recommendation.
- This is the kind of model I would benchmark on purpose, not quietly standardize across the repo.

---

## Implementation Notes

- If this repository wants a single documented llama.cpp default, use:
  - **Model:** `Qwen/Qwen3-14B-GGUF:Q5_K_M`
  - **Context:** `8192` to start
- If a fallback default is needed for comparison or operational simplicity, use:
  - **Model:** `Qwen/Qwen2.5-14B-Instruct-GGUF:Q5_K_M`
- For future docs or scripts, avoid advertising **32K+ context as the default** just because the model supports it.
- For Qwen3 specifically:
  - prefer **`/no_think`** for routine assistant flows,
  - reserve **`/think`** for harder reasoning prompts,
  - remember that think mode can make latency feel worse because the model spends tokens on hidden reasoning before the final answer.

### Example llama.cpp starting point

```powershell
llama-server -hf Qwen/Qwen3-14B-GGUF:Q5_K_M --jinja -ngl 99 -c 8192
```

If future implementation work needs a CLI example closer to Qwen's own guidance, keep their recommended sampling defaults in mind for thinking vs non-thinking mode rather than forcing a single universal preset.

---

## Acceptance Criteria

- [x] One primary recommendation is named.
- [x] At least two runner-up alternatives are documented.
- [x] Tradeoffs include quality, VRAM/RAM fit, quant expectations, context practicality, speed/latency expectations, and llama.cpp compatibility.
- [x] The recommendation is honest about uncertainty and the fast-moving model landscape.
- [x] The document is actionable for future llama.cpp-related work in this repository.

---

## Suggested Validation Steps

1. Download **`Qwen/Qwen3-14B-GGUF:Q5_K_M`** and run it in llama.cpp with **8K context** first.
2. Validate:
   - startup stability,
   - VRAM usage,
   - RAM usage,
   - first-token latency,
   - steady-state generation feel for normal chat.
3. Run a small prompt pack that covers:
   - ordinary assistant chat,
   - summarization,
   - multi-step reasoning,
   - one code-oriented reasoning prompt.
4. Repeat the same prompts against:
   - `Qwen/Qwen2.5-14B-Instruct-GGUF:Q5_K_M`
   - `bartowski/Meta-Llama-3.1-8B-Instruct-GGUF:Q6_K`
5. Only benchmark `Mistral-Small-3.1-24B-Instruct-2503` if the team is willing to accept slower local response times in exchange for a possible quality uptick.
6. If Qwen3 think-mode verbosity becomes an issue in product UX, keep Qwen3 but default prompting to **`/no_think`** before demoting the model family.

---

## Uncertainty Notes

- Open-weight model rankings move quickly.
- Distilled variants and new 12B-14B releases appear frequently and can temporarily look better on selected benchmarks.
- I would **not** change the repo default on leaderboard movement alone. The bar for replacement should be:
  - similar or better local fit,
  - equally clean llama.cpp support,
  - materially better real local behavior on this hardware.

At the time of writing, **Qwen3 14B Q5_K_M** is the strongest practical recommendation I would put in front of this team without apology.

---

## References

- [Qwen3-14B-GGUF model card](https://huggingface.co/Qwen/Qwen3-14B-GGUF)
- [Qwen3-14B-GGUF file listing](https://huggingface.co/api/models/Qwen/Qwen3-14B-GGUF/tree/main)
- [Qwen3-14B base model card](https://huggingface.co/Qwen/Qwen3-14B)
- [Qwen2.5-14B-Instruct model card](https://huggingface.co/Qwen/Qwen2.5-14B-Instruct)
- [Qwen2.5-14B-Instruct-GGUF quant card](https://huggingface.co/bartowski/Qwen2.5-14B-Instruct-GGUF)
- [Meta-Llama-3.1-8B-Instruct-GGUF quant card](https://huggingface.co/bartowski/Meta-Llama-3.1-8B-Instruct-GGUF)
- [Meta-Llama-3.1-8B-Instruct-GGUF file listing](https://huggingface.co/api/models/bartowski/Meta-Llama-3.1-8B-Instruct-GGUF/tree/main)
- [Gemma 3 12B IT GGUF file listing](https://huggingface.co/api/models/ggml-org/gemma-3-12b-it-GGUF/tree/main)
- [Mistral Small 3.1 24B Instruct model card](https://huggingface.co/mistralai/Mistral-Small-3.1-24B-Instruct-2503)
- [Mistral Small 3.1 24B GGUF file listing](https://huggingface.co/api/models/MaziyarPanahi/mistral-small-3.1-24b-instruct-2503-hf-GGUF/tree/main)
- [llama.cpp README](https://github.com/ggml-org/llama.cpp)

---

## Changelog

| Date | Change | Author |
|------|--------|--------|
| 2026-04-13 | Initial research recommendation and shortlist | Alfred |
