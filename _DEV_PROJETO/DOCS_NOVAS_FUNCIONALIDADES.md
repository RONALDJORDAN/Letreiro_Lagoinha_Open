# Documentação das Novas Funcionalidades - v2.1

## 1. Barra Superior de Configurações

Uma nova barra de menu foi adicionada ao topo da janela de controle para acesso rápido a todas as funcionalidades.

### Menu Arquivo

- **Importar/Exportar Cronograma**: Permite salvar e carregar a programação (em desenvolvimento).
- **Configurações Gerais**: Acesso às configurações globais do aplicativo.

### Menu Temas

- **Editor de Temas WYSIWYG**: Abra o novo editor visual para criar e personalizar temas em tempo real.
- **Aplicar Tema...**: Selecione e aplique instantaneamente um dos temas salvos.
- **Salvar Tema Atual...**: Salve as configurações atuais da tela como um novo tema.
- **Gerenciar Temas...**: Exclua ou gerencie seus temas salvos.

### Menu Apresentação

- **Configurar Transições**: Defina a duração do _fade in_ (entrada) e _fade out_ (saída) dos slides.
- **Modo Tela Cheia (F11)**: Alterna rapidamente entre modo janela e tela cheia.
- **Preview em Nova Janela**: Abre uma janela separada para pré-visualização (útil para múltiplos monitores).

### Menu Atalhos

- **Configurar Atalhos**: Visualize e (futuramente) edite as teclas de atalho usadas pelo operador.
- **Restaurar Padrões**: Reseta todos os atalhos para a configuração original.
- **Ver Lista de Atalhos (F1)**: Exibe a lista completa de comandos disponíveis.

### Menu Controle Remoto

- **Iniciar Servidor Web**: Ativa o servidor local para controle via navegador/celular (em desenvolvimento).
- **Ver QR Code**: Exibe o código para conexão rápida de dispositivos móveis.

---

## 2. Editor de Temas WYSIWYG

Uma ferramenta poderosa para criar a identidade visual perfeita para sua apresentação.

### Funcionalidades:

- **Preview em Tempo Real**: Veja as alterações instantaneamente na área de pré-visualização à direita.
- **Fontes**:
  - Escolha fontes separadas para Título e Corpo.
  - Ajuste tamanho, cor e família da fonte.
- **Alinhamento**: Esquerda, Centro ou Direita.
- **Efeitos Visuais**:
  - **Sombras**: Adicione profundidade com controle de desfoque e distância.
  - **Bordas**: Habilite bordas personalizadas com cor e espessura.
  - **Fundo**: Escolha entre cor sólida ou gradiente.
- **Gerenciamento**:
  - **Salvar**: Armazene seu tema com um nome personalizado.
  - **Carregar**: Edite temas existentes.
  - **Excluir**: Remova temas antigos.

## 3. Transições Suaves

Controle o tempo de transição entre os slides para evitar cortes bruscos.

- **Duração do Fade**: Ajustável de 0 a 3 segundos.
- **Tipo de Transição**: Suporte a _Fade_ (transparência), _Slide_ (deslizamento) ou Nenhuma.

## 4. Atalhos de Teclado

Aumente a produtividade com atalhos para as funções mais usadas:

- **F5**: Iniciar Apresentação
- **ESC**: Parar Apresentação (Emergência)
- **F11**: Alternar Banner
- **F9**: Alternar Relógio
- **F10**: Alternar Timer
- **Espaço**: Iniciar/Pausar Timer
- **Setas**: Navegar pelos itens

## 5. Próximos Passos (Roadmap)

- **Implementação completa do Servidor Web** para controle remoto via celular.
- **Editor de Atalhos** totalmente customizável (remapeamento de teclas).
- **Exportação de Cronogramas** para arquivo (JSON/XML).
