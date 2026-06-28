# 🔄 CENTRAL DE ATUALIZAÇÕES — GUIA DE CONFIGURAÇÃO

## 📐 ARQUITETURA DO SISTEMA

```
┌─────────────────────────────────────────────────────────┐
│                    SEU COMPUTADOR                        │
│                                                          │
│  ┌──────────────┐    ┌──────────────┐    ┌────────────┐ │
│  │  App.xaml.cs  │───►│ UpdateService│───►│ UpdateCenter│ │
│  │  (Startup)   │    │ (Engine)     │    │ (UI/Window) │ │
│  └──────────────┘    └──────┬───────┘    └────────────┘ │
│                             │                            │
│                  ┌──────────▼──────────┐                 │
│                  │  update_launcher.bat │                 │
│                  │  (Mata processos e  │                 │
│                  │   roda o setup.exe) │                 │
│                  └─────────────────────┘                 │
└─────────────────────────────────────────────────────────┘
                          │
                 GET /version.json
                          │
                          ▼
┌─────────────────────────────────────────────────────────┐
│              FIREBASE HOSTING / STORAGE                  │
│                                                          │
│  📄 /updates/version.json    (Controle de versão)       │
│  📦 /releases/Setup_v2.2.exe (Instalador novo)          │
└─────────────────────────────────────────────────────────┘
```

---

## 🔧 COMO CONFIGURAR

### 1. Configurar a URL no Firebase

No arquivo `Services/UpdateService.cs`, configure a URL:

```csharp
private const string VersionJsonUrl =
    "https://letreirodigital-88f8e.web.app/updates/version.json";
```

### 2. Criar o `version.json` no Firebase Hosting

Coloque o arquivo `version.json` na pasta `updates/` do seu Firebase Hosting:

```json
{
  "version": "2.2.0",
  "build_number": 220,
  "release_date": "2026-03-01",
  "download_url": "https://firebasestorage.googleapis.com/v0/b/letreirodigital-88f8e.appspot.com/o/releases%2FLetreiroDigital_Setup_v2.2.0.exe?alt=media",
  "file_size_mb": 45.2,
  "sha256": "",
  "required": false,
  "min_version": "2.0.0",
  "changelog": [
    "Novo sistema de temas com preview ao vivo",
    "Correções de performance no modo TV",
    "Suporte a controle remoto via celular"
  ],
  "changelog_url": "",
  "severity": "recommended"
}
```

**Campos:**
| Campo | Descrição |
|-------|-----------|
| `version` | Versão nova (semântica: X.Y.Z) |
| `build_number` | Número do build (incremental) |
| `download_url` | URL direta para o `.exe` do setup |
| `file_size_mb` | Tamanho em MB (exibido na UI) |
| `sha256` | Hash para validação de integridade (opcional) |
| `required` | Se `true`, FORÇA a atualização |
| `min_version` | Versão mínima suportada |
| `severity` | `"critical"`, `"recommended"` ou `"optional"` |

### 3. Upload do Instalador

Faça upload do seu `.exe` (Inno Setup) para o **Firebase Storage**:

```bash
# Via Firebase CLI
firebase deploy --only hosting

# Ou upload manual no Console Firebase:
# Storage > releases/ > Upload do arquivo
```

### 4. Atualizar a Versão no Código

A cada novo release, atualize em `Services/UpdateService.cs`:

```csharp
public static readonly string CurrentVersion = "2.2.0";
public static readonly int CurrentBuildNumber = 220;
```

---

## 🚀 COMO O FLUXO FUNCIONA

### Verificação Automática (Startup)

1. O app inicia normalmente
2. Após 5 segundos, faz um GET silencioso ao `version.json`
3. Se houver versão maior, mostra uma notificação discreta
4. O usuário escolhe se quer abrir a Central de Atualizações

### Verificação Manual (Menu)

1. O usuário clica em **❓ Ajuda > 🔄 Central de Atualizações**
2. Clica em **VERIFICAR ATUALIZAÇÕES**
3. Se houver atualização, vê o changelog e badges de severity
4. Clica em **BAIXAR ATUALIZAÇÃO** → barra de progresso em tempo real
5. Clica em **INSTALAR E REINICIAR**

### Processo de Instalação (O "Pulo do Gato")

1. O app cria um `update_launcher.bat` temporário
2. Lança o batch como processo independente
3. O app **se encerra** (libera todos os arquivos)
4. O batch **aguarda** o processo morrer (loop)
5. O batch **executa** o setup.exe em modo silencioso
6. O setup substitui todos os arquivos
7. O setup **reinicia** o app automaticamente

### Flags do Inno Setup usadas:

```
/VERYSILENT          → Sem janelas visíveis
/SUPPRESSMSGBOXES    → Sem mensagens de confirmação
/CLOSEAPPLICATIONS   → Fecha apps que usam os arquivos
/RESTARTAPPLICATIONS → Reinicia o app ao terminar
```

---

## 🔒 SEGURANÇA

- **SHA256 Validation:** Se você preencher o campo `sha256` no `version.json`, o app valida automaticamente o hash do arquivo baixado antes de instalar.
- **HTTPS:** Todas as comunicações usam HTTPS.
- **Pasta segura:** Downloads ficam em `%LOCALAPPDATA%\LetreiroDigital\Updates\`.

---

## 📂 ARQUIVOS CRIADOS

| Arquivo                            | Descrição                                        |
| ---------------------------------- | ------------------------------------------------ |
| `Models/UpdateInfo.cs`             | Modelos de dados (UpdateInfo, UpdateState)       |
| `Services/UpdateService.cs`        | Engine de atualização (check, download, install) |
| `Views/UpdateCenterWindow.xaml`    | Interface premium da Central                     |
| `Views/UpdateCenterWindow.xaml.cs` | Lógica da interface                              |
| `Assets/updates/version.json`      | Template do JSON de versionamento                |

---

## ✅ CHECKLIST PARA PUBLICAR UMA ATUALIZAÇÃO

1. [ ] Compilar a nova versão do app
2. [ ] Criar o instalador (Inno Setup)
3. [ ] Fazer upload do `.exe` para Firebase Storage
4. [ ] Atualizar `version.json` com a nova versão, URL e changelog
5. [ ] Fazer deploy do `version.json` no Firebase Hosting
6. [ ] Testar a verificação no app antigo
