# Barbara — Feature 160 llama.cpp + DI Test Coverage

**Timestamp:** 2026-04-13T01:54:40Z  
**Role:** Tester  
**Request:** Validate the llama.cpp + DI slice.

## Outcome

✅ **Token counting regression tests added** — `LlamaCppAiServiceTests.cs`:
- Fake HTTP response handling for llama.cpp endpoint shape
- Token counting behavior locked for new backend
- Streaming response parsing validation
- No live llama.cpp server required

✅ **Backend selection and DI tests added**:
- `BackendSelectingAiService` returns correct concrete instance based on settings
- Default Ollama behavior preserved when settings unavailable
- Explicit backend selection (via `ISettings.AiBackend`) respected
- DI registration verified for both `OllamaAiService` and `LlamaCppAiService`

✅ **Infrastructure DI registration coverage** — Targeted unit tests validate:
- Both backends registered as typed HttpClient services
- Selector correctly wired to `IAiService` interface
- No concrete backend leakage into Application or API layers

✅ **AI controller backend/endpoint mapping** — Non-Docker controller tests:
- `/ai/status` correctly reports backend choice
- `/ai/suggest` routing works with both backends
- Backend switch observable through controller responses

✅ **Feature doc items updated** — `docs/160-pluggable-ai-backend.md`:
- [ ] marked complete for llama.cpp tests and DI validation

⚠️ **API integration tests remain Docker-blocked** — Full end-to-end persistence/API validation deferred pending Docker-local dev setup documentation.

## Notes

- Non-Docker regression coverage ensures llama.cpp and DI selector behaviors locked.
- Ollama default behavior preserved and tested; backward compatibility verified.
- Ready for Docker-enabled environment validation when available.
