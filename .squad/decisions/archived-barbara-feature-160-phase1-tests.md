# Barbara — Feature 160 phase 1 test note

- **Decision:** Keep `AiSettingsDto.OllamaEndpoint` as a compatibility alias while introducing `EndpointUrl` and `BackendType` for phase 1.
- **Why:** Existing client/test code already consumes `OllamaEndpoint`, and the first slice is about proving the new backend surface without forcing a broader client migration in the same change.
- **Test impact:** Phase-1 tests assert the new `EndpointUrl`/`BackendType` behavior on application and API surfaces, while the alias prevents unrelated breakage outside this slice.
