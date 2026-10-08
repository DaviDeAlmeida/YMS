# Modelagem do domínio: sessão de decisões

Registro da sessão de `/grill-with-docs` sobre o domínio de Recebimento do YMS. Os termos resolvidos vivem em [`GLOSSARY.md`](../../GLOSSARY.md); aqui ficam as decisões que não são vocabulário e as perguntas ainda abertas, para retomar a sessão depois.

Iniciada em 2026-10-07. Última atualização em 2026-10-08.

## Decidido

### Escopo e participantes

- **Foco só em recebimento.** O Fornecedor agenda a entrega de Pedidos de Compra do Owner em um CD; a logística do Owner aprova, recusa e reagenda.
- **Vários Owners no mesmo sistema**, com dados isolados entre si. Cada Owner configura vários CDs.
- **Fornecedor único por CNPJ**, ligado a cada Owner por um Vínculo, com um único login para todos os Owners. _(candidato a ADR)_

### Pedidos de Compra

- **Origem:** o Owner envia os Pedidos de Compra pela API REST do YMS, um a um ou em lote.
- **Entrega parcial permitida:** um Pedido de Compra pode ser dividido em vários Agendamentos, com Saldo controlado por item.
- **O Saldo é reservado na solicitação** do Agendamento e volta quando ele é Recusado, Cancelado ou vira No-show.
- **Diferença de quantidade na entrega:** no MVP, o YMS considera entregue o que foi agendado. A API de Concluído aceita a quantidade recebida como campo opcional, só registrado, para que no futuro a diferença volte ao Saldo.
- **Sem margem acima do Saldo** no MVP. Uma margem configurável fica para o futuro.
- **A situação do Pedido de Compra é calculada** (Aberto, Parcialmente agendado, Totalmente agendado, Atendido); só Cancelado vem de ação do Owner.

### Janelas

- **O Fornecedor escolhe uma Janela livre** com capacidade, e o Owner aprova.

### Estados do Agendamento

- **8 estados:** Solicitado, Aprovado, Recusado, Cancelado, Chegou, Em descarga, Concluído, No-show. Reagendamento é uma ação, não um estado.
- **Reagendamento pelo Owner** mantém o Agendamento Aprovado; **pelo Fornecedor**, ele volta para Solicitado.
- **O último estado é Concluído, não Conferido.** A conferência é responsabilidade do WMS; se o WMS informar o fim da conferência, o YMS só registra o horário.
- **Aprovação automática:** o Owner pode ligar, configurável por CD. O padrão é aprovação manual.
- **Sem estado "Saiu" (gate-out)** por enquanto.
- **No-show automático:** uma rotina marca o Agendamento Aprovado sem chegada após o fim da Janela mais a Tolerância do CD. O Owner pode aceitar a chegada atrasada (No-show → Chegou).
- **Chegada após o fim da Janela** recebe a marcação Atrasado.
- **Quem altera os estados de pátio:**
  - Chegou: a portaria, pela tela.
  - Em descarga e Concluído: o operador da doca pela tela, ou o WMS do Owner pela API.
  - O sistema registra quem fez, por onde e quando.
- **Doca:** informada ao passar para Em descarga. Cada CD tem Docas cadastradas, e uma Doca não pode ter dois Agendamentos em descarga ao mesmo tempo.

## Em aberto

Cada item traz a recomendação entre parênteses.

### Cota mensal

- **Q1 (revisão):** ter cota mensal de Agendamentos por Fornecedor? (fica para depois do MVP; o modelo nasce preparado)
  - A cota é diferente da capacidade da Janela: a capacidade limita o CD num horário, para todos os Fornecedores; a cota limita um Fornecedor num mês.
  - Se a cota entrar, Q1.1 a Q1.3 voltam:
    - Cota por Vínculo e CD, com valor padrão do CD.
    - Cancelado e Recusado devolvem a vaga; No-show consome.
    - Esgotada a cota, o Agendamento fica marcado "acima da cota" e não há aprovação automática.

### Fornecedores e usuários

- **Q2.1:** como nasce um Vínculo? (o Owner convida o Fornecedor pelo CNPJ e e-mail)
- **Q2.2:** quais tipos de usuário existem? (Usuário do Owner, restrito por CD; Usuário do Fornecedor; Administrador da plataforma)

### API de Pedidos de Compra

- **Q3.1:** como o ERP do Owner se autentica? (OAuth2 Client Credentials pelo Keycloak, por Owner)
- **Q3.2:** como funciona o reenvio de um pedido? (upsert pelo número do ERP, único por Owner; a quantidade de um item não pode ficar abaixo do já agendado)
- **Q3.3:** o que acontece num lote com pedidos inválidos? (sucesso parcial, com resultado item a item; até 500 pedidos por requisição)
- **Q3.4:** o que acontece ao cancelar um pedido pela API? (retira os itens dos Agendamentos não concluídos; Agendamento vazio é cancelado e o Fornecedor é avisado)
- **Q3.5:** existe tela para Pedidos de Compra? (só consulta; criação e alteração só pela API)

### Janelas

- **Q5.1:** como o Owner configura as Janelas? (grade semanal por CD, com exceções por data)
- **Q5.2:** quando o Agendamento ocupa a capacidade da Janela? (já na solicitação; libera ao ser Recusado ou Cancelado)
- **Q5.3:** qual a antecedência para agendar? (prazo mínimo e máximo por CD; o mínimo também limita o cancelamento pelo Fornecedor)
- **Q5.4:** em que unidade se mede a capacidade da Janela? (Agendamentos no MVP; o Agendamento já registra a quantidade prevista de paletes ou volumes, para medir por paletes ou tempo de doca depois)

### Pátio e notificações

- **Q6.6:** o YMS avisa o sistema do Owner quando o Agendamento muda de estado? (webhooks depois do MVP; toda mudança de estado já nasce como evento de domínio)

### Veículo, produtos e arquitetura

- **Q7:** como registrar veículo e motorista? (placa, motorista, documento e transportadora como dados do Agendamento, sem cadastro próprio)
- **Q8:** Produto e Categoria pertencem ao Owner? (sim, com o Produto chegando pela API junto com o Pedido de Compra, por upsert pelo SKU)
- **Q9:** quais decisões viram ADR? (PostgreSQL, monólito modular com módulos Cadastros, Pedidos de Compra e Agendamento, e Fornecedor único com Vínculo)
