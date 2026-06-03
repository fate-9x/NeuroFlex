# Plan de Seguridad y Mejoras del Procesamiento de Datos del Paciente — NeuroFlex VR

> Documento generado a partir del análisis de los scripts actuales que recopilan métricas clínicas en el cliente Unity (Meta Quest) y las envían al backend en AWS. Sirve como guía de implementación para endurecer el flujo conforme a la **Ley 19.628 de Chile** sobre protección de datos personales (datos sensibles de salud).

---

## 1. Contexto y motivación

NeuroFlex es una app VR de rehabilitación cognitiva (Unity 2022.3.28f1 → Meta Quest). Durante la sesión, el paciente realiza la transición sit-to-stand mientras resuelve problemas de aritmética agarrando objetos numerados. Las métricas resultantes se POSTean a un API Gateway en AWS (`sa-east-1`).

### 1.1 Hallazgos de la auditoría (estado actual)

| # | Hallazgo | Ubicación | Riesgo |
|---|---|---|---|
| 1 | URL del endpoint hardcoded sin separación por entorno | `Assets/Scripts/Utils/APIManager.cs:10` | Configuración inflexible; expone infra prod |
| 2 | Sin autenticación (solo `Content-Type: application/json`) | `Assets/Scripts/Utils/APIManager.cs:24` | Cualquier cliente puede POSTear datos arbitrarios |
| 3 | Estado HTTP nunca inspeccionado; éxito decidido por prefijo `"Error:"` | `APIManager.cs:28`, `ExtractDataCollector.cs:153` | 4xx/5xx pueden marcarse como éxito |
| 4 | Sin timeout en `UnityWebRequest` | `APIManager.cs:20` | Request puede colgar indefinidamente |
| 5 | Payload completo y respuesta cruda escritos a Player log | `ExtractDataCollector.cs:142,165,173` | PII en logs persistentes del dispositivo |
| 6 | Overlay debug en pantalla con payload completo | `Inputs.cs:171-192` | Filtración visual en clínica |
| 7 | Log de la respuesta matemática correcta | `QuestionManager.cs:57` | Pérdida de integridad académica |
| 8 | Retry sin `idempotency key` (2s fijo × 3) | `ExtractDataCollector.cs:138-197` | Duplicados ante 5xx transitorios |
| 9 | Sin identificador de paciente; sólo `session_id` post-hoc | `APIManager.cs:46-59` | Sesiones huérfanas si falla la respuesta |
| 10 | `lastSessionId` y `OnSessionIdReceived` estáticos | `ExtractDataCollector.cs:11-12` | Estado global cross-scene; suscripciones fugadas |
| 11 | Sin validación de rangos (NaN/Infinity/negativos pasan) | `ExtractDataCollector.cs` (varias) | Datos corruptos en backend |
| 12 | `EndZone.OnTriggerEnter` puede disparar múltiples envíos | `Assets/Scripts/Question/EndZone.cs:49` | Sesiones duplicadas en S3 |
| 13 | Fallback que crea `APIManager` huérfano si falta | `Inputs.cs:40-42` | Envío silencioso a URL default sin auth |
| 14 | Server `s3_key` reflejado en logs cliente | `ExtractDataCollector.cs:165` | Filtración de paths internos |

### 1.2 Requisitos confirmados

- **Backend AWS bajo control total** (API Gateway, Lambda, S3, DynamoDB, Cognito, KMS).
- **Cumplimiento Ley 19.628** (datos sensibles de salud → art. 7 cifrado, art. 12 derechos ARCO, consentimiento explícito, propósito declarado).
- **Modelo de enlace paciente↔sesión vía display code temporal**: el Quest **no autentica pacientes**. Antes de iniciar la sesión, solicita un display code corto a AWS; el paciente lo entrega al especialista a través de un software externo, y el especialista hace el binding `patientId ↔ displayCode` server-side. Los datos de la sesión viajan con un `sessionToken` efímero; backend resuelve `patientId` al hacerse el bind.
- **Auth máquina-a-máquina** entre Quest y API: credenciales de dispositivo/app (Cognito client credentials), no credenciales de paciente.

### 1.3 Resultado esperado

Pipeline cliente → API con auth de dispositivo, sesiones enlazadas por display code, transmisión confiable e idempotente, logging seguro, cumplimiento Ley 19.628.

---

## 2. Modelo de sesión basado en display code

```
Quest                          AWS API                        Software especialista
  │  POST /session/start  ───►   genera displayCode
  │  (auth: device creds)        (6-8 chars, TTL ~10min)
  │                              + sessionToken (JWT
  │                              aud=session, exp 2h)
  │  ◄── {displayCode,           guarda en DynamoDB:
  │       sessionToken,          {displayCode → sessionId,
  │       sessionId, ttl}        patientId=null,
  │                              status=pending}
  │                                                            │
  │  muestra displayCode al                                    │
  │  paciente en VR                                            │
  │                                                  paciente lee y entrega
  │                                                  el código al especialista
  │                                                            │
  │                                                  POST /session/bind
  │                                                  {displayCode, patientId}
  │                                                  (auth: specialist creds)
  │                                                  ◄─── setea patientId,
  │                                                       status=bound
  │
  │  POST /data (Bearer sessionToken,
  │             X-Idempotency-Key, body con métricas)
  │   backend valida sessionToken, recupera sessionId,
  │   adjunta patientId (si bound; si no, queda pending
  │   y se enlaza al hacer bind dentro del TTL)
  │  ◄── {sessionId, ok}
```

### Reglas del display code
- 6–8 chars, alfabeto sin ambigüedad (sin `O/0/I/1`), case-insensitive.
- Rate limit en `/session/bind`: **5 intentos / 5 min por IP/especialista**.
- `sessionToken` JWT firmado con clave **KMS asimétrica**, TTL 2h, audience `session`, claim `sessionId` (sin `patientId`).
- Datos pueden enviarse aunque el binding no haya ocurrido aún; el backend deja `patientId=null` y lo completa al hacerse el bind dentro del TTL.
- Si el TTL expira sin bind → sesión queda huérfana, marcada para revisión clínica.
- Una sesión nunca cambia de paciente una vez `bound`.

---

## 3. Parte A — Cambios en el cliente Unity

### A1. Refactor `APIManager.cs` (transporte HTTP)
**Ruta:** `Assets/Scripts/Utils/APIManager.cs`

- Mover URL hardcoded (L10) a `ScriptableObject` **`ApiConfig.asset`** con campos `baseUrl`, `region`, `environment`, `deviceClientId`. Variantes dev/staging/prod.
- Header `Authorization: Bearer <sessionToken>` (obtenido de `SessionService`, ver A5).
- Header `X-Idempotency-Key: <guid>` por intento de envío (mismo guid en los 3 retries).
- Header `X-Schema-Version: 1`.
- `request.timeout = 15`.
- Inspeccionar `request.responseCode`:
  - 2xx → success.
  - 401/403 → renovar `sessionToken` y reintentar **una sola vez**.
  - 4xx (resto) → no-retry.
  - 5xx / timeout → retry exponencial (2s, 4s, 8s).
- Eliminar la clasificación por prefijo `"Error:"`. Devolver un struct `ApiResult { bool ok; int status; string body; string errorCode; }`.
- Quitar `Debug.Log` de payload en éxito; en error loggear sólo `status + errorCode` (nunca el body).

### A2. Refactor `ExtractDataCollector.cs`
**Ruta:** `Assets/Scripts/Utils/ExtractDataCollector.cs`

- Quitar logs L142, L165, L173. Reemplazar por `Debug.Log($"API status={status} sessionId={Mask(sessionId)}")`.
- Retry exponencial 2/4/8 s (L188).
- Validar rangos antes de POST: rechazar `NaN`, `Infinity`, negativos; clamp tiempos en `[0, 600s]`, precisión en `[0, 100]`.
- Reemplazar `static OnSessionIdReceived` (L12) y `static lastSessionId` (L11) por instance fields + clear en `OnDestroy`.
- Extender el struct `ExtractData` (`APIManager.cs:46-59`) con: `sessionId` (recibido de `/session/start`), `consentVersion`, `appVersion`, `deviceModel`. **No incluir `patientId`** (lo resuelve backend por binding).

### A3. Endurecer `EndZone.cs`
**Ruta:** `Assets/Scripts/Question/EndZone.cs`

- Guard `bool _sent` en `OnTriggerEnter` (L49) para evitar reenvíos en re-entradas.
- Mantener `CompareTag("Player")`.

### A4. Sanitizar logs de gameplay
- `QuestionManager.cs:57` `Debug.Log("Correct Number: ...")` → envolver en `#if UNITY_EDITOR`.
- `Inputs.cs:171-192` `ShowDebugData` → gate por `#if DEVELOPMENT_BUILD` o flag `ApiConfig.debugOverlay`.
- `Inputs.cs:40-42` eliminar fallback que crea un `APIManager` huérfano; emitir `Debug.LogError` y abortar envío (evita POST silencioso a URL default sin auth).

### A5. Nueva pieza: `SessionService`
**Ruta nueva:** `Assets/Scripts/Session/SessionService.cs`

API mínima:
- `Task<SessionInfo> StartSessionAsync()` → POST `/session/start` con `deviceToken`. Guarda `sessionId`, `sessionToken`, `displayCode`, `expiresAt`.
- `Task<string> GetSessionTokenAsync()` → retorna token vigente; si expira < 60s o tras 401, llama `RefreshAsync()`.
- `Task RefreshAsync()` → POST `/session/refresh` con device creds.
- `string CurrentDisplayCode { get; }` para UI.
- No persiste en disco (sesión efímera). Cierre de app = fin.

### A6. Pantalla `DisplayCodeScene`
**Rutas nuevas:**
- `Assets/Scenes/DisplayCode.unity` — pantalla previa a `Start`. Llama `SessionService.StartSessionAsync()` y muestra el `displayCode` grande, con instrucción "Entregue este código al especialista". Botón A continúa a `Consent` cuando el especialista confirma el binding (polling opcional a `/session/status` o avance manual tras confirmación verbal).
- `Assets/Scripts/Session/DisplayCodeController.cs` — coordina UI + `SessionService`.

### A7. Pantalla `ConsentScene` (Ley 19.628)
**Rutas nuevas:**
- `Assets/Scenes/Consent.unity` — posterior a `DisplayCode`. Muestra propósito del tratamiento de datos, derechos ARCO, versión de consentimiento, botón "Acepto".
- `Assets/Scripts/Consent/ConsentController.cs` — POST `/session/consent` `{sessionId, consentVersion, accepted:true}`. Sin aceptación, no se envían datos clínicos.

### A8. Almacenamiento seguro de credenciales de dispositivo
**Ruta nueva:** `Assets/Scripts/Auth/SecureCredentialStore.cs`
- Wrapper Unity ↔ plugin AAR (Android Keystore) para cifrar `deviceClientSecret`.
- Stub para Editor que retorna placeholder (testing).
- Provisioning inicial del dispositivo: la clínica ejecuta un flujo de registro (código de activación de un solo uso) que graba las credenciales en el Keystore. No quedan en `PlayerPrefs` ni en `ApiConfig.asset`.

### Flujo de escenas resultante
```
DisplayCode → Consent → Start → MiniMental → SelectEnvironment → City | NatureScene → FinalScene
```

---

## 4. Parte B — Guía de seguridad backend

### B1. Auth dispositivo Quest ↔ API
- **Cognito User Pool** dedicado a dispositivos (un usuario por Quest o app client tipo *client credentials*).
- Quest guarda `deviceClientId` + `deviceClientSecret` cifrados en Android Keystore (A8).
- `/session/start`: API Gateway valida client credentials con Cognito (flow M2M). Lambda emite `sessionToken` propio (JWT firmado con **KMS asymmetric key**, claims `{sessionId, deviceId, aud:"session", exp}`).
- `/data` y `/session/consent`: API Gateway con **Lambda Authorizer** que verifica firma del `sessionToken` con la clave pública KMS; rechaza si `aud != "session"` o token expirado.

### B2. Display code y binding
- DynamoDB tabla **`Sessions`**: `{sessionId (PK), displayCode (GSI), deviceId, patientId?, consentVersion?, status, createdAt, ttl}`. TTL nativo DynamoDB borra registros pendientes 24h tras vencer.
- `/session/bind` (autenticado con credenciales del especialista — Cognito User Pool separado **`Specialists`**): valida `displayCode` no expirado, `status=pending`, setea `patientId`, marca `status=bound`. Rate limit 5/5min por especialista. Audit log a CloudTrail.
- Generador de display code: `crypto.randomBytes` → base32 sin ambiguos, 6–8 chars, verificación de unicidad activa contra `Sessions`.

### B3. Integridad e idempotencia
- Header `X-Idempotency-Key` → DynamoDB **`Idempotency`** con TTL 24h, key = `(sessionId, idempotencyKey)`. Repeticiones devuelven el resultado cacheado.
- (Opcional, mayor robustez) HMAC-SHA256 del body con secret derivado del `deviceClientSecret`, en header `X-Body-Sig`. Lambda recomputa y compara.

### B4. Cifrado (Ley 19.628 art. 7 — datos sensibles)
- API Gateway con `MinimumTLSVersion: TLS_1_2`.
- S3 bucket `neuroflex-patient-data`:
  - **SSE-KMS** con key dedicada.
  - `BlockPublicAccess: true`.
  - Política deny `aws:SecureTransport=false`.
  - Prefijo `sessions/{patientId}/{sessionId}.json`.
- DynamoDB tablas con encryption-at-rest KMS.
- IAM role de la Lambda de ingesta: principio de mínimo privilegio — sólo `s3:PutObject` al prefijo correspondiente, sin `s3:GetObject*`.

### B5. Logging y monitoreo
- **Cliente:** nunca payload completo. Sólo `status`, `idempotencyKey`, `displayCode` enmascarado (`AB**EF`), `sessionId` enmascarado.
- **Backend:** CloudWatch retención 90d; hash truncado de `patientId`/`sessionId` en logs (PII bajo Ley 19.628).
- **CloudTrail** habilitado para auditoría de S3/DynamoDB/Cognito.
- Alarmas CloudWatch:
  - `>10 respuestas 401 en 5 min` por `deviceId` → posible compromiso de credenciales del dispositivo.
  - `>5 binds fallidos en 5 min` por especialista → posible fuerza bruta del display code.
  - Sesiones `pending` que expiran sin bind > umbral diario → revisar flujo clínico.

### B6. Derechos ARCO (Ley 19.628 art. 12)
Endpoints autenticados con credenciales del paciente o del especialista (no se llaman desde el Quest):
- `GET /patient/{id}/data` — **A**cceso.
- `PATCH /patient/{id}/data` — **R**ectificación.
- `DELETE /patient/{id}` — **C**ancelación: soft-delete 30d → hard-delete en S3 + DynamoDB + Cognito disable.
- `POST /patient/{id}/opt-out` — **O**posición: bloquea futuros `/session/bind` con ese `patientId`.

### B7. Seguridad operativa
- Secrets en **AWS Secrets Manager**, jamás en repo.
- `ApiConfig.asset` referencia sólo `deviceClientId` por entorno; el `deviceClientSecret` se entrega al Quest en provisioning y vive en Keystore.
- `.gitignore`: variantes `ApiConfig.*.asset` con URLs internas según política.
- Rotación: KMS keys anual; `sessionToken` 2h; `deviceClientSecret` rotable vía Cognito.
- Build Android: habilitar **IL2CPP** + **Managed Stripping High** para dificultar reverse engineering de IDs/URLs.

---

## 5. Archivos clave (modificar o crear)

| Ruta | Acción |
|---|---|
| `Assets/Scripts/Utils/APIManager.cs` | Modificar — refactor transporte (A1) |
| `Assets/Scripts/Utils/ExtractDataCollector.cs` | Modificar — sanitizar logs, retry exponencial, validación, quitar statics (A2) |
| `Assets/Scripts/Question/EndZone.cs` | Modificar — guard de envío único (A3) |
| `Assets/Scripts/Question/QuestionManager.cs` | Modificar — gate log respuesta correcta (A4) |
| `Assets/Scripts/Player/Inputs.cs` | Modificar — eliminar fallback APIManager + gate overlay (A4) |
| `Assets/Scripts/Session/SessionService.cs` | **Nuevo** — `/session/start`, `/session/refresh`, manejo `sessionToken` (A5) |
| `Assets/Scripts/Session/DisplayCodeController.cs` | **Nuevo** — UI display code (A6) |
| `Assets/Scripts/Consent/ConsentController.cs` | **Nuevo** — consentimiento Ley 19.628 (A7) |
| `Assets/Scripts/Auth/SecureCredentialStore.cs` | **Nuevo** — wrapper Android Keystore (A8) |
| `Assets/ScriptableObjects/ApiConfig.asset` | **Nuevo** — config por entorno (A1) |
| `Assets/Scenes/DisplayCode.unity` | **Nueva** — pantalla display code |
| `Assets/Scenes/Consent.unity` | **Nueva** — pantalla consentimiento |
| `Assets/Plugins/Android/SecureCredentialStore.aar` | **Nuevo** — plugin nativo Keystore |

Reutilizar:
- `Assets/Scripts/Utils/SceneController.cs` (`LoadScene`) para las transiciones nuevas.
- Patrón **"Utils" GameObject** (descrito en `CLAUDE.md`) para registrar `SessionService` como manager persistente.
- `Assets/Scripts/Subtitles/SubtitleManager.cs` para instrucciones en pantalla display code.

---

## 6. Plan por fases

1. **Fase 0 — Backend (paralelo):** Cognito User Pools (devices + Specialists), API Gateway + Lambda Authorizer KMS, DynamoDB `Sessions` + `Idempotency`, S3 SSE-KMS, Lambdas `/session/start`, `/session/bind`, `/session/refresh`, `/session/consent`, `/data`. Probar con Postman.
2. **Fase 1 — Transporte Unity:** `ApiConfig.asset` + refactor `APIManager.cs` (timeout, status real, idempotency). Validar que un POST sin token devuelve 401.
3. **Fase 2 — Sesión:** `SessionService` + `SecureCredentialStore` + escena `DisplayCode`. Smoke test: arranque → display code mostrado → bind manual vía Postman → POST `/data` autorizado.
4. **Fase 3 — Consentimiento:** `ConsentController` + escena `Consent` + endpoint `/session/consent`. Bloquear `/data` sin consentimiento vigente.
5. **Fase 4 — Higiene:** Sanitización de logs (A4), guard en EndZone (A3), validación de rangos, eliminación de statics.
6. **Fase 5 — ARCO:** Endpoints B6 + UI especialista para acceso/borrado/oposición.
7. **Fase 6 — Hardening:** IL2CPP + Managed Stripping High + pentest interno (reuse de display code, replay de `sessionToken`, fuerza bruta del bind).

---

## 7. Verificación end-to-end

### Unity Play Mode (build dev contra entorno stage)
- Arranque → POST `/session/start` exitoso → display code mostrado en VR.
- Sin bind externo → envíos `/data` quedan en `pending`; tras bind → `patientId` resuelto en DynamoDB.
- Sin token → 401 visible en CloudWatch.
- Forzar 500 en backend → 3 retries con la **misma** idempotency key; un solo registro persistido.
- Re-entrar al trigger de `EndZone` → un solo POST.
- `Debug.Log` sin payload, sin display code completo, sin `sessionId` completo.

### Backend
- `sessionToken` expirado → 401.
- Bind con display code expirado → 410.
- Bind con display code inexistente → 404; tras 5 intentos rate-limit dispara.
- Sin consentimiento previo → `/data` retorna 403.

### Cumplimiento Ley 19.628
- `DELETE /patient/{id}` → confirmar borrado en S3 (`aws s3 ls`) + DynamoDB + Cognito disabled. CloudTrail muestra el evento.
- `POST /patient/{id}/opt-out` → siguientes binds rechazados.
- Versión de consentimiento aceptada queda registrada por sesión.

### Dispositivo Quest (build Android)
- Instalar APK; capturar tráfico con mitmproxy + cert custom → confirmar TLS 1.2 y rechazo de cert no confiable.
- Validar que las credenciales del dispositivo no aparecen en `/sdcard/Android/data/.../*` ni en logs `adb logcat`.

### Monitoreo
- Forzar 11 respuestas 401 → alarma CloudWatch.
- Forzar 6 binds inválidos → alarma CloudWatch.

---

## 8. Consideraciones importantes

- **No autenticar pacientes en el Quest.** El dispositivo es compartido en la clínica; el binding humano-en-medio (paciente lee código al especialista) está intencionalmente diseñado para evitar credenciales en el headset.
- **`patientId` nunca viaja desde el Quest.** Se resuelve server-side por el binding; esto reduce la superficie PII en el dispositivo.
- **El `sessionToken` no contiene `patientId`** (privacy by design, art. 11 Ley 19.628 — finalidad y proporcionalidad).
- **Consentimiento previo a cualquier `/data`** es obligatorio bajo Ley 19.628 para datos sensibles (art. 10).
- **Logs y overlays de debug deben gateated.** Cualquier `Debug.Log` con payload completo, display code o `sessionId` completo es un hallazgo bloqueante para producción.
- **Retención y borrado.** Definir política formal por sesión (recomendado: 5 años para historia clínica electrónica chilena), documentada en el consentimiento.
- **Provisioning del Quest** debe ser un procedimiento documentado (un técnico clínico ejecuta el flujo de activación, no el paciente).
- **Si el especialista no enlaza dentro del TTL,** la sesión queda huérfana → debe existir un panel administrativo de revisión y un proceso de borrado o re-asignación documentado.
- **Pentest** antes de producción: enfocarse en (a) reuse del display code, (b) replay del `sessionToken`, (c) inyección de métricas falsas con un token robado, (d) abuso del endpoint `/session/bind`.

---

## 9. Referencias rápidas

- Endpoint actual (a reemplazar): `https://f13h4cz6id.execute-api.sa-east-1.amazonaws.com/data` (`APIManager.cs:10`).
- Punto único de envío hoy: `ExtractDataCollector.SendDataToAPIWithRetry` (`ExtractDataCollector.cs:138`).
- Disparador único hoy: `EndZone.OnTriggerEnter` (`EndZone.cs:49`).
- Identificador de sesión actual: `session_id` devuelto por el backend, expuesto en `ExtractDataCollector.lastSessionId` (`ExtractDataCollector.cs:11`) y consumido por `EndZone` para UI.
- Tags usados en el flujo: `Player`, `Spawn_1/2/3`, `LeftHand`, `RightHand`, `MainCamera` (ver `CLAUDE.md`).
