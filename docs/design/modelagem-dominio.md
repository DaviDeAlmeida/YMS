# Modelagem do domínio: sessão de decisões

Registro da sessão de `/grill-with-docs` sobre o domínio de Recebimento do YMS. Os termos resolvidos vivem em [`GLOSSARY.md`](../../GLOSSARY.md); aqui ficam as decisões que não são vocabulário e as perguntas ainda abertas, para retomar a sessão depois. O sistema legado que serve de inspiração está resumido em [`docs/reference/yms-referencia.md`](../reference/yms-referencia.md).

Iniciada em 2026-10-07. Última atualização em 2026-10-16.

## Decidido

### Escopo e participantes

- **Foco só em recebimento.** O Fornecedor agenda a entrega de Pedidos de Compra do Owner em um CD; a logística do Owner aprova, recusa e reagenda.
- **Vários Owners no mesmo sistema**, com dados isolados entre si. Cada Owner configura vários CDs.
- **Fornecedor por Owner, sem Vínculo** (S19/S20 e S27, 2026-10-16): o mesmo CNPJ enviado por dois Owners vira dois Fornecedores independentes, cada um com seu `externalId`, nome, endereço e usuários. Uma empresa que vende a dois Owners tem um login em cada. Substitui o Fornecedor único por CNPJ com Vínculo. _(candidato a ADR)_
- **O CNPJ do Fornecedor pode se repetir dentro do Owner** (S33): só o `externalId` é único por Owner. Dois Fornecedores do mesmo Owner com o mesmo CNPJ e `externalId` diferentes são Fornecedores distintos.
- **Um perfil por usuário.** Cada perfil tem acessos e carteiras específicos.
- **Portaria e doca formam um só perfil, o Operador de pátio**, que marca Chegou, Em descarga e Concluído.
- **Carteira:** o Administrador, ao cadastrar o usuário, define as listas de Fornecedores, Categorias e CDs que ele enxerga; cada lista vale "todos" ou itens específicos.
- **Operador logístico** é um usuário do Owner que aprova, recusa e reagenda Agendamentos.
- **Administrador** é o usuário do Owner com maior acesso; cria usuários do próprio Owner e dos Fornecedores.
- **Transportadora fora do MVP:** sem perfil nem cadastro; continua como dado do Agendamento.
- **Owners são criados só por um endpoint da API**, sem tela no MVP. O endpoint é protegido por uma credencial da plataforma (`platform-admin`), à qual os Owners não têm acesso. A criação do Owner já cria o primeiro Administrador, que recebe o e-mail de ativação, e o client OAuth2 do ERP do Owner.
- **Campos do Owner:** `Id`, `name`, `createdAt` e `updatedAt`. Não tem CNPJ, e não tem endpoint em lote.
- **Toda tabela de dados do Owner tem `owner_id`** (Categoria, Produto, Fornecedor, Centro de Distribuição, Pedido de Compra etc.).
- **O usuário do Fornecedor só enxerga Pedidos de Compra e Agendamentos do Owner daquele Fornecedor.**
- **Perfis do MVP:** Administrador, Operador logístico, Operador de pátio e Fornecedor. O perfil Fornecedor não tem subperfis; seus usuários são criados pelo Administrador do Owner. O `platform-admin` não é perfil de usuário: é uma credencial de máquina que só cria Owners pela API.

### Pedidos de Compra

- **Origem:** o Owner envia os Pedidos de Compra pela API REST do YMS, um a um ou em lote.
- **Integração dos cadastros:** Categoria, Produto, Fornecedor e Pedido de Compra chegam pela API, cada um com um endpoint unitário (um registro por requisição) e um endpoint em lote (vários registros numa requisição). O Centro de Distribuição é configurado pela tela.
- **Endpoint unitário é síncrono** (`200`/`201`, ou `422` com os erros). **Endpoint em lote é assíncrono** pelo RabbitMQ: responde `202` com um `batchId`, e o resultado item a item é consultado em `GET /batches/{id}`. O lote tem até 500 itens e aceita sucesso parcial.
- **Entrega parcial permitida:** um Pedido de Compra pode ser dividido em vários Agendamentos, com Saldo controlado por item.
- **O Saldo é reservado na solicitação** do Agendamento e volta quando ele é Recusado, Cancelado ou vira No-show.
- **Diferença de quantidade na entrega:** no MVP, o YMS considera entregue o que foi agendado. A API de Concluído aceita a quantidade recebida como campo opcional, só registrado, para que no futuro a diferença volte ao Saldo.
- **Sem Nota Fiscal no MVP.**
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

### Veículo

- **Sem módulo de veículos no MVP.**

### Notificações

- **Sem webhooks no MVP.** O YMS não avisa o sistema do Owner sobre mudanças de estado do Agendamento.

### Infraestrutura

- **AWS**, com toda a infraestrutura em **Terraform**.
- **Stack:** .NET 10, CQRS com MediatR (pacote da Lucky Penny Software, licença dupla; exige license key em produção), Duende IdentityServer v8 (a mais atual, só .NET 10) com a licença Community Edition para autenticação, PostgreSQL com Entity Framework Core (Npgsql) como ORM, RabbitMQ para mensageria.
- **Estrutura em camadas DDD, sem módulos:** projetos `YMS.Domain`, `YMS.Application`, `YMS.Infrastructure`, `YMS.Api` e `YMS.Identity` (Duende IdentityServer), com pastas por área (`Categories`, `Products`, `Suppliers`, `Orders`, `Batches`, `Appointments`) dentro de cada camada. Um único `DbContext`. Substitui a proposta de monólito modular; dá para separar em módulos depois, se precisar.
- **Contêineres:** Docker Compose no ambiente local, Kubernetes para orquestração.

### Cota mensal

- **Sem cota mensal no MVP.** A cota limita um Fornecedor num mês, o que é diferente da capacidade da Janela, que limita o CD num horário, para todos os Fornecedores. O modelo nasce preparado, porque o Agendamento já tem Fornecedor e CD. Se a cota entrar depois, o rascunho é este:
  - Cota por Fornecedor e CD, com valor padrão do CD.
  - Cancelado e Recusado devolvem a vaga; No-show consome.
  - Esgotada a cota, o Agendamento fica marcado "acima da cota" e não há aprovação automática.

### Spec da API de integração ([#1](https://github.com/DaviDeAlmeida/YMS/issues/1))

- **Fora da spec:** o cadastro de Centro de Distribuição (o CD continua existindo por seed e por fixture de teste) e a autenticação (sem JWT nem IdentityServer). A spec só exige que o Owner atual venha de uma claim do usuário autenticado; nos testes, essa claim vem de um esquema de autenticação de teste.
- **Pedido de Compra também fica fora da spec #1**, que cobre só Categoria, Produto e Fornecedor. O Pedido de Compra ganha uma spec própria; as decisões abaixo sobre ele valem para essa spec.
- **Os endpoints de Pedido de Compra usam a rota `orders`**, não `purchase-orders`, e a classe se chama `Order` (S26).
- **A autenticação fica para specs futuras** (S30). A spec #1 testa com um esquema de autenticação de teste que emite o `ownerId` na claim.
- **O Pedido de Compra tem Categoria própria**, enviada pelo ERP. É por ela que a Carteira filtra os Pedidos.
- **Um Pedido de Compra aceita itens de Produtos de outras Categorias.** A Categoria do Pedido é a que vale para a Carteira; a Categoria do Produto é só informativa.
- **Categoria é plana**, sem hierarquia.
- **O Produto tem unidade de medida obrigatória.** Não há quantidade por embalagem. Por enquanto, a unidade é fixa em `UN`.
- **Todas as validações dos endpoints usam FluentValidation**, divididas assim:
  - Formato da entrada (obrigatório, tamanho, CNPJ, GTIN, EAN principal): validator do FluentValidation na `YMS.Application`.
  - Referências e unicidade, que consultam o banco: validator assíncrono do FluentValidation na `YMS.Application`.
  - Invariantes do agregado (quantidade abaixo do agendado, reenvio de Pedido Cancelado): método do agregado na `YMS.Domain`, que lança uma exceção de domínio; a API a converte em `422`, no mesmo formato.
- **CQRS com pastas separadas** para Commands e Queries: área → `Commands` ou `Queries` → uma pasta por caso de uso (Command, Handler e Validator juntos).
- **Todo o código é em inglês**: projetos, pastas, classes, rotas e campos do contrato. Os nomes do domínio seguem a coluna em inglês do `GLOSSARY.md`.
- **Campos da integração** (nomes do contrato da API):
  - Categoria: `externalId`, `description`, `createdAt`, `updatedAt`.
  - Produto: os mesmos da Categoria, mais `categoryExternalId` (a Categoria do Produto) e `eans`, uma lista de `{ value, isPrimary }` com pelo menos um EAN e exatamente um `isPrimary = true`; se a lista tiver um EAN só, ele vira o principal (S18.1).
  - Fornecedor: `externalId`, `name`, `cnpj` e `address` com `country`, `state`, `city`, `street`, `number`, `complement`, `district` e `zipCode`. Obrigatórios no endereço: `country`, `state`, `city`, `street` e `zipCode`; com `country = BR`, a UF e o CEP (8 dígitos) são validados.
- **O `ownerId` nunca vem da requisição.** É preenchido automaticamente a partir do usuário autenticado, tanto na integração quanto nas telas.
- **`createdAt` e `updatedAt` são preenchidos pelo YMS**, em UTC, e só aparecem na resposta.
- **Todo modelo tem um `Id` interno**, a chave primária no PostgreSQL, separado do `externalId` do ERP. Vale para Categoria, Produto, Fornecedor e os demais modelos. O tipo é `uuid` v7, gerado pela aplicação com `Guid.CreateVersion7()`. O `Id` não aparece na API (S32): as rotas e as referências entre modelos usam o `externalId`.
- **A unidade de medida não entra no contrato** enquanto for fixa: o YMS grava `UN` e a devolve na resposta.
- **EAN é texto livre:** sem validação de tamanho, formato ou dígito verificador, e sem checagem de EAN repetido entre Produtos. No reenvio do Produto, a lista de EANs substitui a anterior.
- **Sem campo `active`** em Categoria, Produto ou Fornecedor.
- **`GET` por chave** para Categoria, Produto e Fornecedor, sem listagem.
- **Lote sem `Idempotency-Key`** e sem regra de retenção definida.

## Em aberto

Cada item traz a recomendação entre parênteses.

Os números com prefixo `S` são da sessão de 2026-10-12 em diante e não se confundem com os da sessão anterior. A spec #1 (Categoria, Produto e Fornecedor) não tem mais perguntas em aberto.

### Spec de autenticação (futura)

- **S28:** o "super usuário" entregue ao cliente para obter o `accessToken` é uma credencial de máquina (OAuth2 Client Credentials, separada do Administrador) ou um usuário com login e senha? (credencial de máquina, chamada **Credencial de integração** no glossário)
- **S29:** como o Owner é criado? Opções: (a) pelo endpoint `platform-admin`, que já devolve a Credencial de integração; (b) pela tabela de Owner, criada à mão ou por script no MVP, com um endpoint separado para a credencial. (a)

### Spec do Pedido de Compra (a criar)

- **S22:** quais são os campos da integração do Pedido de Compra? (você manda a lista; ela destrava S5.1, S6, S7 e S8)
- **S5.1:** a quantidade do item é inteira e maior que zero, porque a unidade é fixa em `UN`? (sim)
- **S6:** o Pedido tem data de emissão (obrigatória), comprador e observação (opcionais), sem valores? (sim)
- **S7:** o item é identificado por um número de linha vindo do ERP, que vira a chave no reenvio, permitindo o mesmo Produto em linhas diferentes? (sim)
- **S8:** o Pedido referencia o Fornecedor pelo CNPJ ou pelo `externalId`? (CNPJ)
- **S11:** `POST /orders/{externalId}/cancel` entra na spec, só marcando Cancelado? (sim; o efeito nos Agendamentos fica para a spec de Agendamentos)
- **S25:** "Entrega" é o mesmo que Agendamento, ou é outro conceito (o conteúdo do veículo, ou o recebimento físico)? (o mesmo: o termo continua sendo Agendamento)

### Depois de fechar as perguntas

- Reescrever a issue [#1](https://github.com/DaviDeAlmeida/YMS/issues/1): tirar Pedido de Compra, CD e autenticação; incluir os campos e as regras decididas em 2026-10-12.
- Rodar `/to-tickets #1`.

### API de Pedidos de Compra

- **Q3.1:** como o ERP do Owner se autentica? (OAuth2 Client Credentials pelo IdentityServer, um client por Owner)
- **Q3.2:** como funciona o reenvio de um pedido? (upsert pelo número do ERP, único por Owner; a quantidade de um item não pode ficar abaixo do já agendado)
- **Q3.4:** o que acontece ao cancelar um pedido pela API? (retira os itens dos Agendamentos não concluídos; Agendamento vazio é cancelado e o Fornecedor é avisado)
- **Q3.5:** existe tela para Pedidos de Compra? (só consulta; criação e alteração só pela API)

### Janelas

- **Q5.1:** como o Owner configura as Janelas? (grade semanal por CD, com exceções por data)
- **Q5.2:** quando o Agendamento ocupa a capacidade da Janela? (já na solicitação; libera ao ser Recusado ou Cancelado)
- **Q5.3:** qual a antecedência para agendar? (prazo mínimo e máximo por CD; o mínimo também limita o cancelamento pelo Fornecedor)
- **Q5.4:** em que unidade se mede a capacidade da Janela? (Agendamentos no MVP; o Agendamento já registra a quantidade prevista de paletes ou volumes, para medir por paletes ou tempo de doca depois)

### Veículo, produtos e arquitetura

- **Q9:** quais decisões viram ADR? (PostgreSQL, estrutura em camadas DDD, Fornecedor por Owner, isolamento por `owner_id`, stack .NET, AWS com Terraform)
