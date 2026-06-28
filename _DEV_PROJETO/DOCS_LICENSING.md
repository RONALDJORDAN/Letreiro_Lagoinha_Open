# Documentação do Sistema de Licenciamento

Este documento descreve como configurar o Firebase Realtime Database para o sistema de licenciamento por HWID.

## 1. Estrutura do Banco (JSON)

Importe este JSON no seu Realtime Database para criar a estrutura inicial. Substitua "CHAVE-DO-USUARIO-XYZ" pela chave que você fornecerá ao cliente.

```json
{
  "licencas": {
    "CHAVE-TESTE-12345": {
      "hwid_vinculado": "",
      "data_ativacao": ""
    },
    "CHAVE-CLIENTE-001": {
      "hwid_vinculado": "",
      "data_ativacao": ""
    }
  }
}
```

- **hwid_vinculado**: Deixe vazio ("") para novas chaves. O sistema preencherá automaticamente no primeiro uso.
- **data_ativacao**: Será preenchida automaticamente quando o usuário ativar.

## 2. Regras de Segurança (Firebase Rules)

Configure estas regras na aba "Rules" do seu Realtime Database. Elas garantem que ninguém possa alterar o HWID de uma chave já ativada.

```json
{
  "rules": {
    "licencas": {
      // O administrador pode listar e gerenciar tudo
      ".read": "auth != null",
      ".write": "auth != null",

      "$chave": {
        // O app precisa ler a chave específica para validar (sem estar logado)
        ".read": "data.exists()",

        // Permite primeira ativação (gravar todos os campos de uma vez)
        // Trava a edição após o HWID ser vinculado
        ".write": "auth != null || !data.child('hwid_vinculado').exists() || data.child('hwid_vinculado').val() == ''"
      }
    }
  }
}
```

## 3. Painel Administrativo (Site Admin)

Foi criado um site para gerenciar as chaves em `_ADMIN_SITE`.

### Configuração:

1. No Console do Firebase, vá em **Authentication** e habilite o método de login **Email/Senha**.
2. Crie um usuário com seu email e senha (este será o login do admin).
3. Vá em **Project Settings** (ícone de engrenagem) > **General**.
4. Role até "Your apps" e selecione "Web" (</>). Registre o app "Admin Panel".
5. Copie o objeto `firebaseConfig`.
6. Abra o arquivo `_ADMIN_SITE/config.js` e cole as configurações.

### Como usar:

Basta abrir o arquivo `index.html` no seu navegador. Faça login com o email/senha criado no passo 2.

## 4. Como usar no código (Desktop App)

No arquivo `Services/LicenseService.cs`, altere a constante `FirebaseUrl` para a URL do seu banco de dados:

```csharp
private const string FirebaseUrl = "https://SEU-PROJETO.firebaseio.com/";
```

O sistema validará a licença ao iniciar o aplicativo.
